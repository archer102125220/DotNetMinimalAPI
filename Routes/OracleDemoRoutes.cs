using DotNetMinimalAPI.Data;
using DotNetMinimalAPI.Models;
using DotNetMinimalAPI.Models.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace DotNetMinimalAPI.Routes;

/// <summary>
/// Oracle 資料庫示範路由。
/// 在 Minimal API 架構中，我們不使用 Controller，而是將路由定義為靜態擴充方法。
/// 這能大幅減少記憶體分配與啟動時間，非常適合配合 Native AOT 使用。
/// </summary>
public static class OracleDemoRoutes
{
    /// <summary>
    /// 註冊所有的 Oracle Demo 相關路由。
    /// 建議在 Program.cs 裡統一呼叫：app.MapGroup("/api/oracle-demo").MapOracleDemoRoutes();
    /// </summary>
    public static RouteGroupBuilder MapOracleDemoRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetItems)
             .WithName("GetOracleDemoItems")
             .WithSummary("取得所有 Oracle 資料庫示範項目 (支援 keyword 搜尋)");

        group.MapGet("/{id}", GetItemById)
             .WithName("GetOracleDemoItemById")
             .WithSummary("取得特定 Oracle 示範項目");

        group.MapPost("/", CreateItem)
             .WithName("CreateOracleDemoItem")
             .WithSummary("建立新 Oracle 示範項目");

        group.MapPut("/{id}", UpdateItem)
             .WithName("UpdateOracleDemoItem")
             .WithSummary("更新特定 Oracle 示範項目");

        group.MapDelete("/{id}", DeleteItem)
             .WithName("DeleteOracleDemoItem")
             .WithSummary("刪除特定 Oracle 示範項目");

        return group;
    }

    #region EF Core Compiled Queries (Native AOT 解決方案)
    // 💡【教學：為什麼要用 EF.CompileAsyncQuery？】
    // 在啟用了 <PublishAot>true</PublishAot> 的專案中，不支援「動態建立執行期程式碼」(也就是沒有 JIT 編譯器)。
    // EF Core 原本的 .Where(i => i.Id == id).ToListAsync() 會在「執行期」依賴 JIT 將 Expression Tree 轉譯成 SQL。
    // 這在 AOT 模式下會導致 System.InvalidOperationException 錯誤！
    // 
    // 解決方案：
    // 我們必須將查詢提升為「靜態唯讀的預先編譯查詢」(Static Readonly Func)，這樣 .NET AOT 編譯器
    // 在建置期間就能完全分析這個查詢形狀 (Query Shape)，避免執行期動態編譯。
    
    /// <summary>
    /// 取得所有項目的靜態編譯查詢 (唯讀，使用 AsNoTracking 提升效能)
    /// </summary>
    private static readonly Func<AppDbContext, IAsyncEnumerable<OracleDemoItem>> GetAllItemsQuery =
        EF.CompileAsyncQuery<AppDbContext, OracleDemoItem>((AppDbContext db) => db.OracleDemoItems.AsNoTracking().OrderByDescending(i => i.CreatedAt));

    /// <summary>
    /// 透過關鍵字搜尋項目的靜態編譯查詢 (注意：以 EF.Functions.Like 取代動態組合的 LINQ)
    /// </summary>
    private static readonly Func<AppDbContext, string, IAsyncEnumerable<OracleDemoItem>> SearchItemsQuery =
        EF.CompileAsyncQuery<AppDbContext, string, OracleDemoItem>((AppDbContext db, string pattern) => db.OracleDemoItems.Where(i => EF.Functions.Like(i.Name, pattern)).AsNoTracking().OrderByDescending(i => i.CreatedAt));

    /// <summary>
    /// 取得單一項目 (用於讀取，掛載 AsNoTracking，減少記憶體與追蹤成本)
    /// </summary>
    private static readonly Func<AppDbContext, int, Task<OracleDemoItem?>> GetItemByIdQuery =
        EF.CompileAsyncQuery<AppDbContext, int, OracleDemoItem?>((AppDbContext db, int id) => db.OracleDemoItems.AsNoTracking().FirstOrDefault(i => i.Id == id));

    /// <summary>
    /// 取得單一項目 (用於修改/刪除，【不使用】AsNoTracking，這樣 SaveChangesAsync 才能感知變更)
    /// </summary>
    private static readonly Func<AppDbContext, int, Task<OracleDemoItem?>> GetTrackedItemByIdQuery =
        EF.CompileAsyncQuery<AppDbContext, int, OracleDemoItem?>((AppDbContext db, int id) => db.OracleDemoItems.FirstOrDefault(i => i.Id == id));
    #endregion

    // 💡【教學：強型別回傳 TypedResults】
    // 方法的簽章使用 Task<Results<Ok<...>, BadRequest>>，這能幫助 OpenAPI (Swagger/Scalar) 
    // 在「不使用反射」的情況下推斷出正確的 HTTP 狀態碼與結構描述 (Schema)，這對 AOT 也是必須的。
    private static async Task<Results<Ok<List<OracleDemoItemResponse>>, BadRequest>> GetItems(string? keyword, AppDbContext db)
    {
        List<OracleDemoItem> items = new();
        if (string.IsNullOrEmpty(keyword))
        {
            await foreach (OracleDemoItem item in GetAllItemsQuery(db))
            {
                items.Add(item);
            }
        }
        else
        {
            string searchPattern = $"%{keyword}%";
            await foreach (OracleDemoItem item in SearchItemsQuery(db, searchPattern))
            {
                items.Add(item);
            }
        }

        // 💡【教學：使用 DTO (Data Transfer Object)】
        // 絕對不要直接回傳 EF Core 的 Entity (OracleDemoItem) 給客戶端，這會暴露資料庫欄位並可能造成無窮迴圈序列化。
        // 請務必轉換為專用的 Response 紀錄 (Record / Class)。
        List<OracleDemoItemResponse> response = items.Select(i => new OracleDemoItemResponse(i.Id, i.Name, i.Description, i.CreatedAt, i.CategoryId)).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<OracleDemoItemResponse>, NotFound>> GetItemById(int id, AppDbContext db)
    {
        OracleDemoItem? item = await GetItemByIdQuery(db, id);
        if (item is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new OracleDemoItemResponse(item.Id, item.Name, item.Description, item.CreatedAt, item.CategoryId));
    }

    private static async Task<Results<Created<OracleDemoItemResponse>, BadRequest>> CreateItem(CreateOracleDemoItemRequest request, AppDbContext db)
    {
        OracleDemoItem item = new OracleDemoItem
        {
            Name = request.Name,
            Description = request.Description,
            CategoryId = request.CategoryId
        };

        db.OracleDemoItems.Add(item);
        await db.SaveChangesAsync();

        OracleDemoItemResponse response = new OracleDemoItemResponse(item.Id, item.Name, item.Description, item.CreatedAt, item.CategoryId);
        return TypedResults.Created($"/api/oracle-demo/{item.Id}", response);
    }

    private static async Task<Results<NoContent, NotFound>> UpdateItem(int id, UpdateOracleDemoItemRequest request, AppDbContext db)
    {
        OracleDemoItem? item = await GetTrackedItemByIdQuery(db, id);
        if (item is null)
        {
            return TypedResults.NotFound();
        }

        item.Name = request.Name;
        item.Description = request.Description;
        item.CategoryId = request.CategoryId;

        await db.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> DeleteItem(int id, AppDbContext db)
    {
        OracleDemoItem? item = await GetTrackedItemByIdQuery(db, id);
        if (item is null)
        {
            return TypedResults.NotFound();
        }

        db.OracleDemoItems.Remove(item);
        await db.SaveChangesAsync();
        return TypedResults.NoContent();
    }
}

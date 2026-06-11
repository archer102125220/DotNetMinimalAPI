using System.Data.Common;
using DotNetMinimalAPI.Data;
using DotNetMinimalAPI.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;

namespace DotNetMinimalAPI.Routes;

/// <summary>
/// ⚠️ 【強烈警告：不安全的舊有寫法示範】 ⚠️
/// 
/// 此檔案僅供「參考舊專案寫法」而建立，展示了【不使用 DTO】，直接將資料庫 Entity 
/// 作為 Request 與 Response 的危險操作方式。
/// 
/// 
/// 【風險提示】：
/// 1. Over-posting (大量指派)：POST/PUT 時直接接收 Entity，會讓使用者有機會竄改 `Id` 或 `CreatedAt` 等內部保護欄位。
/// 2. 資料外洩：GET 時直接回傳 Entity，會連同所有敏感欄位一併回傳給前端。
/// 3. 無窮迴圈：若未來加入 `Category.Items` 導覽屬性，回傳 Entity 會導致 JSON 序列化產生無限迴圈當機。
/// 
/// 🛡️ 【關於 SQL 隱碼攻擊 (SQL Injection) 的防禦】：
/// 本檔案中的寫法「沒有」SQL 隱碼攻擊的風險。因為無論是 `db.OracleDemoItems.Add()` 還是 
/// `FirstOrDefault(i => i.Id == id)`，Entity Framework Core 在底層都會自動將其轉換為「參數化查詢 (Parameterized Queries)」。
/// (下方有提供如果您必須手寫 原生 SQL (Raw SQL) 時的安全防禦範例程式碼)
/// </summary>
public static class LegacyOracleDemoRoutes
{
    public static RouteGroupBuilder MapLegacyOracleDemoRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetItems).WithSummary("⚠️ 危險寫法：直接回傳 Entity 陣列");
        group.MapGet("/{id}", GetItemById).WithSummary("⚠️ 危險寫法：直接回傳單一 Entity");
        group.MapPost("/", CreateItem).WithSummary("⚠️ 危險寫法：直接接收 Entity (Over-posting 風險)");
        group.MapPut("/{id}", UpdateItem).WithSummary("⚠️ 危險寫法：直接從 Entity 更新資料");
        group.MapDelete("/{id}", DeleteItem).WithSummary("⚠️ 刪除項目");
        group.MapGet("/adonet", GetItemsWithAdoNetLegacy).WithSummary("⚠️ 危險寫法：ADO.NET 直接回傳 Entity 且有隱碼攻擊風險");

        return group;
    }

    #region EF Core AOT 編譯查詢
    // 為了配合本專案的 Native AOT 限制，這裡依然使用 CompileAsyncQuery 避免編譯錯誤
    private static readonly Func<AppDbContext, IAsyncEnumerable<OracleDemoItem>> GetAllItemsQuery =
        EF.CompileAsyncQuery<AppDbContext, OracleDemoItem>((AppDbContext db) => db.OracleDemoItems.AsNoTracking().OrderByDescending(i => i.CreatedAt));

    private static readonly Func<AppDbContext, int, Task<OracleDemoItem?>> GetItemByIdQuery =
        EF.CompileAsyncQuery<AppDbContext, int, OracleDemoItem?>((AppDbContext db, int id) => db.OracleDemoItems.AsNoTracking().FirstOrDefault(i => i.Id == id));

    private static readonly Func<AppDbContext, int, Task<OracleDemoItem?>> GetTrackedItemByIdQuery =
        EF.CompileAsyncQuery<AppDbContext, int, OracleDemoItem?>((AppDbContext db, int id) => db.OracleDemoItems.FirstOrDefault(i => i.Id == id));
    #endregion

    /// <summary>
    /// ⚠️ 查詢：直接回傳 List<OracleDemoItem> (Entity)
    /// </summary>
    private static async Task<Results<Ok<List<OracleDemoItem>>, BadRequest>> GetItems(AppDbContext db)
    {
        List<OracleDemoItem> items = new();
        await foreach (OracleDemoItem item in GetAllItemsQuery(db))
        {
            items.Add(item);
        }
        
        // 【危險】直接將含有導覽屬性 (Category) 的 Entity 回傳，若發生循環參考將導致 JSON 序列化當機
        return TypedResults.Ok(items);
    }

    /// <summary>
    /// ⚠️ 查詢單筆：直接回傳單一 Entity
    /// (🛡️ 這裡的 `GetItemByIdQuery` 底層已被 EF Core 自動轉為參數化 SQL `WHERE Id = :p0`，因此免疫隱碼攻擊)
    /// </summary>
    private static async Task<Results<Ok<OracleDemoItem>, NotFound>> GetItemById(int id, AppDbContext db)
    {
        OracleDemoItem? item = await GetItemByIdQuery(db, id);
        if (item is null) return TypedResults.NotFound();
        
        return TypedResults.Ok(item);
    }

    /// <summary>
    /// ⚠️ 新增 (POST)：直接把 Entity 當作參數接收
    /// </summary>
    private static async Task<Results<Created<OracleDemoItem>, BadRequest>> CreateItem(OracleDemoItem item, AppDbContext db)
    {
        // 【極度危險】Over-posting 漏洞！
        // 駭客可以在 Request Body 中塞入： { "Id": 99999, "CreatedAt": "2000-01-01T00:00:00Z" }
        // EF Core 如果沒有嚴格把關，可能會導致資料庫中寫入被惡意竄改的保護欄位！
        db.OracleDemoItems.Add(item);
        await db.SaveChangesAsync();

        return TypedResults.Created($"/api/legacy-oracle-demo/{item.Id}", item);
    }

    /// <summary>
    /// ⚠️ 修改 (PUT)：將傳入的 Entity 直接映射更新
    /// </summary>
    private static async Task<Results<NoContent, NotFound>> UpdateItem(int id, OracleDemoItem updatedItem, AppDbContext db)
    {
        OracleDemoItem? item = await GetTrackedItemByIdQuery(db, id);
        if (item is null) return TypedResults.NotFound();

        // 雖然這裡是手動賦值，但因為傳入的是整個 Entity (updatedItem)，
        // 舊專案很多時候會直接寫： db.Entry(updatedItem).State = EntityState.Modified;
        // 這會導致那些沒有被前端傳進來的欄位 (例如 CreatedAt) 全數被 Null 覆寫，引發嚴重的資料遺失。
        item.Name = updatedItem.Name;
        item.Description = updatedItem.Description;
        item.CategoryId = updatedItem.CategoryId;

        await db.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    /// <summary>
    /// ⚠️ 刪除 (DELETE)
    /// </summary>
    private static async Task<Results<NoContent, NotFound>> DeleteItem(int id, AppDbContext db)
    {
        OracleDemoItem? item = await GetTrackedItemByIdQuery(db, id);
        if (item is null) return TypedResults.NotFound();

        db.OracleDemoItems.Remove(item);
        await db.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    /// <summary>
    /// 🛡️ 【防禦教學示範】：如何在舊系統中使用「原生 SQL」且防禦隱碼攻擊
    /// 
    /// 如果在舊系統中，開發者因為查詢複雜而必須手寫 Raw SQL，請務必按照下方的方式撰寫。
    /// (此處僅以註解展示，因為 Native AOT 環境下不建議使用依賴動態反射的 FromSqlInterpolated)
    /// </summary>
    /*
    private static async Task<Results<Ok<List<OracleDemoItem>>, BadRequest>> SearchWithRawSql(string keyword, AppDbContext db)
    {
        // ❌ 絕對禁止的寫法 (SQL Injection 漏洞)：
        // 字串直接串接會讓惡意使用者輸入 "'; DROP TABLE OracleDemoItems; --"
        // var items = await db.OracleDemoItems.FromSqlRaw("SELECT * FROM OracleDemoItems WHERE Name LIKE '%" + keyword + "%'").ToListAsync();

        // ✅ 老系統的補救防禦方案 (自動參數化)：
        // 使用 FromSqlInterpolated，EF Core 會自動把 $"" 字串內插攔截下來，
        // 並將 keyword 轉換成安全的參數 (例如 :p0)，防止隱碼攻擊。
        string searchPattern = $"%{keyword}%";
        var items = await db.OracleDemoItems
            .FromSqlInterpolated($"SELECT * FROM \"OracleDemoItems\" WHERE \"Name\" LIKE {searchPattern}")
            .ToListAsync();
            
        return TypedResults.Ok(items);
    }
    */

    /// <summary>
    /// ⚠️ 【危險寫法示範：原生 ADO.NET + 隱碼攻擊 + 直接回傳 Entity】
    /// 這裡示範了舊系統中最致命的幾種寫法：
    /// 1. 直接字串串接 SQL 語法 (產生極度嚴重的 SQL Injection 漏洞)。
    /// 2. 使用原生 ADO.NET 讀出資料後，直接塞給 Entity 而不用 DTO (無窮迴圈與資料外洩)。
    /// </summary>
    private static async Task<Results<Ok<List<OracleDemoItem>>, BadRequest<string>>> GetItemsWithAdoNetLegacy(string? keyword, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("OracleDemoConnection");
        if (string.IsNullOrEmpty(connectionString)) return TypedResults.BadRequest("無法取得連線字串");

        List<OracleDemoItem> resultList = new();

        await using OracleConnection connection = new OracleConnection(connectionString);
        await connection.OpenAsync();

        await using OracleCommand command = connection.CreateCommand();
        
        string sqlText = """
            SELECT "Id", "Name", "Description", "CreatedAt", "CategoryId"
            FROM "OracleDemoItems"
        """;

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            // ❌ 【極度危險】SQL Injection 漏洞！
            // 舊系統常見的錯誤：直接把使用者的輸入 (keyword) 用字串加進 SQL 中。
            // 駭客只要輸入： `'; DROP TABLE "OracleDemoItems"; --`
            // 就會導致整個資料表被惡意刪除！
            sqlText += " WHERE \"Name\" LIKE '%" + keyword + "%'";

            // ✅ 【防禦方式】：使用「參數化查詢 (Parameterized Query)」
            // 絕對不能像上面那樣直接把字串加進去。必須改成宣告參數，讓資料庫底層將使用者的輸入視為「純文字」而非「可執行的指令」。
            // 以下為正確的防禦寫法 (請參考 AdoNetOracleDemoRoutes.cs 的實作)：
            /*
            sqlText += " WHERE \"Name\" LIKE :Keyword";
            command.Parameters.Add(new OracleParameter("Keyword", OracleDbType.NVarchar2) 
            { 
                Value = $"%{keyword}%" 
            });
            */
        }

        sqlText += " ORDER BY \"CreatedAt\" DESC";
        command.CommandText = sqlText;

        await using DbDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            // ❌ 【危險】直接組裝成 Entity 回傳，若此 Entity 以後掛上 Category 關聯，會引發序列化無窮迴圈
            OracleDemoItem item = new OracleDemoItem
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                CreatedAt = reader.GetDateTime(3),
                CategoryId = reader.IsDBNull(4) ? null : reader.GetInt32(4)
            };

            // ✅ 【正確寫法】：必須使用 DTO (Data Transfer Object) 進行資料轉換，絕不可將底層 Entity 曝露給前端
            // 應該要將資料裝進一個純資料傳遞用的 Record 或 Class，才能切斷與資料庫的關聯。
            // (請參考 AdoNetOracleDemoRoutes.cs 中的寫法)：
            /*
            OracleDemoItemResponse dtoItem = new OracleDemoItemResponse(
                Id: reader.GetInt32(0),
                Name: reader.GetString(1),
                Description: reader.IsDBNull(2) ? null : reader.GetString(2),
                CreatedAt: reader.GetDateTime(3),
                CategoryId: reader.IsDBNull(4) ? null : reader.GetInt32(4)
            );
            // 當然，最上面的 resultList 宣告也要記得改為 List<OracleDemoItemResponse>
            // resultList.Add(dtoItem);
            */

            resultList.Add(item);
        }

        return TypedResults.Ok(resultList);
    }
}

using System.Data.Common;
using DotNetMinimalAPI.Models.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Oracle.ManagedDataAccess.Client; // 需要原生的 Oracle 驅動程式

namespace DotNetMinimalAPI.Routes;

/// <summary>
/// 💡 【教學：原生 ADO.NET 並存示範】
/// 在原本的 MVC 專案中，我們展示了 EF Core 與原生 ADO.NET (OracleConnection) 的並存。
/// 
/// 在 Minimal API 且開啟 Native AOT 的環境下，原生 ADO.NET 其實是非常棒的選擇！
/// 因為我們手動從 DataReader 取出欄位 (完全沒有用到 Reflection 或動態綁定)，
/// 這不但完全符合 AOT 的嚴格要求，且執行效能是所有連線方式中最高的。
/// </summary>
public static class AdoNetOracleDemoRoutes
{
    public static RouteGroupBuilder MapAdoNetOracleDemoRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetItemsWithAdoNet).WithSummary("示範：使用原生 ADO.NET (OracleConnection) 查詢");
        
        return group;
    }

    /// <summary>
    /// 示範如何直接使用 Oracle.ManagedDataAccess.Client 原生 ADO.NET 方式連線與查詢
    /// 並結合 Minimal API 的 DTO 回傳。
    /// </summary>
    private static async Task<Results<Ok<List<OracleDemoItemResponse>>, BadRequest<string>>> GetItemsWithAdoNet(string? keyword, IConfiguration configuration)
    {
        // 透過依賴注入取得 IConfiguration，直接從 appsettings.json 中讀取連線字串
        string? connectionString = configuration.GetConnectionString("OracleDemoConnection");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            return TypedResults.BadRequest("無法從 appsettings.json 取得 OracleDemoConnection 連接字串");
        }

        List<OracleDemoItemResponse> resultList = new();

        // ⚠️ 深度檢查注意：必須使用 await using 包覆 IDisposable 物件 (OracleConnection, OracleCommand, DbDataReader)
        // 原生的 ADO.NET 操作需要開發者自行負責釋放連線。如果忘記 using，會造成 Connection Pool 被耗盡。
        await using OracleConnection connection = new OracleConnection(connectionString);
        
        // Async First 政策：所有資料庫操作都必須使用 Async 版本。
        await connection.OpenAsync();

        await using OracleCommand command = connection.CreateCommand();
        
        // 撰寫原生 SQL 查詢
        // ⚠️ 注意：Oracle 對於加了雙引號建立的欄位和表格會「強制區分大小寫」，所以這裡的 SQL 也要有雙引號。
        string sqlText = """
            SELECT 
                item."Id", 
                item."Name", 
                item."Description", 
                item."CreatedAt", 
                item."CategoryId"
            FROM "OracleDemoItems" item
        """;

        // 動態加入搜尋條件 (WHERE)
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            sqlText += " WHERE item.\"Name\" LIKE :keyword";
            
            // 🛡️ 防禦隱碼攻擊：絕對禁止字串拼接！必須使用 Parameter 參數化查詢
            command.Parameters.Add(new OracleParameter("keyword", $"%{keyword}%"));
        }

        sqlText += " ORDER BY item.\"CreatedAt\" DESC";
        command.CommandText = sqlText;

        // ExecuteReaderAsync 會開啟資料流讀取器
        await using DbDataReader reader = await command.ExecuteReaderAsync();
        
        // ReadAsync() 會逐筆將資料拉到應用程式記憶體中
        while (await reader.ReadAsync())
        {
            // 原生讀取資料時，針對可能為 NULL 的欄位，必須先呼叫 IsDBNull 進行檢查。
            // 否則呼叫 GetString 或 GetInt32 時會引發 SqlNullValueException！
            
            int id = reader.GetInt32(0);
            string name = reader.GetString(1);
            string? description = reader.IsDBNull(2) ? null : reader.GetString(2);
            DateTime createdAt = reader.GetDateTime(3);
            int? categoryId = reader.IsDBNull(4) ? null : reader.GetInt32(4);

            // 直接封裝進我們設計好的 DTO
            resultList.Add(new OracleDemoItemResponse(id, name, description, createdAt, categoryId));
        }

        return TypedResults.Ok(resultList);
    }
}

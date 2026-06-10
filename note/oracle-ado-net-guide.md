# 原生 ADO.NET (Oracle.ManagedDataAccess.Client) 操作指南

這份筆記整理了如何在 .NET 專案中，不透過 Entity Framework Core (ORM)，而是直接使用 **原生 ADO.NET** 與 Oracle 資料庫進行連線與操作。

---

## 1. 什麼是 ADO.NET？為什麼要用它？

ADO.NET 是 .NET 平台最基礎的資料存取技術。我們平常用的 Entity Framework Core (EF Core) 其實底層也是基於 ADO.NET 實作的。

### 何時應該選擇原生 ADO.NET？
雖然 EF Core 提供了強大的 LINQ 查詢與物件關聯對應 (ORM)，但在以下情境，直接使用原生 ADO.NET 會是更好的選擇：
- **極致的效能要求**：不需要經過 ORM 的物件轉換與追蹤 (Tracking)，查詢速度最快、記憶體佔用最低。
- **極度複雜的 SQL**：遇到多層 Subquery、複雜的 Window Functions (分析函數)、特定的 Oracle 獨有語法，EF Core LINQ 很難或無法轉換時。
- **大批量資料處理 (Bulk Operations)**：一次要寫入或更新數十萬筆資料。
- **呼叫特定的 Stored Procedure**：某些回傳多個游標 (REF CURSOR) 的預存程序，透過原生 ADO.NET 處理最為彈性。

---

## 2. 套件安裝

要連線到 Oracle 資料庫，我們需要官方提供的 Managed Driver。
- **套件名稱**：`Oracle.ManagedDataAccess.Core`

> [!NOTE]
> **本專案現況：**
> 因為專案中已經安裝了 `Oracle.EntityFrameworkCore`，而它已經將 `Oracle.ManagedDataAccess.Core` 作為底層相依套件一併安裝了。因此，您**不需要**額外手動安裝，直接在程式碼中加入 `using Oracle.ManagedDataAccess.Client;` 即可使用。

---

## 3. ADO.NET 核心三大物件

在操作原生資料庫時，通常圍繞著以下三個核心物件：
1. **`OracleConnection`**：負責建立與資料庫實體的連線。
2. **`OracleCommand`**：負責攜帶並執行 SQL 指令。
3. **`OracleDataReader`** (或 `DbDataReader`)：負責將查詢結果，以「流式 (Streaming)」的方式逐筆從資料庫讀回記憶體。

---

## 4. 基礎查詢範例程式碼

以下是在 Controller 或 Service 中呼叫原生 ADO.NET 撈取資料的標準寫法：

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;

public async Task<List<OracleDemoItem>> GetItemsViaAdoNet()
{
    // 1. 取得連線字串 (可直接從現有 DbContext 取，或透過 IConfiguration 讀取 appsettings.json)
    string connectionString = _context.Database.GetConnectionString();
    var resultList = new List<OracleDemoItem>();

    // 2. 建立連線 (使用 await using 確保安全釋放)
    await using (OracleConnection connection = new OracleConnection(connectionString))
    {
        // 3. 開啟連線 (必須使用 Async 版本)
        await connection.OpenAsync();

        // 4. 建立 Command
        await using (OracleCommand command = connection.CreateCommand())
        {
            // 5. 撰寫 SQL 語法
            // 注意：Oracle 如果透過 EF Core 建表，欄位會有雙引號，因此手寫 SQL 也要加上雙引號。
            command.CommandText = "SELECT \"Id\", \"Name\", \"CreatedAt\", \"Description\" FROM \"OracleDemoItems\"";

            // 6. 執行查詢並取得 Reader
            await using (var reader = await command.ExecuteReaderAsync())
            {
                // 7. 逐筆讀取資料
                while (await reader.ReadAsync())
                {
                    resultList.Add(new OracleDemoItem
                    {
                        // 透過索引值取資料效能最好
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        CreatedAt = reader.GetDateTime(2),
                        
                        // 處理可能為 NULL 的欄位，必須先呼叫 IsDBNull 檢查
                        Description = reader.IsDBNull(3) ? null : reader.GetString(3)
                    });
                }
            }
        }
    }

    return resultList;
}
```

---

## 5. ⚠️ 撰寫時的必備規範與陷阱 (Deep Check Rules)

在專案中撰寫原生 ADO.NET 時，**必須嚴格遵守以下規範**，否則極易造成系統崩潰或記憶體洩漏：

### 🚨 規則一：強制使用 `await using` 包覆 `IDisposable`
`OracleConnection`、`OracleCommand` 與 `DataReader` 都實作了 `IDisposable`。
資料庫連線是非常昂貴的資源，如果沒有確實釋放，Connection Pool 會迅速耗盡 (Timeout)。
- **❌ 錯誤寫法**：`var conn = new OracleConnection(...);`
- **✅ 正確寫法**：`await using (var conn = new OracleConnection(...)) { ... }`

### 🚨 規則二：非同步優先 (Async First)
為了避免在資料存取時阻塞伺服器的執行緒 (Thread Starvation)，所有 I/O 操作必須是非同步的。
- **❌ 錯誤寫法**：`conn.Open()`, `command.ExecuteReader()`, `reader.Read()`
- **✅ 正確寫法**：`await conn.OpenAsync()`, `await command.ExecuteReaderAsync()`, `await reader.ReadAsync()`

### 🚨 規則三：小心 Oracle 的大小寫區分
預設情況下，Oracle 的 Table 與 Column 是不分大小寫的 (全部視為大寫)。
但在 .NET 中，透過 EF Core Code-First 產生的資料表，通常會被加上雙引號以保護 CamelCase 命名。
這意味著手寫 SQL 時，`SELECT Id FROM OracleDemoItems` 會失敗 (`ORA-00904: invalid identifier`)。
- **✅ 正確寫法**：必須加上雙引號 `SELECT \"Id\" FROM \"OracleDemoItems\"`。

### 🚨 規則四：處理 NULL 值 (`IsDBNull`)
在使用 `DataReader` 讀取資料時，如果資料庫裡的該欄位是 `NULL`，直接呼叫 `reader.GetString()` 會引發 `SqlNullValueException` 或強轉失敗。
- **❌ 錯誤寫法**：`Description = reader.GetString(3)`
- **✅ 正確寫法**：`Description = reader.IsDBNull(3) ? null : reader.GetString(3)`

### 🚨 規則五：防止 SQL Injection (必須使用參數化查詢)
當 SQL 語句中需要動態帶入使用者的輸入條件時，**絕對禁止使用字串拼接**。必須使用 `OracleParameter` 來進行參數化查詢，否則會面臨嚴重的 SQL Injection (隱碼攻擊) 風險。
- **❌ 錯誤寫法**：`command.CommandText = $"SELECT * FROM \"Users\" WHERE \"Name\" = '{userName}'";`
- **✅ 正確寫法**：
  ```csharp
  command.CommandText = "SELECT * FROM \"Users\" WHERE \"Name\" = :name";
  command.Parameters.Add(new OracleParameter("name", userName));
  ```

### 🚨 規則六：重複使用 Command 時必須清空或更新 Parameters
在同一個方法中，如果您重用同一個 `OracleCommand` 物件來執行多個不同的 SQL 語句，**必須注意清除舊的參數**，或者使用更新參數值的方式，否則執行第二個 SQL 時可能會因為帶入前一個指令的參數而發生錯誤。
- **❌ 錯誤寫法** (參數累積導致錯誤)：
  ```csharp
  command.CommandText = "SELECT * FROM \"Users\" WHERE \"Id\" = :id";
  command.Parameters.Add(new OracleParameter("id", 1));
  await command.ExecuteReaderAsync();

  // 忘記清空 Parameters，第二個查詢會連同 id 參數一起送出！
  command.CommandText = "SELECT * FROM \"Orders\" WHERE \"UserId\" = :userId";
  command.Parameters.Add(new OracleParameter("userId", 1));
  await command.ExecuteReaderAsync();
  ```
- **✅ 正確寫法 1** (使用 `Parameters.Clear()`)：
  ```csharp
  command.Parameters.Clear(); // 執行新查詢前先清空參數
  command.CommandText = "SELECT * FROM ...";
  command.Parameters.Add(...);
  ```
- **✅ 正確寫法 2** (保留參數定義，僅更新值)：
  ```csharp
  command.Parameters["id"].Value = 2; // 若 SQL 相同，只需更新值即可重用
  await command.ExecuteReaderAsync();
  ```

---

## 6. 其他常用操作

除了 `ExecuteReaderAsync` (用於 SELECT 查詢回傳多筆)，還有兩種常見的 Command 執行方式：

1. **`ExecuteScalarAsync`**：
   用於只回傳「單一值」的查詢，例如 COUNT、SUM 等聚合函數。
   ```csharp
   command.CommandText = "SELECT COUNT(*) FROM \"OracleDemoItems\"";
   var count = Convert.ToInt32(await command.ExecuteScalarAsync());
   ```

2. **`ExecuteNonQueryAsync`**：
   用於執行 INSERT、UPDATE、DELETE 等不需要回傳資料表，只需回傳「受影響行數」的指令。
   ```csharp
   command.CommandText = "UPDATE \"OracleDemoItems\" SET \"Name\" = 'Test' WHERE \"Id\" = 1";
   int affectedRows = await command.ExecuteNonQueryAsync();
   ```

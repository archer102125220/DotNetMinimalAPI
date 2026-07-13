# 預存程序 (Stored Procedures) 與 EF Core 的整合指南

現代開發環境中，使用 ORM (如 Entity Framework Core) 進行資料存取已是主流，這使得傳統的「預存程序 (SQL Stored Procedures)」逐漸變成針對「極端情況」的手段，而非標準配置。

本指南記錄了為何現代專案較少使用預存程序，以及在不得已或特殊需求下，如何將預存程序融入 EF Core 的 Code-First 開發流程中，以確保版控與維護的便利性。

## 為什麼現代開發較少使用預存程序？

1. **商業邏輯分散 (Logic Fragmentation)**：如果一部分邏輯寫在 C#，一部分寫在 DB 預存程序，會導致系統難以追蹤、除錯與測試。
2. **缺乏強型別與版控**：預存程序是純 SQL 字串，開發階段編譯器無法幫你檢查語法或型別錯誤。雖然能將腳本放入版控，但在 CI/CD 自動化佈署與降版 (Rollback) 上，相對 ORM 的 Code-First Migration 較不直覺。
3. **資料庫綁定 (Vendor Lock-in)**：不同廠牌的預存程序（如 Oracle 的 PL/SQL, SQL Server 的 T-SQL）語法差異極大，一旦大量依賴，系統未來切換資料庫引擎的成本會非常高。
4. **ORM 已經足夠強大**：絕大多數的 CRUD 與關聯查詢，EF Core 的 LINQ 都能轉譯出非常高效的 SQL，也能妥善利用 Parameterized Query 避免 SQL Injection。

**何時才建議使用預存程序？**
* 需要將海量資料在資料庫內部進行複雜運算與過濾（避免將百萬筆資料拉回 Web Server 記憶體做處理）。
* 歷史包袱（繼承舊有系統，必須呼叫既有 DB 端寫好的 Procedure）。
* 極端的效能瓶頸（極少數情況下 ORM 產生的 SQL 無法滿足效能要求，需要 DBA 針對特定查詢進行手工調校與快取）。

---

## 實作範例：在 EF Core 中管理與呼叫預存程序

在 `DotNetMinimalAPI` 專案中，我們實作了一個以 Oracle 為例的預存程序 `SP_UPDATE_ITEM_DESCRIPTION` 來展示如何兼顧管理與維護。

### 1. 使用 EF Core Migrations 管理預存程序 (推薦做法)

雖然預存程序是純 SQL，但為了保證團隊所有人的資料庫 schema 狀態一致，**強烈建議將預存程序的建立腳本放入 EF Core Migration 中**，讓它跟著 Schema 版本一起推進。

#### 建立空白 Migration
```bash
dotnet ef migrations add AddUpdateDescriptionProcedure --context AppDbContext
```

#### 撰寫 Migration 內容 (`Up` / `Down`)
在產生的 Migration 檔案中，使用 `migrationBuilder.Sql()` 來執行 DDL (Data Definition Language)：

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

namespace DotNetMinimalAPI.Migrations
{
    public partial class AddUpdateDescriptionProcedure : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 注意：Oracle 的寫法與 SQL Server 不同
            var sql = @"
CREATE OR REPLACE PROCEDURE SP_UPDATE_ITEM_DESCRIPTION (
    p_Id IN NUMBER,
    p_Description IN NVARCHAR2
)
AS
BEGIN
    UPDATE ""OracleDemoItems""
    SET ""Description"" = p_Description,
        ""UpdatedAt"" = CURRENT_TIMESTAMP
    WHERE ""Id"" = p_Id;
END;";
            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE SP_UPDATE_ITEM_DESCRIPTION");
        }
    }
}
```

> [!WARNING]
> **手動建立 Migration 檔案的常見錯誤 (Gotcha)**  
> 如果您是手動新增 `.cs` 檔案而非使用 CLI 產生，請務必補上 **`[DbContext(typeof(AppDbContext))]`** 與 **`[Migration("時間戳_名稱")]`** 這兩個 Attribute。若遺漏，EF Core CLI 將無法識別此 Migration，導致 `dotnet ef database update` 會直接略過它並顯示「Already up to date」。

### 2. 建立獨立的 `.sql` 檔案備查 (最佳實踐)

雖然最終的佈署與執行是依賴 EF Core Migration，但將一大包 SQL 字串寫死在 C# 檔案中會失去 SQL 語法高亮 (Syntax Highlighting)，也不利於 DBA (資料庫管理員) 進行 Review。

因此，**強烈建議在專案中建立專屬的 SQL 目錄**，並將純 SQL 語法儲存為 `.sql` 檔案作為備查與開發之用。

例如，建立 `Database/Procedures/Oracle/SP_UPDATE_ITEM_DESCRIPTION.sql`：
```sql
CREATE OR REPLACE PROCEDURE SP_UPDATE_ITEM_DESCRIPTION (
    p_Id IN NUMBER,
    p_Description IN NVARCHAR2
)
AS
BEGIN
    UPDATE "OracleDemoItems"
    SET "Description" = p_Description,
        "UpdatedAt" = CURRENT_TIMESTAMP
    WHERE "Id" = p_Id;
END;
```

這樣做的好處：
* 享有 IDE 原生的 SQL 語法檢查與高亮，減少在 C# 字串中拼錯字的風險。
* 方便 DBA 單獨檢視、測試與優化 SQL 邏輯。
* 開發者可以先在 DataGrip / DBeaver 等工具中測試 `.sql` 檔，確認無誤後，再將內容複製到 Migration 的 `migrationBuilder.Sql()` 中執行。

### 3. 在 C# 中呼叫預存程序

EF Core 提供了兩種主要方式來呼叫預存程序：
* `FromSqlRaw` / `FromSqlInterpolated`：用於預存程序會**回傳資料集 (Result Sets)**，且回傳欄位能與某個 Entity 結構對應時。
* `ExecuteSqlRawAsync` / `ExecuteSqlInterpolatedAsync`：用於**不回傳結果集**的操作（例如單純執行 `UPDATE`, `DELETE`, `INSERT` 或批次作業）。

以更新 `Description` 的預存程序為例，因為不需要回傳結果集，我們使用 `ExecuteSqlRawAsync`：

```csharp
public async Task UpdateItemDescriptionViaProcAsync(int id, string newDescription)
{
    // 使用 ExecuteSqlRawAsync 執行 Procedure
    // 注意參數的繫結方式會因資料庫類型有所不同 (例如 Oracle 使用 :p0，SQL Server 使用 @p0)
    await _context.Database.ExecuteSqlRawAsync(
        "BEGIN SP_UPDATE_ITEM_DESCRIPTION(:p0, :p1); END;", 
        id, 
        newDescription ?? (object)DBNull.Value
    );
}
```

> [!NOTE]
> 1. 在呼叫 Oracle 的 Procedure 時，通常需要包裹在 `BEGIN ... END;` 的匿名區塊中。
> 2. 如果傳入的變數可能為 `null`，必須明確轉換為 `DBNull.Value` (`newDescription ?? (object)DBNull.Value`)，才能正確傳遞 Null 給資料庫，否則會報錯。

### 4. UI 整合考量

無論底層是使用 EF Core 的標準物件追蹤 (Tracking) 來更新資料，還是直接呼叫預存程序，前端控制器 (Controller) 與 UI 的實作都可以保持不變。
在本專案中，我們利用 **HTMX** 以非同步方式 (`hx-post`) 呼叫 Controller 端點，讓預存程序的執行過程對使用者完全透明，並搭配了「雙擊編輯 (Double-click to edit)」的互動體驗，展現了現代化架構的低耦合優勢。

---

### 5. 各資料庫預存程序 (Stored Procedures) 語法與呼叫差異比較

在相同的業務需求下，不同的關聯式資料庫 (RDBMS) 在「建立」與「呼叫」預存程序時，會有些微的語法差異。以下是本專案支援的四種資料庫之差異總結：

#### 1. SQL 建立語法 (DDL) 差異

*   **Oracle**
    *   **建立**: 支援 `CREATE OR REPLACE PROCEDURE`。
    *   **參數**: 使用 `IN`、`OUT` 修飾詞，型別如 `NUMBER`、`NVARCHAR2`。
    *   **主體**: 使用 `AS BEGIN ... END;`。
*   **MSSQL (SQL Server)**
    *   **建立**: 支援 `CREATE OR ALTER PROCEDURE`。
    *   **參數**: 變數名稱必須加上 `@` 前綴（如 `@p_Id`），型別如 `INT`、`NVARCHAR(MAX)`。
    *   **主體**: 使用 `AS BEGIN ... END`。
*   **PostgreSQL**
    *   **建立**: 支援 `CREATE OR REPLACE PROCEDURE`。
    *   **參數**: 型別如 `integer`、`character varying`。
    *   **主體**: 必須指定語言 `LANGUAGE plpgsql`，並將主體包在 `AS $$ BEGIN ... END; $$;` 區塊中。
*   **MySQL**
    *   **建立**: **不支援** `OR REPLACE`，通常在 Migration 中需先執行 `DROP PROCEDURE IF EXISTS`，再執行 `CREATE PROCEDURE`。
    *   **參數**: 使用 `IN` 修飾詞，型別如 `INT`、`TEXT`。
    *   **主體**: 使用 `BEGIN ... END`。

#### 2. C# EF Core 呼叫語法差異

透過 EF Core 的 `ExecuteSqlRawAsync` 或是 `ExecuteSqlInterpolatedAsync` 呼叫時，各家資料庫所接受的 SQL 命令也不同：

*   **Oracle**: 必須包裝在 PL/SQL 匿名區塊中。
    ```csharp
    await _context.Database.ExecuteSqlRawAsync("BEGIN SP_UPDATE_ITEM_DESCRIPTION(:p0, :p1); END;", id, newDescription);
    ```
*   **MSSQL (SQL Server)**: 使用 `EXEC` 指令。
    ```csharp
    await _context.Database.ExecuteSqlInterpolatedAsync($"EXEC SP_UPDATE_ITEM_DESCRIPTION {id}, {newDescription}");
    ```
*   **PostgreSQL**: (PostgreSQL 11 之後引入的 Procedure) 使用 `CALL` 指令。
    ```csharp
    await _context.Database.ExecuteSqlInterpolatedAsync($"CALL SP_UPDATE_ITEM_DESCRIPTION({id}, {newDescription})");
    ```
*   **MySQL**: 同樣使用 `CALL` 指令。
    ```csharp
    await _context.Database.ExecuteSqlInterpolatedAsync($"CALL SP_UPDATE_ITEM_DESCRIPTION({id}, {newDescription})");
    ```

> [!TIP]
> 建議在 C# 端呼叫時，優先使用 `ExecuteSqlInterpolatedAsync($"...")` 的字串插值語法。EF Core 會自動將大括號 `{}` 內的變數轉換為對應資料庫的安全參數 (Parameterized Query)，能有效防範 SQL Injection 攻擊，並且不用手動處理 `:p0` 或是 `@p0` 的差異。

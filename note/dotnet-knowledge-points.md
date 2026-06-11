# .NET 開發知識要點

這裡記錄了在開發 C# 與 ASP.NET Core 專案時，常見的觀念、小知識與語法細節。

## XML 註解與 `/// <inheritdoc />`

在 C# 中，我們經常使用以 `///` 開頭的 XML 註解來撰寫說明文件，這些註解會被編譯器收集，並讓 IDE (如 Visual Studio / VS Code) 能夠顯示 IntelliSense 提示。

### 什麼是 `<inheritdoc />`？

`/// <inheritdoc />` 是一個特殊的 XML 標籤，意思是 **「繼承 (Inherit) 父類別或介面 (Doc) 的註解」**。

當你實作介面或複寫 (override) 基礎類別的方法時，你不必重寫一次註解。只要加上這個標籤，IDE 就會自動去抓取原始介面或父類別上寫的說明。

```csharp
public class MyBaseClass
{
    /// <summary>
    /// 這是一個基礎方法的說明。
    /// </summary>
    public virtual void DoSomething() { }
}

public class MyChildClass : MyBaseClass
{
    /// <inheritdoc />
    public override void DoSomething() { } 
    // 當滑鼠移到這裡，IDE 會顯示「這是一個基礎方法的說明。」
}
```

### 替換或刪除會有影響嗎？

**完全不會有任何程式執行或功能上的影響。**

如果你覺得父類別預設的註解 (例如 EF Core 自動生成的 Migration 檔案中那些英文註解) 不夠清楚，你可以隨時把 `<inheritdoc />` 刪除，並換成你自己寫的 `<summary>`。

這唯一的改變就是：IDE 顯示的提示會變成你自訂的內容，反而有助於團隊開發或未來回顧程式碼。

## EF Core：重啟程式會自動建立資料表嗎？

許多新手在使用 Entity Framework Core 時會有個常見的誤解：「既然我在 `Program.cs` 裡面呼叫了 Seed Data (種子資料) 的初始化邏輯，如果資料庫或是資料表不存在，重新啟動程式 (`dotnet watch run`) 會自動幫我建立出來嗎？」

**答案是：預設情況下，不會。**

### 為什麼不自動建立？
ASP.NET Core 的設計理念在於安全與職責分離。資料庫結構（Schema）的變更是非常嚴肅且具風險的操作。如果應用程式每次重啟都會嘗試修改資料庫，在正式環境中可能會引發災難性的後果（例如不小心覆蓋了資料）。

### EF Core 建立資料表的正規兩步驟流程

如果您使用 **Code-First**（程式碼優先）來設計資料庫，建立資料表分為明確的兩階段，這兩步通常都必須透過命令列 (CLI) 手動執行：

1. **產生藍圖 (`dotnet ef migrations add <名稱>`)**
   這一步會讀取您的 C# 實體模型 (`Models`)，並翻譯成準備用來建立或修改資料庫結構的 C# 腳本檔案（位於 `Migrations` 資料夾內）。這只是在本地產生「設計圖」，**資料庫本身此時還沒有任何變化**。

2. **正式施工 (`dotnet ef database update`)**
   這一步會把剛才建立好的所有「設計圖」實際推送到資料庫伺服器（例如 MySQL、Oracle），執行真正的 SQL 語法（例如 `CREATE TABLE`）。**直到執行完這一步，資料表才會真正誕生**。

### 常見錯誤情境 (`Table doesn't exist`)
如果您只有執行了 `migrations add` 產生設計圖，接著就直接重啟應用程式 (`dotnet watch run`)，這時如果您的應用程式（例如在 `DbInitializer.cs` 裡）去對資料庫執行查詢或新增資料，資料庫就會無情地回報錯誤：

> `MySqlException: Table 'YourDatabase.YourTable' doesn't exist`

**解決方案**非常簡單：打開終端機，執行一次 `dotnet ef database update`（如果有多個 DbContext，記得加上 `-c YourDbContextName` 指定），把資料表建好之後，應用程式就能正常寫入資料了！

## C# 字串排版與組合語法演進

在 C# 開發中，組合字串或是撰寫多行 SQL 語法（如在 `OracleDemoController` 第 279 行所見的寫法）有非常多種方式，以下整理了常見的寫法以及它們分別支援的 C# 與 .NET 版本：

### 1. 字串串接 (String Concatenation) `+`
最基本、傳統的字串相加。不支援多行，寫多行時每行都要加上 `+`，容易看錯。
*   **支援版本**：C# 1.0 / .NET Framework 1.0 起
*   **寫法**：
    ```csharp
    string sql = "SELECT * " +
                 "FROM Users " +
                 "WHERE Id = " + userId;
    ```

### 2. 字串格式化 `string.Format`
使用 `{0}`, `{1}` 作為佔位符，將變數代入字串中，較 `+` 清楚，但遇到大量變數時容易對錯索引位置。
*   **支援版本**：C# 1.0 / .NET Framework 1.0 起
*   **寫法**：
    ```csharp
    string sql = string.Format("SELECT * FROM Users WHERE Id = {0}", userId);
    ```

### 3. 逐字字串 (Verbatim String Literal) `@""`
字串開頭加上 `@`。支援多行輸入，且不需要跳脫反斜線 `\`。但如果字串內本身有雙引號 `"`，需要寫兩個雙引號 `""` 來跳脫。
*   **支援版本**：C# 1.0 / .NET Framework 1.0 起
*   **寫法**：
    ```csharp
    string sql = @"
        SELECT *
        FROM Users
        WHERE Name = ""John""
    ";
    ```

### 4. 字串插值 (String Interpolation) `$""`
字串開頭加上 `$`。允許直接在 `{}` 裡面寫變數或 C# 運算式，大幅提高可讀性，取代了 `string.Format` 的寫法。
*   **支援版本**：C# 6.0 / .NET Framework 4.6 / .NET Core 1.0 起
*   **寫法**：
    ```csharp
    string sql = $"SELECT * FROM Users WHERE Id = {userId}";
    ```

### 5. 逐字字串插值 (Interpolated Verbatim String) `$@""` 或 `@$""`
同時使用 `$` 與 `@`，結合了「變數代入」與「多行支援 / 忽略反斜線跳脫」的優點。
*(註：C# 6 只能用 `$@`，C# 8 開始支援 `@$` 兩種順序互換)*
*   **支援版本**：C# 6.0 / .NET Framework 4.6 / .NET Core 1.0 起
*   **寫法**：
    ```csharp
    string sql = $@"
        SELECT *
        FROM Users
        WHERE Id = {userId} AND Name = ""John""
    ";
    ```

### 6. 原始字串常值 (Raw String Literal) `"""..."""`
使用至少三個雙引號 `"""` 包起來。完美解決多行字串、排版縮排、以及內部引號跳脫的問題。
*   **解決縮排問題**：編譯器會自動以結尾 `"""` 所在的縮排位置為基準，自動切掉前方多餘的空白。
*   **解決雙引號問題**：內部可以直接使用單一雙引號 `"`，不需再寫成 `""` 跳脫（這就是為什麼在 `OracleDemoController` 裡寫 Oracle SQL 需要雙引號欄位時非常方便）。
*   **支援版本**：C# 11 / .NET 7 起
*   **寫法**：
    ```csharp
    string sql = """
        SELECT "Id", "Name"
        FROM "Users"
        """;
    ```

### 7. 原始字串插值 (Interpolated Raw String Literal) `$$"""..."""`
在原始字串前方加上 `$`。
如果是兩個 `$$`，則代表裡面遇到兩個大括號 `{{ }}` 才是變數代入，單一個大括號 `{ }` 則會被當作普通字元輸出。這在組合 JSON 或帶有大括號的 SQL 語法時特別有用。
*   **支援版本**：C# 11 / .NET 7 起
*   **寫法**：
    ```csharp
    string json = $$"""
        {
            "Id": {{userId}},
            "Name": "John"
        }
        """;
    ```

### 8. 動態字串組合利器 `StringBuilder`
在 C# 中，字串 (`string`) 是**不可變的 (Immutable)**。這代表每次使用 `+` 或 `+=` 修改字串時，系統都會在記憶體中配置一塊新空間來存放新字串，並把舊字串丟棄交由垃圾回收器 (GC) 處理。如果只是組合兩三個變數，這不會有影響；但如果在**迴圈中**或是**大量條件分支中**不斷拼接字串，會造成嚴重的效能與記憶體負擔。

`StringBuilder` (位於 `System.Text` 命名空間) 則是**可變的字串緩衝區**。它會在內部維護一個字元陣列，當你呼叫 `.Append()` 或 `.AppendLine()` 時，它會直接修改內部陣列內容，不會一直產生新的字串物件，效能極佳。

*   **使用時機**：在迴圈內組合大量字串、或是需要根據大量複雜條件動態拼湊超長字串（如動態 SQL 條件）時。若只是單純一行內組合少量變數，請直接用字串插值 (`$""`) 即可。
*   **支援版本**：C# 1.0 / .NET Framework 1.0 起
*   **寫法**：
    ```csharp
    using System.Text;

    // 建立一個 StringBuilder 物件
    StringBuilder sb = new StringBuilder();

    sb.AppendLine("SELECT * FROM Users WHERE 1=1");

    if (hasNameFilter)
    {
        sb.AppendLine("AND Name = @Name");
    }

    if (hasAgeFilter)
    {
        sb.AppendLine("AND Age > @Age");
    }

    // 最後呼叫 ToString() 轉回一般 string
    string sql = sb.ToString();
    ```

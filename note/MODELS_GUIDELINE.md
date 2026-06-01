# ASP.NET Core Minimal API: 資料模型 (Models) 與 EF Core 開發指南

本指南整合了專案中關於 Entity Framework Core (EF Core) 的安裝設定、資料模型 (`Models` 目錄) 的結構規範，以及深度檢查政策 (Deep Check Policy)。為維持專案架構清晰、安全且高效，請所有開發者嚴格遵守。

---

## 🛢️ 第一部分：Entity Framework Core (EF Core) 基礎安裝與連線設定

在定義資料庫實體模型 (Entities) 之前，必須先確保專案已正確安裝與配置 EF Core。

### 1. 基礎安裝
若專案尚未設定 EF Core，可依下列步驟透過 .NET CLI 完成核心套件與相關工具的安裝：

```bash
# 核心套件與 EF Core 工具
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.EntityFrameworkCore.Tools

# 資料庫 Provider (本專案預設以 Oracle 為例)
dotnet add package Oracle.EntityFrameworkCore
# 若使用 SQL Server 則為: dotnet add package Microsoft.EntityFrameworkCore.SqlServer
# 若使用 PostgreSQL 則為: dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
# 若使用 MySQL 則為: dotnet add package Pomelo.EntityFrameworkCore.MySql
# 若使用 SQLite 則為: dotnet add package Microsoft.EntityFrameworkCore.Sqlite
```

> [!NOTE]
> **關於 MySQL (Pomelo) 的 NU1608 警告**
> 在較新的 .NET / EF Core 版本（例如 EF Core 10）中安裝 `Pomelo.EntityFrameworkCore.MySql` 時，編譯時可能會遇到 `NU1608` 警告。這是因為 Pomelo 官方更新進度通常稍微落後於 EF Core 的最新主要版本。這屬於已知現象，通常不會影響基礎功能運作。待 Pomelo 釋出對應的 EF Core 10 版本後，再透過 NuGet 更新套件即可消除此警告。

### 2. 資料庫連線設定

**設定 `appsettings.json`：**
本專案支援多種資料庫，您可以依據環境選擇對應的連線字串。以下提供四大資料庫的設定範例（皆使用預設的 `dot-net-minimal-api` 帳號）：

```json
{
  "ConnectionStrings": {
    "OracleConnection": "User Id=dot-net-minimal-api;Password=DotNetMinimalAPIAbc123;Data Source=localhost:1521/FREEPDB1;",
    "SqlServerConnection": "Server=localhost,1434;Database=DotNetMinimalDb;User Id=dot-net-minimal-api;Password=DotNetMinimalAPIAbc123;TrustServerCertificate=True;",
    "PostgresConnection": "Host=localhost;Port=5432;Database=DotNetMinimalDb;Username=dot-net-minimal-api;Password=DotNetMinimalAPIAbc123;",
    "MySqlConnection": "Server=localhost;Port=3306;Database=DotNetMinimalDb;User=dot-net-minimal-api;Password=DotNetMinimalAPIAbc123;"
  }
}
```

**建立 DbContext：**
在 `Models` 或是 `Data` 資料夾中建立繼承自 `DbContext` 的類別（例如 `AppDbContext`）：
```csharp
using Microsoft.EntityFrameworkCore;

namespace DotNetMinimalAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // 這裡定義 DbQuery 或是 DbSet
    // public DbSet<User> Users { get; set; } = null!;
}
```

**註冊 DbContext (`Program.cs`)：**
在 `Program.cs` 檔案中，將 `AppDbContext` 註冊至依賴注入容器中：
```csharp
using Microsoft.EntityFrameworkCore;
using DotNetMinimalAPI.Data;

var builder = WebApplication.CreateBuilder(args);

// 加入 DbContext 註冊
// 請依照實際使用的資料庫，選擇對應的方法：

// 1. Oracle (預設)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("OracleConnection")));

// 2. SQL Server
// builder.Services.AddDbContext<AppDbContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServerConnection")));

// 3. PostgreSQL
// builder.Services.AddDbContext<AppDbContext>(options =>
//     options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));

// 4. MySQL
// builder.Services.AddDbContext<AppDbContext>(options =>
// {
//     var connectionString = builder.Configuration.GetConnectionString("MySqlConnection");
//     options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
// });

// ... 其他服務註冊

var app = builder.Build();
```

---

## 📂 第二部分：Models 目錄結構與職責

完成 ORM 設定後，我們應將不同用途的模型妥善分類放置於 `Models` 資料夾下：

### 1. `Entities/` (實體模型)
- **用途**：代表資料庫的資料表結構，與 EF Core 直接對應。
- **規則**：
  - **絕對不可以**直接將 Entity 回傳給 API 客戶端，這會導致敏感資料（如密碼、雜湊值）外洩或引發 JSON 遞迴序列化問題。
  - 僅在 `Data` (DbContext) 或 `Services` 層級中進行操作。

### 2. `DTOs/` (資料傳輸物件)
- **用途**：專門用於 API 請求與回應，以及 Service 層與 Endpoints 之間的資料傳遞。
- **規則**：
  - **永遠不要**將 Entity 直接當作請求或回應。
  - 每個 API 端點建議有專屬的 DTO（例如 `UserRegisterRequest`, `UserProfileResponse`）。
  - 結構應保持扁平，僅包含傳輸所需的純資料。
  - 可以透過第三方套件（如 FluentValidation）或手動邏輯進行 API 的輸入驗證。

---

## 🛡️ 第三部分：核心開發規範 (Endpoint-DTO 模式)

### 1. Thin Endpoints 模式
- **Always use DTOs**：Route handler (Endpoint) 的職責是從 Service 取得資料 (Entity / 內部 DTO)，將其轉換為對外的 Response DTO，然後回傳。
- ❌ **錯誤示範**：直接回傳資料庫實體
  ```csharp
  app.MapGet("/users/{userId}", async (int userId, AppDbContext dbContext) => 
  {
      User? user = await dbContext.Users.FindAsync(userId);
      return Results.Ok(user); // ❌ 危險！可能外洩機密資料，且易引發 JSON 序列化遞迴問題
  });
  ```
- ✅ **正確示範**：轉換為 DTO
  ```csharp
  app.MapGet("/users/{userId}", async (int userId, IUserService userService) => 
  {
      var user = await userService.GetUserAsync(userId);
      var dto = new UserProfileResponse {
          Username = user.Username,
          Email = user.Email
      };
      return Results.Ok(dto); // ✅ 安全
  });
  ```

### 2. 型別安全與 Nullable Reference Types
- 專案已啟用 `<Nullable>enable</Nullable>`，請**務必**正確處理 Null。
- 如果某個字串或物件允許為空，請標記為 `?`。
- 若不允許為空但在建構時尚未賦值，可使用 `required` 關鍵字（C# 11+）或給予預設值：
  ```csharp
  public required string Title { get; set; }
  public string Content { get; set; } = string.Empty;
  ```

### 3. 執行時期資料驗證 (Runtime Data Validation)
Minimal APIs **預設不支援** `[Required]` 等 Data Annotations 的自動 `ModelState` 驗證。為了達成有效的驗證，我們建議採用以下幾種方式：

- **(推薦) 使用 FluentValidation**：建立獨立的 Validator 類別，搭配 EndpointFilter 進行自動驗證。
- **(簡單) 手動驗證**：在 Endpoint 內直接使用 `if` 判斷並回傳 `Results.BadRequest()`。
- **(進階) 自訂 EndpointFilter**：撰寫通用的 Filter 來攔截 Request 並執行 Data Annotations 或 FluentValidation。

**FluentValidation 搭配 EndpointFilter 範例：**
```csharp
// 1. 定義 DTO 與 Validator
public class UserRegisterRequest
{
    public required string Username { get; set; }
    public required string Email { get; set; }
}

public class UserRegisterValidator : AbstractValidator<UserRegisterRequest>
{
    public UserRegisterValidator()
    {
        RuleFor(x => x.Username).NotEmpty().Length(3, 50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

// 2. 註冊與使用
app.MapPost("/users/register", (UserRegisterRequest req) => 
{
    // 處理註冊邏輯...
    return Results.Ok();
})
.AddEndpointFilter<ValidationFilter<UserRegisterRequest>>(); // 加入驗證 Filter
```

### 4. 避免動態與弱型別
- **嚴禁使用** `dynamic` 或 `object`（除非必須使用 Reflection 或處理未知結構的 JSON）。

---

## 🚧 第四部分：資料庫遷移 (Migrations) 與深度檢查政策

### 1. 資料庫遷移 (Migrations) 安全規範

⚠️ **資料庫修改確認規範 (CRITICAL)**：
在執行任何 Schema 變更前，**必須確認環境是否已部署至正式環境 (Production)**。
- **未部署**：可以刪除未套用的 migration 或刪除資料庫重新建立 (`dotnet ef database drop`, `dotnet ef database update`)。
- **已部署**：**絕對禁止**修改已執行過的 migrations，必須建立**新的** migration 檔案。

**常用指令：**
```bash
dotnet ef migrations add <MigrationName>
dotnet ef database update
dotnet ef migrations remove
```

### 2. EF Core 開發深度檢查政策 (Deep Check)

開發或審查包含 EF Core 相關程式碼時，必須遵守以下安全與效能檢查標準：

#### Round 1: 表面檢查 (Basic Check)
- ✅ 標準語法及 namespace 引用是否正確。
- ✅ 透過依賴注入 (DI) 取得 `AppDbContext` (禁止使用 `new AppDbContext()`)。
- ✅ 變數命名與基本的 Null 檢查。

#### Round 2: 深度檢查 (Deep Check) - ⚠️ MANDATORY
撰寫或修改 EF Core 查詢時，請務必避免以下 Anti-Patterns：

| 錯誤模式 (Anti-Pattern) | 正確做法 (Correct Pattern) | 優先級 |
|--------------|----------------|----------|
| 遺漏 `await` 或是不正確的回傳 Task | 必須明確加上 `await` 處理非同步呼叫 | 🔴 High |
| 迴圈中發生 N+1 查詢問題 | 使用 `.Include()`、`.Select()` 或在迴圈外先行批次讀取 | 🔴 High |
| 未釋放的 `IDisposable` (Streams, HttpClients) | 使用 `using (...) { }` 或 `using var obj = ...;` 包覆 | 🔴 High |
| 使用同步的 DB 呼叫 (`.ToList()`) | 必須使用非同步版本：`await .ToListAsync()` | 🟡 Medium |
| 針對唯讀操作追蹤實體 (Tracking) | 加上 `.AsNoTracking()` | 🟡 Medium |

**正確的唯讀查詢範例：**
```csharp
public async Task<List<User>> GetActiveUsersAsync()
{
    return await _context.Users
        .AsNoTracking() // 唯讀時不追蹤實體，提升效能
        .Where(u => u.IsActive)
        .ToListAsync(); // 使用非同步方法
}
```

---

## 🎯 總結檢查清單 (Checklist)
- [ ] 專案已確實安裝與設定 EF Core，並於 `Program.cs` 中註冊 DbContext。
- [ ] 實體 (Entity) 是否被隔離，沒有直接回傳給 API 客戶端？
- [ ] DTO 的命名是否明確 (例如加上 Request/Response 後綴)？
- [ ] 屬性的 Nullable (`?`) 標示是否精確？
- [ ] 是否已針對 Minimal API 環境建立適合的資料驗證機制（如 FluentValidation）？
- [ ] 所有 EF Core 查詢是否都採用非同步 (`await`)？
- [ ] 唯讀的查詢是否都加上了 `.AsNoTracking()`？

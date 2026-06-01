# JWT 驗證與 System.IdentityModel.Tokens.Jwt 教學筆記

這份筆記記錄了如何在 .NET (特別是 ASP.NET Core Minimal API) 專案中引入 `System.IdentityModel.Tokens.Jwt` 套件，以及如何實作基於 JSON Web Token (JWT) 的驗證機制。

## 1. 簡介

`System.IdentityModel.Tokens.Jwt` 是微軟官方提供用於處理 JSON Web Tokens (JWT) 的核心套件。它包含了建立、序列化、反序列化以及驗證 JWT 的所有必要類別。

在現代 Web 應用程式中，JWT 經常被用來：
*   **無狀態驗證 (Stateless Authentication)**：API 伺服器不需要在 Session 中儲存使用者的登入狀態。
*   **授權 (Authorization)**：Token 中可以夾帶使用者的角色 (Roles) 或宣告 (Claims)，方便伺服器端驗證權限。
*   **單一登入 (SSO)**：在多個微服務或應用程式間共用驗證狀態。

## 2. 安裝套件

在 .NET 專案中，你可以透過 .NET CLI 來安裝此套件。

```bash
# 切換到你的專案目錄
cd /path/to/your/project

# 安裝 JWT 套件
dotnet add package System.IdentityModel.Tokens.Jwt
```

> **注意**：如果你是要在 ASP.NET Core 中實作 JWT 驗證，通常還會需要安裝 `Microsoft.AspNetCore.Authentication.JwtBearer`，因為它包含了 ASP.NET Core 驗證中介軟體 (Middleware) 的實作。`Microsoft.AspNetCore.Authentication.JwtBearer` 內部會相依於 `System.IdentityModel.Tokens.Jwt`。
> 
> ```bash
> dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
> ```

## 3. 基本設定與實作流程

### 3.1 準備金鑰與設定 (appsettings.json)

首先，在你的 `appsettings.json` 中加入 JWT 相關的設定參數。請確保 `Key` 夠長且複雜 (至少 16 字元以上，建議 256-bit 或更長)，並且**不要將正式環境的金鑰直接 commit 到版本控制系統中**。

```json
{
  "JwtSettings": {
    "Issuer": "YourAppName",
    "Audience": "YourAppUsers",
    "Key": "Your_Super_Secret_Key_At_Least_32_Characters_Long",
    "ExpireMinutes": 60
  }
}
```

### 3.2 註冊 JWT 驗證服務 (Program.cs)

在 `Program.cs` 中，加入 JWT 驗證的服務註冊與中介軟體。

```csharp
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// 1. 取得 JWT 設定
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Key"] ?? throw new ArgumentNullException("JwtSettings:Key is missing");

// 2. 設定 Authentication 與 Authorization 服務
builder.Services.AddAuthentication(options =>
{
    // 設定預設的驗證機制為 JwtBearer
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // 設定 JWT Bearer 的參數
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,               // 是否驗證發行者
        ValidIssuer = jwtSettings["Issuer"], // 允許的發行者

        ValidateAudience = true,             // 是否驗證接收者
        ValidAudience = jwtSettings["Audience"], // 允許的接收者

        ValidateLifetime = true,             // 是否驗證 Token 的有效期限
        
        ValidateIssuerSigningKey = true,     // 是否驗證簽章金鑰
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)) // 簽章金鑰
    };
});
builder.Services.AddAuthorization();

var app = builder.Build();

// 3. 啟用驗證與授權 Middleware (順序很重要，UseAuthentication 必須在 UseAuthorization 之前)
app.UseAuthentication();
app.UseAuthorization();

// ... 在這裡定義你的 Map 端點

app.Run();
```

### 3.3 產生 JWT Token (登入邏輯)

當使用者成功登入 (例如驗證帳號密碼成功) 後，你需要使用 `System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler` 來產生並回傳 JWT 給客戶端。

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

public class AuthService
{
    private readonly IConfiguration _config;

    public AuthService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateJwtToken(string userId, string userName, string role)
    {
        var jwtSettings = _config.GetSection("JwtSettings");
        var secretKey = jwtSettings["Key"] ?? throw new InvalidOperationException("Missing JWT Key");

        // 1. 定義 Claims (宣告) - 將使用者資訊放入 Token 中
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),           // Subject
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // JWT ID
            new Claim(ClaimTypes.Name, userName),                     // 名稱
            new Claim(ClaimTypes.Role, role)                          // 角色
        };

        // 2. 建立簽章金鑰與演算法
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpireMinutes"]));

        // 3. 建立 Token 描述物件
        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        // 4. 產生字串格式的 Token
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

### 3.4 保護 API 端點 (使用 `.RequireAuthorization()`)

在 Minimal API 中，我們在註冊路由時附加 `.RequireAuthorization()` 來限制只有攜帶有效 JWT 的請求才能存取。

```csharp
using System.Security.Claims;

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// 建立一個需要授權的路由群組
var protectedGroup = app.MapGroup("/api/protected").RequireAuthorization();

// 需要登入才能存取
protectedGroup.MapGet("/data", (ClaimsPrincipal user) =>
{
    // 可以從 ClaimsPrincipal 取得 Token 內的資訊
    var userName = user.Identity?.Name;
    return Results.Ok(new { Message = $"Hello {userName}, this is protected data!" });
});

// 需要特定角色才能存取
protectedGroup.MapGet("/admin-data", () =>
{
    return Results.Ok(new { Message = "This is admin only data." });
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

app.Run();
```

## 4. ⚠️ 安全性與最佳實踐建議

1.  **金鑰管理**：絕對不要把 JWT 密鑰 (Secret Key) 寫死在程式碼中，也不要 commit 到 Git 儲存庫。應該使用環境變數、Azure Key Vault 或 AWS Secrets Manager 來管理。
2.  **HTTPS 必備**：JWT 只是編碼，**沒有加密** (除非使用 JWE)。因此，所有的通訊都必須透過 HTTPS 傳輸，防止 Token 被攔截 (Man-in-the-Middle Attack)。
3.  **Token 有效期**：`ExpireMinutes` 應該設定得越短越好 (例如 15 ~ 60 分鐘)。
4.  **Refresh Token 機制**：為了平衡安全與使用者體驗，應實作 Refresh Token 機制。當短效的 Access Token (JWT) 過期時，使用長效的 Refresh Token 來換取新的 JWT。
5.  **不要放入敏感資訊**：JWT 的 Payload (Claims) 只是一層 Base64 編碼，任何人拿到 Token 都可以解碼看到內容。**絕對不要將密碼或其他機密個資放入 JWT 中**。
6.  **登出處理**：由於 JWT 是無狀態的，一旦發行在過期前都是有效的。如果要實作真正的「登出」或「撤銷 (Revoke)」，通常需要在後端維護一份黑名單 (Blocklist) 或是透過調整 Refresh Token 來間接達成。

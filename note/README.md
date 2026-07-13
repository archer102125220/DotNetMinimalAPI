# DotNetMinimalAPI 開發筆記與知識庫入口

歡迎來到 `DotNetMinimalAPI` 專案的知識庫目錄！這裡匯集了本專案所有的架構規範、環境設定、資料庫操作以及 C#/.NET 的核心知識。

無論您是剛接手專案的新手，或是想要複習特定技術細節的開發者，都可以透過下方的分類快速找到對應的筆記。

---

## 🏗️ 專案與環境設定 (Project & Environment Setup)
- [專案架構與設定說明](./PROJECT_STRUCTURE.md) - Minimal API (`Program.cs`) 結構、AOT 與核心設定檔解析。
- [macOS 開發環境安裝指南](./ASDF_DOTNET_INSTALL_MAC.md) - 透過 `asdf` 安裝 .NET 10 SDK 完整教學。
- [macOS 開發環境安裝指南 (Mise)](./MISE_DOTNET_INSTALL_MAC.md) - 透過 `mise` 安裝與管理 .NET 10 SDK 完整教學。
- [Docker 環境設定](./DOCKER_ENVIRONMENT.md) - 建立並管理本地端的 Oracle 資料庫容器。
- [環境變數與機密管理](./dotnet-env-and-secrets.md) - `appsettings.json` 與開發機密 (User Secrets) 管理教學。
- [正式部署指南](./DEPLOYMENT.md) - 伺服器部署流程、Native AOT 發布與生產環境資安防護檢查表。

## 📜 核心開發規範 (Development Guidelines)
- [AI 與團隊核心開發規範](./AI_CODING_GUIDELINES.md) - 專案的程式碼風格、AOT 限制、嚴格深度檢查 (Deep Check) 標準。
- [Minimal API 資料模型指南](./MODELS_GUIDELINE.md) - DTO 的使用方式、FluentValidation 驗證與 Thin Endpoints 模式。
- [Minimal API 路由指南](./minimal-api-routing-guide.md) - `MapGroup` 的運用與端點 (Endpoints) 設計最佳實踐。
- [預存程序與 EF Core 整合指南](./ef-core-sql-procedures-guide.md) - 記錄為何現代開發較少使用 SQL Procedures，以及如何在 EF Core Migrations 中管理並呼叫預存程序。

## 🛢️ 資料庫與 ORM 操作 (Database & ORM)
- [EF Core (ORM) 基礎與進階操作](./ef-core-orm-guide.md) - 資料庫連線、查詢最佳實踐 (`AsNoTracking`) 與 N+1 陷阱防範。
- [原生 ADO.NET 操作指南](./oracle-ado-net-guide.md) - 不依賴 EF Core 的極致效能查詢方式與 SQL Injection 防護。
- [Oracle 資料庫與 Docker 指令指南](./oracle-database-commands-guide.md) - 常見的 Oracle SQL 語法與 Docker `exec` 互動指令。
- [EF Core 資料庫遷移 (Migrations) 機制](./ef-core-migration-mechanism.md) - Code-First Schema 同步、部署與版本控制。
- [EF Core 多資料庫切換教學](./ef-core-multiple-databases.md) - 如何讓系統無縫支援 Oracle, SQL Server, PostgreSQL。
- [Mock Database 測試指南](./mock_database_guide.md) - 透過 In-Memory DB 或 SQLite 進行單元測試與整合測試。
- [ORM 與原生 SQL 架構對比](./orm-architecture-comparison.md) - 比較 EF Core、Dapper 與原生 ADO.NET 的差異與適用場景。

## 🔒 安全性與身分驗證 (Security & Authentication)
- [JWT 驗證實作指南](./JWT_AUTHENTICATION.md) - JSON Web Token 設定、註冊以及保護路由 (`RequireAuthorization`)。

## 🧠 C# 與 .NET 知識庫 (C# & .NET Knowledge)
- [C# 擴充方法 (Extension Methods) 實戰](./csharp-extensions-guide.md) - 如何將常用邏輯包裝為優雅的擴充方法。
- [.NET 核心知識點與面試題](./dotnet-knowledge-points.md) - 整理 .NET 執行階段、記憶體管理 (GC) 等底層知識。
- [.NET 10 與 .NET 6 差異解析](./dotnet10-vs-dotnet6.md) - 新舊 LTS 版本的語法糖與效能進化整理。
- [.NET 10 與傳統 .NET Framework 比較](./dotnet10-vs-dotnet-framework.md) - .NET Core 家族與過往 Windows 綁定框架的架構對比。

## 🤝 前後端協作與整合 (Frontend & Backend Integration)
- [TypeScript 型別產生指南](./openapi-typescript-generation-guide.md) - 透過 OpenAPI / Swagger 自動生成 TypeScript 型別定義，實現前後端型別同步。

## 🔄 語言轉換對照 (TypeScript to C#)
- [TypeScript vs C# 核心觀念對照](./typescript-vs-csharp.md) - 協助前端工程師快速轉換後端物件導向思維。
- [TypeScript vs .NET C# 進階語法對照](./typescript-vs-dotnet6-csharp.md) - 深入探討非同步、泛型、LINQ 等高階語法映射。
- [TypeScript vs 傳統 .NET Framework 對照](./typescript-vs-dotnet-framework-csharp.md) - 針對維護舊系統的過渡期語法指南。

## 🛠️ 實作範例 (Implementation Demos)
- [Oracle Minimal API 整合範例](./oracle-minimal-api-demo-guide.md) - 完整的 Minimal API CRUD 端點實作與 Oracle 資料庫對接教學。

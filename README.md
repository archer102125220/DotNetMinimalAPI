# DotNetMinimalAPI

這是一個用於學習和練習 **.NET Minimal API** 開發的專案。主要目的是透過實際操作來熟悉 .NET Core 的架構、路由設定、依賴注入以及輕量級的 API 建立方式。

## 什麼是 Minimal API？

Minimal API 是在 .NET 6 中引入的一種建立 HTTP API 的輕量級模式。與傳統基於 Controllers (控制器) 的架構相比，Minimal API 大幅減少了樣板程式碼 (Boilerplate Code) 和繁雜的設定。開發者可以利用極少的程式碼，甚至只在單一檔案 (`Program.cs`) 中，就能快速定義路由和處理邏輯。

### 通常在什麼情況下使用？

- **微服務架構 (Microservices)**：非常適合用來建立功能專一、小巧且輕量的獨立微服務。
- **快速原型開發 (Prototyping)**：在開發初期需要快速驗證概念或建立 API 端點時，能省去建立完整 Controller 架構的時間。
- **功能單一的 API**：例如僅負責接收 Webhook、處理簡單的無伺服器函式 (Serverless Functions) 或提供簡單的查詢介面。
- **效能敏感的輕量應用**：因其啟動快、佔用資源少的特性，適合部署在資源受限的環境（如邊緣計算或容器中）。
- **初學者學習**：因為去除了複雜的目錄結構與控制器概念，能讓初學者更專注並直覺地理解 HTTP 請求與路由的運作原理。

## 專案環境
- 框架：.NET 10.0
- 語言：C#

## 如何建立 Minimal API 專案

若你想從頭建立一個全新的 Minimal API 專案，可以使用 .NET CLI 提供的範本。開啟終端機並執行以下指令：

```bash
# 建立一個名為 MyMinimalApi 的 Web 專案 (預設即為 Minimal API 範本)
dotnet new web -n MyMinimalApi

# 進入專案目錄
cd MyMinimalApi
```

> [!NOTE]
> 在 .NET 6 之後，`dotnet new web` 預設就會建立一個 Minimal API 的專案結構（使用 Top-level statements 並且沒有 Controllers 資料夾）。若需要傳統含有 Controller 的 Web API 專案，則需使用 `dotnet new webapi`。

## 如何啟動專案

> [!NOTE]
> **Mac 使用者請注意**：微軟已經宣布淘汰 Mac 版的 Visual Studio（Visual Studio 2022 for Mac 已停止更新），因此在 Mac 環境下開發最新的 .NET 專案，官方推薦且主流的做法是直接使用 **.NET CLI**（命令列指令）搭配 VS Code 或 Rider。

請先確保你的電腦已安裝適當版本的 [.NET SDK](https://dotnet.microsoft.com/download)。

### 1. 使用終端機 (命令列) 啟動

將你的終端機目錄切換到本專案資料夾下，並執行以下指令：

**一般執行模式：**
```bash
dotnet run
```

**開發者模式 (熱重載 Hot Reload)：**
（推薦使用此模式，當你修改程式碼並存檔時，API 伺服器會自動重新載入，無須手動重啟）
```bash
dotnet watch run
```

### 2. 使用 IDE 啟動

- **VS Code**: 
  1. 在專案根目錄開啟 VS Code (`code .`)。
  2. 若已安裝 C# / C# Dev Kit 擴充功能，按下 `F5` 即可啟動偵錯。
- **Visual Studio (Windows) / Rider**:
  > [!TIP]
  > **跨平台與 IDE 開啟提示**：本專案初始是透過 .NET CLI 建立，目錄下預設可能沒有 `.sln` (方案檔)。若使用 Windows 版 Visual Studio，只需直接開啟 `.csproj` 專案檔即可。Visual Studio 載入後，在首次儲存時會自動提示您生成 `.sln` 檔。

  1. 直接開啟 `.csproj` 專案檔。
  2. 點擊上方的「執行 (Run)」或按下 `F5` 開始執行。

## 測試 API

專案成功啟動後，API 預設會運行在 `http://localhost:5074` (依據 `Properties/launchSettings.json` 設定)。

> [!WARNING]
> **為什麼開啟 `http://localhost:5074/` 會出現 HTTP ERROR 404 找不到網頁？**
> 這完全是正常的。因為這是一個純後端 API 專案，我們沒有針對網站根目錄 (`/`) 定義任何路由或回傳靜態網頁，因此會顯示 404 錯誤。

你可以打開瀏覽器，或使用 API 測試工具（如 Postman、Thunder Client、curl）來發送請求測試。以下是我們專案中定義的實際可用路徑：

- **查看 API 文件與測試介面 (推薦使用)**：
  請在瀏覽器開啟 [http://localhost:5074/scalar](http://localhost:5074/scalar) 以使用 Scalar 互動式文件介面。
- **直接取得 API 資料**：
  請在瀏覽器開啟 [http://localhost:5074/todos](http://localhost:5074/todos) 可直接取得代辦事項的 JSON 資料。

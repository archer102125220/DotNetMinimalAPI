# DotNetMinimalAPI

這是一個用於學習和練習 **.NET Minimal API** 開發的專案。主要目的是透過實際操作來熟悉 .NET Core 的架構、路由設定、依賴注入以及輕量級的 API 建立方式。

## 專案環境
- 框架：.NET 10.0
- 語言：C#

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

你可以打開瀏覽器，或使用 API 測試工具（如 Postman、Thunder Client、curl）來發送請求測試。例如：
```
http://localhost:5074/todos
```

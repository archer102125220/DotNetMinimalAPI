# 使用 mise 在 macOS 安裝與管理 .NET SDK

這份文件記錄了如何在 macOS 環境下，使用次世代的開發環境管理工具 [mise](https://mise.jdx.dev/) 來安裝和管理 .NET SDK。
`mise` (前身為 rtx) 是一款用 Rust 編寫的高效能工具，可以做為 `asdf` 的完美替代品，且執行速度更快、設定更簡潔。

## 1. 系統環境準備

如果您尚未安裝 `mise`，可以透過 Homebrew 來安裝：

```bash
brew install mise
```

安裝完成後，需要將 `mise` 載入設定加入您的 Shell 中。以 `zsh` 為例，請在您的 `~/.zshrc` 檔案末端加入：

```bash
echo 'eval "$(mise activate zsh)"' >> ~/.zshrc
source ~/.zshrc
```

## 2. 安裝 .NET SDK

透過 `mise` 安裝 .NET 非常簡單，不需要像 `asdf` 那樣手動加入外掛程式 (plugin)，`mise` 會自動處理。

### 尋找可用版本
若要查看有哪些版本的 .NET 可以安裝：
```bash
mise ls-remote dotnet
```

### 執行安裝並設定為全域 (Global)
針對本專案 (DotNetMinimalAPI)，我們預期使用 .NET 10.0 SDK。您可以直接透過 `use` 指令安裝並設為全域預設：

```bash
mise use --global dotnet@10.0
```
或指定精確的 Patch 版本：
```bash
mise use --global dotnet@10.0.100
```
這會在您的 `~/.config/mise/config.toml` 中記錄全域的預設版本。

### 設定為單一專案 (Local)
如果您只希望在這個特定專案中使用，請進入專案根目錄後執行：

```bash
mise use dotnet@10.0
```
這會在當前專案資料夾下建立或更新 `mise.toml` 檔案。使用 `mise.toml` 的好處是，除了鎖定版本，還可以在裡面定義專案專屬的環境變數。

## 3. 驗證安裝

完成設定後，檢查 `dotnet` 指令是否正常運作：

```bash
dotnet --version
```
如果出現對應的 SDK 版本號，代表您已成功透過 `mise` 裝好 .NET SDK！

## 4. 全域工具 (Global Tools) 與環境變數

如果您習慣透過 `dotnet tool install -g <tool_name>` 安裝全域命令列工具 (例如 `dotnet-ef`)，它們預設會安裝在 `~/.dotnet/tools`。

請將以下路徑加入到您的 `~/.zshrc` 中：

```bash
export PATH="$HOME/.dotnet/tools:$PATH"
```
加入後，記得執行 `source ~/.zshrc` 使環境變數生效。

> **提示**：在使用 `asdf` 時，有時安裝完工具後需要執行 `asdf reshim`，但在 `mise` 中通常會自動處理 shim，無需手動 reshim。

## 5. 解決 IDE 找不到 .NET SDK 的問題

由於 `mise` 同樣透過修改 `PATH` 或使用 shims 來管理版本，當您直接從 macOS 的應用程式圖示開啟 IDE (Visual Studio Code, Rider) 時，IDE 可能無法讀取到 `~/.zshrc` 中的設定，導致找不到 `.NET SDK`。

您可以透過以下方式解決：

### 1. 透過終端機啟動 IDE (推薦做法)
最穩定且簡單的方式是，在已經載入 `mise` 的終端機內啟動 IDE，這樣 IDE 就會自動繼承當前的環境變數：
*   **VS Code**: 在專案目錄下執行 `code .`
*   **Rider**: 執行 `rider .`

### 2. VS Code 手動路徑設定
如果習慣點擊圖示開啟 VS Code，可以手動指定 `dotnet` 的路徑給 C# 擴充套件。
在專案的 `.vscode/settings.json` 中加上：
```json
{
    "dotnet.dotnetPath": "~/.local/share/mise/shims"
}
```

### 3. Rider 手動路徑設定
如果 Rider 啟動時提示找不到 .NET CLI：
1. 開啟 **Preferences (設定) > Build, Execution, Deployment > Toolset and Build**。
2. 找到 **.NET CLI executable path** 欄位。
3. 將路徑更改為 mise shim 的路徑：`~/.local/share/mise/shims/dotnet`。

### 4. 設定 `DOTNET_ROOT` 環境變數
有些工具或擴充套件 (例如 OmniSharp/C# Dev Kit) 依賴 `DOTNET_ROOT` 來定位 SDK。
利用 `mise` 的強大功能，您可以直接在專案的 `mise.toml` 中宣告這個變數，如此一來只要進入專案，`mise` 就會自動幫您載入正確的 `DOTNET_ROOT`：

```toml
[tools]
dotnet = "10.0"

[env]
# 動態指向 mise 安裝的 dotnet 目錄
DOTNET_ROOT = "{{env.HOME}}/.local/share/mise/installs/dotnet/10.0.100"
```
這樣設定的好處是將環境變數與工具版本完美綁定在專案設定檔中。

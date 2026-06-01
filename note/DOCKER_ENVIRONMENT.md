# .NET Web API 專案：Docker 服務環境與資料庫初始化教學

這份文件記錄了本專案 `docker/` 資料夾下的服務架構。這些資料庫環境**主要是以學習與測試不同資料庫操作為目的而建立的**。專案目前規劃**預設會以 MSSQL (Microsoft SQL Server) 作為主要使用的資料庫**，因此本文件特別針對 MSSQL 的快速建立與自動初始化進行了詳細說明。

---

## 📂 `docker` 目錄結構概覽

專案底下的 `docker/` 資料夾預留了多種資料庫環境供學習與切換測試使用：
- `mssql/`：Microsoft SQL Server 2022 (**本專案預設與主要規劃使用的資料庫**)
- `mysql/`：MySQL 服務設定 (學習測試備用)
- `postgres/`：PostgreSQL 服務設定 (學習測試備用)
- `oracle/`：Oracle 資料庫設定 (學習測試備用)

---

## 🔑 統一環境變數與連線資訊

為了方便切換與測試，本專案在所有資料庫環境中，皆預設配置了相同的應用程式專屬帳號與密碼 (取代預設的高權限 `root` / `sa` / `postgres` 帳號)：
- **統一帳號 (User)**: `dot-net-minimal-api`
- **統一密碼 (Password)**: `DotNetMinimalAPIAbc123`
- **預設資料庫名稱**: 依資料庫特性而定 (見下表)

| 資料庫服務 | 對外 Port | 資料庫名稱 / Schema | 連線帳號 | 連線密碼 | 備註 |
| --- | --- | --- | --- | --- | --- |
| **MSSQL** (預設) | `1434` (對應內部 1433) | `DotNetMinimalDb` | `dot-net-minimal-api` | `DotNetMinimalAPIAbc123` | Apple Silicon Mac 需 Rosetta (`linux/amd64`) |
| **MySQL** | `3307` (對應內部 3306) | `dot_net_minimal_api_db` | `dot-net-minimal-api` | `DotNetMinimalAPIAbc123` | Apple Silicon Mac 需指定 `linux/x86_64` |
| **PostgreSQL**| `5433` (對應內部 5432) | `dot_net_minimal_api_db` | `dot-net-minimal-api` | `DotNetMinimalAPIAbc123` | |
| **Oracle 23ai**| `1522` (對應內部 1521) | `FREEPDB1` (PDB) <br> Schema: `dot-net-minimal-api` | `dot-net-minimal-api` | `DotNetMinimalAPIAbc123` | 原生支援 Mac ARM64 架構 |

---

## 🛢️ MSSQL 環境設定與自動初始化 (預設)

為了避免使用預設的高權限 `sa` 帳號進行應用程式連線，並自動為專案建立專屬資料庫，我們在 `docker/mssql/` 實作了「容器啟動自動初始化」的機制。

### 1. `docker-compose.yml`
負責定義服務的基本配置與檔案掛載。
- **Image**: 使用 `mcr.microsoft.com/mssql/server:2022-latest`
- **Port**: 將容器內的 1433 port 映射到主機的 `1434` (`1434:1433`)
- **Volumes 掛載**:
  - `mssql_data`：持久化儲存資料庫檔案。
  - `init.sql`：掛載到 `/docker-entrypoint-initdb.d/init.sql`。
  - `entrypoint.sh`：掛載並覆寫為預設執行指令 (`command: /bin/bash .../entrypoint.sh`)。
- **Platform**: 設定 `linux/amd64` 以確保 Apple Silicon (M1/M2/M3) Mac 能透過 Rosetta 正常執行 x86 架構的 SQL Server。

### 2. `entrypoint.sh` (自動啟動與監聽腳本)
因為官方的 MSSQL Docker 映像檔並未內建類似 PostgreSQL 的 `/initdb.d` 自動啟動機制，因此我們撰寫了這支腳本。
- **背景執行**：透過 `/opt/mssql/bin/sqlservr &` 在背景啟動資料庫服務。
- **健康檢查與等待**：透過 `while` 迴圈與 `sqlcmd -Q "SELECT 1"` 不斷嘗試連線，直到資料庫完全啟動。
- **觸發初始化**：確認服務啟動後，呼叫 `sqlcmd` 來執行 `init.sql`。
- **保持運行**：最後透過 `wait $PID` 攔截背景程序的生命週期，讓 Docker 容器持續運作不退出。

### 3. `init.sql` (資料庫與帳號建立腳本)
本腳本包含了「防呆與冪等性 (Idempotent)」的設計（即多次執行也不會引發錯誤），其主要任務：
1. **建立資料庫**：建立名為 `DotNetMinimalDb` 的資料庫。
2. **建立登入與使用者 (Login & User)**：建立應用程式專用的帳號 `dot-net-minimal-api`，密碼設定為 `DotNetMinimalAPIAbc123`。
3. **權限配置**：將此使用者加入 `db_owner` 角色，確保 EF Core 能順利執行 Migrations 以及一般讀寫操作。

---

## 🚀 常用操作指令

### 啟動資料庫服務
開啟終端機，切換到指定的資料庫目錄（以 MSSQL 為例）並執行啟動指令：
```bash
cd docker/mssql
docker-compose up -d
```

### 檢視啟動與初始化日誌
如果想確認資料庫是否正確初始化完畢，可以查看 Log：
```bash
docker-compose logs -f
```
*(在 MSSQL 的 Log 中你會在底部看到 `SQL Server is up - running initialization script...` 以及 `Initialization finished.` 的字樣)*

### 清除並重置資料庫
如果你修改了 `init.sql` 或環境變數想重新套用，需要清除原有的 Volume 才能觸發重新初始化：
```bash
# 停止容器並刪除 Volume (注意：這會清空所有的資料庫資料！)
docker-compose down -v

# 再次啟動以重新初始化
docker-compose up -d
```

---

## 💡 開發注意事項
- 在 `.NET Web API` 的 `appsettings.json` 中設定連線字串 (Connection String) 時，請參考上方表格中的對外 Port、帳號與密碼，**不要**使用高權限管理員帳號 (`sa`, `root`, `postgres`)。
  - MSSQL 範例：`Server=localhost,1434;Database=DotNetMinimalDb;User Id=dot-net-minimal-api;Password=DotNetMinimalAPIAbc123;TrustServerCertificate=True;`
- 專案程式碼如果需要依賴新的資料庫欄位或表，應優先透過 EF Core 的 Migrations (`dotnet ef migrations add ...`) 來處理，盡量不要手動去改 `init.sql`，`init.sql` 僅負責「基礎環境與權限」的建置。

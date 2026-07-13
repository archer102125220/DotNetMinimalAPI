# 忽略檔 (.gitignore 與 .dockerignore) 設定指南

在使用 Docker 開發時，妥善設定「忽略檔」是非常關鍵的一步。如果設定不當，輕則導致編譯變慢、Image 檔案過大，重則可能將**機密密碼外洩**到公開的 GitHub 上，或是把機密檔案死死地打包進 Docker Image 裡面。

Docker 開發中，我們會遇到兩種忽略檔：
1. **`.gitignore`**：告訴 Git 什麼檔案**不要上傳到版控中心 (如 GitHub/GitLab)**。
2. **`.dockerignore`**：告訴 Docker 什麼檔案**不要送到 Docker 引擎裡打包**。

---

## 1. `.gitignore` 該忽略哪些 Docker 相關檔案？

`.gitignore` 的核心原則是：**保護機密、排除個人客製化設定、排除執行時期產生的實體資料。**

在你的專案根目錄下的 `.gitignore`，你應該確保有以下這些 Docker 相關的規則：

```text
# ========================
# Docker 相關忽略清單 (.gitignore)
# ========================

# 1. 環境變數檔 (極度危險！絕對不可進版控)
.env
*.env
.env.*

# 2. 本機開發者專用的覆寫設定 (團隊每個人的本機設定可能都不同)
docker-compose.override.yml

# 3. 本機資料庫掛載出來的實體資料夾 (視你的 volume 設定而定)
# 例如你把 db 資料掛載在本機專案底下的目錄
/pgdata/
/mysql_data/
/docker_data/
```

> 💡 **最佳實踐**：雖然 `.env` 和 `.override.yml` 被忽略了，但新進員工還是需要知道有哪些變數要填。通常團隊會另外建立 `.env.example` 和 `docker-compose.override.example.yml`，裡面放假的資料與預設設定。這兩份 example 檔案**可以**進版控，讓其他工程師 clone 下來後自己複製並改名。

---

## 2. `.dockerignore` 該忽略哪些檔案？

當你下達 `docker build .` 時，Docker 第一件事就是把當前目錄下的**所有檔案**打包送給後台的 Docker 引擎。
`.dockerignore` 的核心原則是：**排除本機編譯產物、排除機密檔案、排除與應用程式執行無關的雜物。**

這是一份標準的 .NET 專案 `.dockerignore` 範例：

```text
# ========================
# .dockerignore 忽略清單
# ========================

# 1. 版控系統紀錄 (非常佔空間，且 Image 裡面不需要知道自己在哪個 Git 分支)
.git/
.gitignore

# 2. 本機的編譯與快取產物 (確保 Docker 內的 Build 拿到的是最乾淨的 Source Code)
bin/
obj/
.vs/
.vscode/

# 3. 機密檔案 (環境變數不該被「烤」進 Image 裡，應該是啟動 Container 時才由外部動態傳入)
.env
*.env

# 4. Docker 自己的設定檔與說明文件 (應用程式執行不需要這些)
Dockerfile*
docker-compose*
README.md
```

> ⚠️ **為什麼 `.env` 也要放進 `.dockerignore`？**
> 如果你不把它忽略，當 Dockerfile 執行 `COPY . .` (把目前目錄所有檔案複製進去) 的時候，`.env` 就會被複製到 Image 裡面。如果哪天這個 Image 被推送到公開的 Docker Hub，駭客只要把它拉下來，就能輕易把裡面的帳號密碼檔偷走！

---

## 總結比較

| 檔案名稱 | 作用對象 | 目的 | 常見必須忽略的檔案 |
| --- | --- | --- | --- |
| **`.gitignore`** | Git (版控) | 避免機密與個人設定上傳到共用資源庫 | `.env`, `docker-compose.override.yml`, 資料庫掛載的實體資料夾 |
| **`.dockerignore`** | Docker (打包) | 避免機密烤入 Image、縮小 Image 體積、加速編譯 | `.git/`, `.env`, `bin/`, `obj/`, `Dockerfile` |

妥善設定這兩個檔案，能讓你的 Docker 專案既安全、輕巧又專業！

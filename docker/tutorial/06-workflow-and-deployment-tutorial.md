# 實務 Docker 開發與部署流程總覽

當我們把 Docker 導入專案後，從工程師的本機開發，到最終程式碼上線（部署），有一套業界常見的標準流程。

這份教學會帶您走過這三個階段：
1. **本機開發流程 (Local Development)**
2. **手動部署流程 (Manual Deployment)**
3. **CI/CD 自動化部署流程 (Automated Deployment)**

---

## 1. 本機開發流程 (Local Development)

在本機開發時，重點在於**快速迭代**與**除錯**。我們不希望每次改一行程式碼都要花一分鐘重包 Docker Image。

### 步驟：
1. **環境初始化**：撰寫 `.env` 檔案，設定好本機專用的開發帳號密碼。
2. **啟動依賴服務**：執行 `docker compose up -d`。此時 Docker 會自動合併 `docker-compose.yml` 與您的 `docker-compose.override.yml`。
   - 資料庫容器會啟動，並套用 `init.sql` 塞入假資料。
   - Web 容器會啟動，並透過 `volumes` 掛載您電腦上的 Source Code。
3. **開發與熱重載**：您在本機用 IDE (如 Visual Studio 或 VS Code) 寫 Code。一按下存檔，容器內的工具 (例如 `dotnet watch`) 就會偵測到變更並瞬間重啟應用程式 (Hot Reload)，你完全不需要重新 `docker build`。
4. **除錯**：可以直接打開瀏覽器看網頁，或是用 DBeaver 等資料庫軟體直接連線進 DB 檢查資料（因為我們有開 ports 映射出來）。
5. **收工**：開發結束後，執行 `docker compose down` 關閉容器以節省電腦資源。

---

## 2. 手動部署流程 (Manual Deployment)

適用於專案初期，或是尚未架設 CI/CD 伺服器的團隊。

### 階段 A：在本機打包並上傳 Image
1. **建置 Image (Build)**：確認程式碼沒問題後，執行以下指令將當前程式碼打包成 Image：
   ```bash
   # 記得幫 Image 加上版號標籤 (tag)，例如 v1.0.0
   docker build -t my-registry.com/my-webapp:v1.0.0 .
   ```
2. **推送到 Registry (Push)**：將打包好的 Image 推送到遠端的 Docker 儲存庫 (例如 Docker Hub, AWS ECR, 或是自建的 Harbor)。
   ```bash
   # 先登入
   docker login my-registry.com
   # 推送上雲端
   docker push my-registry.com/my-webapp:v1.0.0
   ```

### 階段 B：在正式機 (Production Server) 上更新
1. **登入正式機**：透過 SSH 連進你的正式伺服器。
2. **修改設定**：更新伺服器上的 `.env` 或 `docker-compose.yml`，將 Image 的 tag 改為剛剛上傳的 `v1.0.0`。
   > 💡 **觀念釐清**：正式機上「不需要」原始碼，也不需要 `Dockerfile` 與 `override.yml`，只需要乾淨的 `docker-compose.yml` 和 `.env` 即可。
3. **拉取新 Image (Pull)**：
   ```bash
   docker compose pull
   ```
4. **重新啟動服務 (Up)**：
   ```bash
   # Docker 會聰明地發現 Image 換了，自動停止舊容器、啟動新容器 (Recreate)
   docker compose up -d
   ```

---

## 3. CI/CD 自動化部署流程 (Automated Deployment)

當專案穩定後，每次上版都靠工程師手動 Build 跟 SSH 是非常危險且沒效率的。這時我們會引入 CI/CD 系統 (例如 GitHub Actions, GitLab CI, Jenkins)。

在這個流程中，**工程師的電腦裡絕對不會手動執行 `docker build` 或 `docker push`**。

### 標準 CI/CD 流程：
1. **工程師 Push Code**：工程師開發完畢，將 Code 推送到 Git 伺服器的 `main` (或 `master`) 分支。
2. **CI Pipeline 觸發 (持續整合)**：
   - Git 伺服器偵測到更新，自動觸發一台中繼的 CI 機器 (Runner)。
   - Runner 自動抓取最新原始碼。
   - Runner 自動編譯並執行單元測試 (Unit Test)。
   - **(Docker 出場)** 測試通過後，Runner 執行 `docker build` 將程式碼包成 Image。
     > 📄 **用到哪些設定檔**：`Dockerfile` (告訴 Runner 怎麼打包程式碼)、`.dockerignore` (過濾不需要打包的垃圾檔案)。
   - Runner 執行 `docker push` 將 Image 推送到 Registry。
3. **CD Pipeline 觸發 (持續部署)**：
   - Runner 透過 SSH 金鑰（或 Webhook Agent）連線進你的「正式機伺服器」。
   - Runner 在正式機上透過腳本自動替換掉舊的 Image 版本號。
   - Runner 在正式機上自動執行 `docker compose pull` 與 `docker compose up -d`。
     > 📄 **用到哪些設定檔**：正式機上只需要純淨的 `docker-compose.yml` (服務編排) 與 `.env` (正式機的環境變數)。**絕對不會**用到 `docker-compose.override.yml`。
4. **完成上線**：整個過程無人為介入，工程師只要推完 Code，喝杯咖啡，幾分鐘內系統就自動上線了。

### CI/CD 中的 Docker 核心優勢
因為有 `Dockerfile` 確保了「環境的絕對一致性」，只要 CI Server 上的 `docker build` 能夠成功，打包出來的 Image 放到正式機上就 **100% 絕對跑得起來**，徹底消滅了以前開發界最可怕的災難：「這支程式在我的電腦上明明可以跑啊！」。

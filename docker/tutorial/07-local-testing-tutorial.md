# 本地端測試 Docker 設定檔指南

在將 Docker 設定檔推送到版控，或是讓 CI/CD 系統接手前，我們必須在本機端確認這些設定檔是正確無誤的。

我們可以透過**分階段的模擬測試**，來抓出潛在的問題。以下測試請都在包含 `docker-compose.yml` 的專案目錄下執行。

---

## 階段 1：測試 `Dockerfile` 與 `.dockerignore` (模擬 CI 打包)
這個步驟用來驗證：**程式碼能不能成功編譯並打包成 Image？不需要的垃圾檔案有沒有被過濾掉？**

1. **執行打包指令**：
   ```bash
   docker build -t test-app .
   ```
   *👉 如果這個指令沒有報錯並且順利跑完，就代表你的 `Dockerfile` 寫對了！*

2. **(進階) 檢查 `.dockerignore` 是否生效**：
   打包成功後，我們可以開一個一次性的容器進去「偷看」檔案系統，確認 `bin/`、`obj/` 等資料夾沒有被不小心包進去：
   ```bash
   docker run -it --rm test-app /bin/bash
   # 如果是 alpine 系列的 image 則用 /bin/sh
   # 進去後下 ls -la 看一下資料夾結構，看完輸入 exit 離開
   ```

---

## 階段 2：測試「本機開發環境」 (包含 override 檔)
這個步驟用來驗證：**你的開發環境配置（掛載目錄、開的 Debug Port 等）是否正常運作？**

1. **直接啟動**：
   ```bash
   docker compose up -d
   ```
   *👉 Docker 會自動去合併 `docker-compose.yml` 和 `docker-compose.override.yml`。*
2. **驗證連線**：打開瀏覽器去連你設定的 Port (例如 `http://localhost:8080`) 看網頁有沒有出來，或是用資料庫軟體連線 `localhost:5432` 看資料庫有沒有活著。
3. **驗證熱重載** (若有設定掛載 src)：隨便改一行 Code 存檔，看畫面有沒有跟著變。
4. **測試完關閉**：
   ```bash
   docker compose down
   ```

---

## 階段 3：測試「類正式機環境」 (⚠️ 最關鍵：模擬 CD 上線)
這個步驟用來驗證：**如果把這包 Image 和設定檔原封不動丟到正式機上，它還能跑嗎？**

在正式機上我們**絕對不會**用到 `docker-compose.override.yml`（因為正式機不需要掛載你的本機原始碼），所以我們必須在本機「刻意忽略」它來做嚴格測試。

1. **強制只讀取基礎設定檔啟動**：
   使用 `-f` 參數，明確告訴 Docker Compose **「我只要讀這份檔案，不要幫我自動合併 override」**：
   ```bash
   # 模擬正式機上線的情境
   docker compose -f docker-compose.yml up -d
   ```
2. **看 Log 抓蟲**：
   ```bash
   docker compose logs -f
   ```
   *👉 仔細觀察有沒有報錯。最常見的錯誤是「正式機讀不到 `.env` 的變數」或是「在 Dockerfile 中忘記把某個需要的設定檔 COPY 進 Image 裡 (因為平常開發都是掛載本機目錄，所以沒發現)」。*

3. **測試完徹底清理乾淨**：
   ```bash
   # 加上 -v 把測試用的 volume 也清掉，保持本機乾淨
   docker compose -f docker-compose.yml down -v
   ```

---

### 💡 總結

只要你在本地端走過這兩道防線：
1. `docker build` (打包沒報錯)
2. `docker compose -f docker-compose.yml up -d` (正式機模擬啟動沒報錯)

你就可以非常放心地把程式碼 Push 出去，交給 CI/CD 處理了！

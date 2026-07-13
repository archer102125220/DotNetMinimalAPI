# Docker 教學與範例

這個資料夾包含了 Docker 的基礎教學，以及給 .NET MVC 專案參考用的 Docker 設定檔範例。

## 目錄

1. [基礎指令介紹](01-basic-commands.md)
2. [Dockerfile 撰寫教學](02-dockerfile-tutorial.md)
3. [Docker Compose 使用教學](03-docker-compose-tutorial.md)
4. [Database Seeding (建立預設資料) 教學](04-database-seeding-tutorial.md)
5. [覆寫設定 (Override) 教學](05-docker-compose-override-tutorial.md)
6. [實務 Docker 開發與部署流程總覽](06-workflow-and-deployment-tutorial.md)
7. [本地端測試 Docker 設定檔指南](07-local-testing-tutorial.md)
8. [忽略檔 (.gitignore 與 .dockerignore) 設定指南](08-ignore-files-tutorial.md)
9. [CI/CD 平台中的環境變數與機密管理](09-cicd-environment-variables-tutorial.md)

## 範例專案結構

在 `sample-app` 資料夾中，我們提供了一組完整的範例，包含：
- `Dockerfile`: 用於打包 .NET Web 應用程式的設定檔。
- `docker-compose.yml`: 用於一鍵啟動 Web 應用程式與關聯資料庫 (例如 PostgreSQL) 的設定。
- `docker-compose.override.yml`: **(進階)** 用於本機開發時，自動覆寫或擴充基礎 `docker-compose.yml` 的專屬設定檔。
- `.dockerignore`: 告訴 Docker 在 build image 時應該忽略哪些檔案。
- `.env`: **(進階)** 存放環境變數（如密碼、資料庫名稱）的設定檔。
- `init.sql`: **(進階)** 資料庫啟動時自動執行的 SQL 初始化腳本。*(透過 `docker-compose.yml` 掛載到資料庫容器內，常做為非 .NET 專案建立 Schema 或預設資料的手段，詳見教學 04)*

您可以直接進入 `sample-app` 目錄，嘗試執行教學中的指令。

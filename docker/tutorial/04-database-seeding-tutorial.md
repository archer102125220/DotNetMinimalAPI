# Database Seeding (建立預設資料) 教學

在容器化環境（Docker）中，資料庫通常是「用完即丟」或隨時會重新建立的。因此，如何自動且優雅地在資料庫啟動後「塞入預設假資料 (Seed Data)」是一門很重要的學問。

本篇將介紹在 Docker 環境下，不同技術棧最常見的三種 Seeding 策略：

---

## 1. 應用程式碼級別 (Application-level)
**適用對象**：使用 Entity Framework Core (C#)、部分強依賴 ORM 的框架。

在 .NET 生態系中，最主流的做法是將預設資料綁定在 C# 程式碼與 Migration 裡面。

**運作方式**：
1. 開發者使用 `modelBuilder.Entity<T>().HasData(...)` 或自訂的 `DbInitializer` 來撰寫假資料。
2. 在 `Program.cs` 啟動時，檢查並呼叫 `context.Database.Migrate()` 進行資料庫更新與 Seeding。
3. 在 `docker-compose.yml` 中，只需設定 Web 容器 `depends_on` DB 容器。Web 容器啟動後就會自動處理好一切。

> **優點**：資料與程式碼版本高度一致，支援複雜邏輯（如密碼雜湊）。
> **缺點**：應用程式啟動時間會微幅拉長。

---

## 2. 資料庫初始化腳本級別 (Database-level)
**適用對象**：傳統 ADO.NET、Dapper、不使用 Code-First 的專案，或是需要匯入極大量測試資料的場景。

這是透過資料庫本身的 Docker Image 提供的初始化功能來達成。

**運作方式**：
1. 撰寫一個純 SQL 檔案，例如 `init.sql`（內容全是 `CREATE TABLE` 與 `INSERT INTO`）。
2. 在 `docker-compose.yml` 中，利用 `volumes` 將該 SQL 檔掛載到資料庫容器的特定目錄（例如 Postgres 是 `/docker-entrypoint-initdb.d/`）。
3. 當資料庫容器「第一次」啟動時，就會自動執行該目錄下的所有腳本。

> **優點**：執行速度極快，不用等 Web 應用程式啟動。
> **缺點**：如果 Schema 改變，必須手動維護 SQL 檔。

---

## 3. 指令列手動或掛載觸發 (CLI / Seeder Container)
**適用對象**：Node.js (Sequelize, Prisma)、PHP (Laravel)、Python (Django) 等習慣將 Migration/Seed 與啟動程式分離的生態系。

在這些語言中，Seeder 通常是一支獨立的腳本或指令（例如 `npx sequelize-cli db:seed:all`）。

**在 Docker 中的常見做法有兩種**：

### A. 修改啟動腳本 (Command)
在 `docker-compose.yml` 中，修改主應用程式的啟動指令，讓它先跑完 Seed 再啟動 Server：
```yaml
services:
  node_app:
    # 串接指令：先 migrate -> 再 seed -> 最後啟動 server
    command: sh -c "npx sequelize-cli db:migrate && npx sequelize-cli db:seed:all && npm start"
```

### B. 免洗容器 (Init Container) (最佳實踐)
為了避免多個 Web 容器同時搶著寫入資料庫（Race Condition），可以額外開一個專門跑 Seed 的容器，跑完就關閉：
```yaml
services:
  web:
    command: npm start # 主程式正常啟動

  seeder_job:
    # 專門用來跑 Seed 的免洗容器
    command: sh -c "npx sequelize-cli db:seed:all"
    depends_on:
      - db
```

> **優點**：避免 Race Condition，權責分離。
> **缺點**：`docker-compose.yml` 設定會稍微複雜一點。

---

### 總結
如果您的專案是 **.NET MVC + EF Core**，請果斷採用**第 1 種**方式。
如果您的團隊有混用 Node.js 等其他語言，了解第 2 與第 3 種方式能幫助您更好地與其他技術棧的 Docker 環境整合。

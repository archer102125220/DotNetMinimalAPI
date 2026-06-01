/*
  =========================================================
  SQL Server 初始化腳本 (init.sql)
  =========================================================
  這個腳本會在 Docker 容器啟動時，由 entrypoint.sh 呼叫執行。
  目的是為了自動建立開發用的資料庫以及專屬的應用程式連線帳號。
  
  所有的 IF NOT EXISTS 檢查是為了確保腳本的「冪等性」(Idempotency)，
  意思是即使這個腳本被重複執行多次，也不會因為物件已存在而報錯。
*/

-- --------------------------------------------------------
-- 1. 建立應用程式的專屬資料庫
-- --------------------------------------------------------
-- 查詢系統檢視表 sys.databases，確認名為 'DotNetMvcDb' 的資料庫是否已經存在
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'DotNetMvcDb')
BEGIN
    -- 如果不存在，則建立新的資料庫
    CREATE DATABASE [DotNetMvcDb];
END
-- 'GO' 是一個批次處理(Batch)的結束標記，它告訴 SQL Server 把前面的指令打包送出執行
GO

-- 切換目前的上下文到剛建立的資料庫中，後續的指令都會在此資料庫內執行
USE [DotNetMvcDb];
GO

-- --------------------------------------------------------
-- 2. 建立登入帳號 (Server-level Login)
-- --------------------------------------------------------
-- 在 SQL Server 中，「Login (登入)」是伺服器層級的，負責處理連線的身份驗證 (Authentication)
-- 這裡查詢 sys.server_principals，確認是否已經有這個登入帳號
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = N'dot-net-minimal-api')
BEGIN
    -- 建立伺服器登入帳號，並指定密碼
    -- 注意：使用中括號 [] 是為了避免名稱中含有特殊字元 (例如連字號 -) 時產生語法錯誤
    CREATE LOGIN [dot-net-minimal-api] WITH PASSWORD = 'DotNetMinimalApiAbc123';
END
GO

-- --------------------------------------------------------
-- 3. 建立資料庫使用者 (Database-level User) 並賦予權限
-- --------------------------------------------------------
-- 「User (使用者)」是資料庫層級的，負責處理權限授權 (Authorization)
-- 剛才建立的 Login 必須要對應到這個資料庫的 User，才能真正操作裡面的資料
-- 查詢 sys.database_principals 確認是否已經建立過此使用者
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = N'dot-net-minimal-api')
BEGIN
    -- 在當前資料庫 (DotNetMvcDb) 中建立 User，並將其與剛才建立的 Server Login 綁定
    CREATE USER [dot-net-minimal-api] FOR LOGIN [dot-net-minimal-api];
    
    -- 將此使用者加入到 db_owner 這個預設的資料庫角色 (Role) 中
    -- db_owner 擁有該資料庫內的所有權限 (包含建表、讀寫資料等)，非常適合本地開發測試使用
    ALTER ROLE db_owner ADD MEMBER [dot-net-minimal-api];
END
GO

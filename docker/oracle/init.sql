-- ==============================================================================
-- Oracle 資料庫與 MySQL / MSSQL 的核心差異：
-- 
-- 1. 在 Oracle 中，沒有 CREATE DATABASE 指令用來建立應用程式資料庫。
--    Docker 容器啟動時，會預設建立好一個 Pluggable Database (PDB) 叫做 FREEPDB1。
-- 2. 在 Oracle 中，User (帳號) 就等於 Schema (資料庫空間)。
--    所以當我們 CREATE USER 時，就等於同時建立了一個同名的獨立資料庫實體空間。
-- 3. 若需要一個帳號管理多個「資料庫」，最標準的做法是：
--    建立主帳號並賦予 DBA 權限，然後在同一個 FREEPDB1 裡面建立多個 User/Schema。
--    程式端即可透過同一個帳號，跨 Schema 存取資料 (例如: SELECT * FROM SCHEMA2.TableA)
-- ==============================================================================

-- 將當前 Session 切換到預設的可插拔資料庫 (FREEPDB1)
ALTER SESSION SET CONTAINER=FREEPDB1;

-- 建立主帳號 (也就是主資料庫 Schema)
-- 備註：帳號名稱若有包含連字號 (-) 必須使用雙引號包覆，否則會語法錯誤
CREATE USER "dot-net-minimal-api" IDENTIFIED BY "DotNetMinimalApiAbc123";

-- 賦予連線 (CONNECT)、資源建立權 (RESOURCE)，以及最高管理員權限 (DBA)
-- 擁有 DBA 權限後，該帳號就能存取 FREEPDB1 內任意其他的 Schema 空間
GRANT CONNECT, RESOURCE, DBA TO "dot-net-minimal-api";

-- 開放該帳號(Schema)無限制的儲存空間配額，允許其自由建立 Tables
ALTER USER "dot-net-minimal-api" QUOTA UNLIMITED ON USERS;

-- ==============================================================================
-- (補充範例) 若您未來需要新增「第二個資料庫空間」，只要取消下方註解即可建立：
-- CREATE USER "DB_LOGS" IDENTIFIED BY "RandomPassword123";
-- ALTER USER "DB_LOGS" QUOTA UNLIMITED ON USERS;
-- 
-- 屆時，您的主帳號 "dot-net-minimal-api" 因為擁有 DBA 權限，
-- 可以直接跨區存取 DB_LOGS 的資料表。
-- 例如在 EF Core 中可這樣設定: builder.ToTable("LogEntries", "DB_LOGS");
-- ==============================================================================

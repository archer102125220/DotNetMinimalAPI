-- 建立使用者並設定密碼
CREATE USER "dot-net-minimal-api" WITH PASSWORD 'DotNetMinimalApiAbc123';

-- 授權資料庫存取權限給該使用者
GRANT ALL PRIVILEGES ON DATABASE dot_net_mvc_web_db TO "dot-net-minimal-api";

-- 將資料庫的擁有者改為該使用者，確保有完整的控制權
ALTER DATABASE dot_net_mvc_web_db OWNER TO "dot-net-minimal-api";

-- 授權 public schema 的所有權限給該使用者
GRANT ALL PRIVILEGES ON SCHEMA public TO "dot-net-minimal-api";

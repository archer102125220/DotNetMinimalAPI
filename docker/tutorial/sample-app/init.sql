# Postgres 初始化腳本範例
# 只要將此腳本掛載到 postgres image 指定的 docker-entrypoint-initdb.d 資料夾下
# 當資料庫容器「第一次」啟動時，就會自動執行這些 SQL 指令。

CREATE TABLE Users (
    Id SERIAL PRIMARY KEY,
    Username VARCHAR(50) NOT NULL,
    Email VARCHAR(100) NOT NULL,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

INSERT INTO Users (Username, Email) VALUES 
('admin', 'admin@example.com'),
('testuser', 'testuser@example.com');

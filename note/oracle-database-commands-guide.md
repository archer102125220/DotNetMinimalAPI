# Oracle Database 常用指令與操作指南

本指南整理了在開發環境中，與 Oracle Database 互動最常使用的各類指令。涵蓋了 Docker 容器操作、連線工具 (`sqlplus`) 以及一些 Oracle 特有的 SQL 語法。

## 1. Docker 容器管理指令

我們在本地端是透過 Docker 運行 Oracle 23ai Free 版本。

*   **啟動現有的 Oracle 容器**
    ```bash
    docker start dot-net-minimal-api-oracle-free-db
    ```
*   **停止 Oracle 容器**
    ```bash
    docker stop dot-net-minimal-api-oracle-free-db
    ```
*   **查看 Oracle 容器運行狀態**
    ```bash
    docker ps -a | grep oracle
    ```

## 2. 進入資料庫 (使用 SQL*Plus)

`sqlplus` 是 Oracle 官方的終端機連線工具。可以直接在 Docker 容器內執行它，進行資料庫操作。

*   **以一般使用者身份進入 SQL*Plus**
    ```bash
    docker exec -it dot-net-minimal-api-oracle-free-db sqlplus myuser/mypassword@//localhost:1521/FREEPDB1
    ```
*   **以系統管理員 (SYSDBA) 身份進入 SQL*Plus** (若需執行權限管理或建立使用者)
    ```bash
    docker exec -it dot-net-minimal-api-oracle-free-db sqlplus sys/myadminpassword@//localhost:1521/FREEPDB1 as sysdba
    ```

### SQL*Plus 內部常用快捷指令
當成功進入 `SQL> ` 提示字元後，可以使用以下系統指令：
*   `exit` 或 `quit`：離開 SQL*Plus 回到一般終端機。
*   `clear screen` 或 `cl scr`：清除畫面。
*   `describe [TableName];` 或 `desc [TableName];`：查看某張資料表的結構與欄位設計。

---

## 3. Oracle 常用 SQL 指令

Oracle 的 SQL 語法與 MySQL 或 SQL Server 有些微不同，以下整理開發時最常用到的指令。

### 查看結構與中繼資料 (Metadata)
*   **列出目前使用者擁有的所有資料表 (Tables)**
    ```sql
    SELECT table_name FROM user_tables;
    ```
*   **列出所有的 Sequence (序列/流水號)**
    ```sql
    SELECT sequence_name FROM user_sequences;
    ```

### 基本 CRUD 操作
*   **查詢資料 (注意：EF Core 建立的表通常需加雙引號)**
    ```sql
    SELECT * FROM "OracleDemoItems";
    ```
*   **分頁查詢 (Oracle 12c+ 的新語法)**
    ```sql
    -- 略過前 10 筆，取接下來的 5 筆資料
    SELECT * FROM "OracleDemoItems"
    ORDER BY "Id"
    OFFSET 10 ROWS FETCH NEXT 5 ROWS ONLY;
    ```
*   **新增資料**
    ```sql
    INSERT INTO "OracleDemoItems" ("Name", "Description", "CreatedAt")
    VALUES ('測試名稱', '這是一段描述', SYSDATE);
    ```
    > 💡 **提示**：`SYSDATE` 或是 `CURRENT_TIMESTAMP` 是 Oracle 取得當前資料庫系統時間的內建變數。

### 關聯查詢 (JOINs)
在 Oracle 中，JOIN 語法與標準 SQL 相同。特別注意因為 EF Core 的關係，表名和欄位名通常需要加雙引號。

> 💡 **語法解析：資料表別名 (Table Alias)**
> 在下面的範例中，你會看到 `"OracleDemoItems" i` 或 `"OracleDemoCategories" c`。
> 這裡的 `i` 和 `c` 是為資料表取的**簡短代稱 (別名)**。這樣在後面指定欄位時（例如 `i."CategoryId" = c."Id"`），就可以避免寫出冗長的完整表名，讓 SQL 語句更精簡易讀。

*   **INNER JOIN (內連接)**
    ```sql
    SELECT i."Name", c."Name" AS "CategoryName"
    FROM "OracleDemoItems" i
    INNER JOIN "OracleDemoCategories" c ON i."CategoryId" = c."Id";
    ```
*   **LEFT JOIN (左外連接)**
    ```sql
    SELECT i."Name", c."Name" AS "CategoryName"
    FROM "OracleDemoItems" i
    LEFT JOIN "OracleDemoCategories" c ON i."CategoryId" = c."Id";
    ```

### 子查詢 (Subqueries)
子查詢可以放在 `WHERE`, `SELECT`, 或是 `FROM` 中作為過濾條件或臨時表。
*   **WHERE 條件子查詢**
    ```sql
    SELECT * FROM "OracleDemoItems"
    WHERE "CategoryId" IN (
        SELECT "Id" FROM "OracleDemoCategories" WHERE "Name" = '測試分類'
    );
    ```
*   **EXISTS 子查詢** (通常效能較好，用於檢查關聯記錄是否存在)
    ```sql
    SELECT * FROM "OracleDemoCategories" c
    WHERE EXISTS (
        SELECT 1 FROM "OracleDemoItems" i WHERE i."CategoryId" = c."Id"
    );
    ```
    > 💡 **語法解析：為什麼寫 `SELECT 1`？**
    > 在 `EXISTS` 的括號內，資料庫只在乎「條件有沒有配對到任何一筆資料」，而**不在乎撈出什麼具體的欄位內容**。
    > 因此業界慣例會寫 `SELECT 1`，告訴資料庫：「只要有配對到，隨便丟個常數 1 給我就好」。這能讓查詢語意更明確，也可能帶來微小的效能提升。

### DUAL 虛擬表
Oracle 強制規定所有的 `SELECT` 語句都**必須**有 `FROM`。如果只想計算簡單的算式或取得時間，必須從內建的 `DUAL` 虛擬表中查詢：
```sql
SELECT SYSDATE FROM DUAL;
SELECT 1 + 1 FROM DUAL;
```

### MERGE 語句 (合併 / Upsert)
在 Oracle 中，如果想要實現「若資料存在則更新，不存在則新增」的邏輯（即 Upsert），會使用 `MERGE INTO` 語句。這在批次處理同步資料或避免重複寫入時非常實用。

```sql
MERGE INTO "OracleDemoItems" target
USING (
    -- 準備要 Upsert 的來源資料 (可以是從另一張表撈取，或用 DUAL 構造單筆)
    SELECT 100 AS "Id", '新測試商品' AS "Name", '新描述內容' AS "Description" FROM DUAL
) source
ON (target."Id" = source."Id")
WHEN MATCHED THEN
    -- 當 ON 條件成立 (資料已存在)，執行 UPDATE
    UPDATE SET 
        target."Name" = source."Name",
        target."Description" = source."Description"
WHEN NOT MATCHED THEN
    -- 當 ON 條件不成立 (資料不存在)，執行 INSERT
    INSERT ("Id", "Name", "Description", "CreatedAt")
    VALUES (source."Id", source."Name", source."Description", SYSDATE);
```

> 💡 **語法解析：**
> *   `MERGE INTO target`: 指定要操作的目標資料表。
> *   `USING source`: 提供資料來源。
> *   `ON (條件)`: 用來判斷資料是否已經存在的條件（通常是對應的 Primary Key 或 Unique Key）。
> *   `WHEN MATCHED THEN`: 指定條件符合時要做的 `UPDATE` 操作。**注意：不可以去更新 `ON` 條件裡面用到的欄位。**
> *   `WHEN NOT MATCHED THEN`: 指定條件不符合時要做的 `INSERT` 操作。

---

## 4. 常見問題與踩坑 (Gotchas)

1.  **區分大小寫 (Case Sensitivity)**
    在 Oracle 中，如果建表時沒有加雙引號，所有的名稱都會被轉成**全大寫**。但因為我們使用的是 EF Core，EF Core 預設會幫我們對欄位和表名加上雙引號，這會讓 Oracle 變成**嚴格區分大小寫**。
    所以下 SQL 指令時，請務必記得加上雙引號：
    ```sql
    -- ❌ 錯誤 (會報錯 Table or view does not exist)
    SELECT * FROM OracleDemoItems;
    
    -- ✅ 正確
    SELECT * FROM "OracleDemoItems";
    ```

2.  **IDENTITY 欄位與 EF Core 種子資料衝突 (ORA-00001)**
    如果在 EF Core 的 `OnModelCreating` 裡面使用了 `HasData` 手動指定 Id (如 Id=1, 2, 3) 來塞入種子資料，Oracle 內部的 Identity 流水號計數器**不會**自動跟著跳過這些 Id。當後續想要從應用程式 Insert 新資料時，資料庫可能會嘗試給它 Id=1，進而發生 Primary Key 重複 (`ORA-00001`) 的錯誤。
    *解決方法*：實務上，若要在 Oracle 使用 `HasData`，建議將固定種子資料的 Id 設為負數 (例如 `-1`, `-2`)，或者改用我們示範的 `DbInitializer.cs` 來動態新增種子資料。

3.  **應用程式執行單筆 SQL 語句時，結尾不需加分號 (Semicolon)**
    習慣 MySQL 或 SQL Server 的開發者，在寫 SQL 指令時常習慣在句尾加上分號 `;` 來做結尾。
    但在透過 C# 應用程式（例如使用 ADO.NET `OracleCommand` 或 Dapper）向 Oracle 發送**單筆** SQL 語句（如 `SELECT`, `INSERT`, `UPDATE`, `DELETE`）時，結尾**絕對不可以**加上分號，否則會觸發 `ORA-00911: invalid character` 錯誤。
    > 💡 **補充與例外：資料庫管理工具 (UI Tools)**
    > 如果您是使用 **DBeaver、DataGrip、Oracle SQL Developer、Navicat** 或是命令列的 **SQL*Plus** 等資料庫管理介面：
    > 這些工具通常**支援甚至要求**在句尾加上分號 `;`。原因是這些工具在解析我們輸入的一大串「SQL 腳本 (Script)」時，需要依靠分號來切割多筆獨立的 SQL 語句，然後在背後自動將分號剝除，再逐一發送給 Oracle 伺服器執行。
    > 
    > 此外，若是在 C# 程式碼中執行包含 `BEGIN ... END;` 的 PL/SQL 匿名區塊（Anonymous Block），區塊內部的敘述與 `END` 後方則**必須**包含分號。

    ```csharp
    // ❌ 錯誤 (加上分號會引發 ORA-00911)
    string sqlBad = "SELECT * FROM \"OracleDemoItems\";";
    var data = await connection.QueryAsync<Item>(sqlBad);
    
    // ✅ 正確
    string sqlGood = "SELECT * FROM \"OracleDemoItems\"";
    var data = await connection.QueryAsync<Item>(sqlGood);
    ```

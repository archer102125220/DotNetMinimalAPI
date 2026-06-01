#!/bin/bash
# 第一行的 `#!/bin/bash` 稱為 Shebang，用來告訴系統這個腳本必須使用 bash 來執行。

# -----------------------------------------------------------------------------
# 1. 在背景啟動 SQL Server
# -----------------------------------------------------------------------------
# /opt/mssql/bin/sqlservr 是 SQL Server 的主程式。
# 指令最後的 `&` 符號非常重要，它代表「把這個程序丟到背景執行 (Background process)」。
# 如果不加 `&`，腳本就會卡在這裡，永遠無法執行到後面的資料庫初始化步驟。
/opt/mssql/bin/sqlservr &

# `$!` 是一個 Bash 特殊變數，用來取得「最後一個在背景執行的程序的 PID (進程識別碼)」。
# 我們把 SQL Server 的 PID 存進變數 PID 裡，這在腳本的最後面會派上用場。
PID=$!

echo "Waiting for SQL Server to start..."

# -----------------------------------------------------------------------------
# 2. 自動尋找正確的 sqlcmd 工具路徑
# -----------------------------------------------------------------------------
# `sqlcmd` 是 SQL Server 提供的命令列工具，用來執行 SQL 語句。
# 因為不同版本的 MSSQL Image (例如 2019 與 2022)，內建的 sqlcmd 路徑可能不同。
# 這裡先預設為 mssql-tools18 (2022 版常見) 的路徑。
SQLCMD="/opt/mssql-tools18/bin/sqlcmd"

# `[ ! -f "$SQLCMD" ]` 意思是「如果這個檔案不存在」。
if [ ! -f "$SQLCMD" ]; then
    # 如果找不到 tools18 的路徑，就降級尋找舊版 mssql-tools 的路徑。
    SQLCMD="/opt/mssql-tools/bin/sqlcmd"
fi

# -----------------------------------------------------------------------------
# 3. 輪詢 (Polling) 等待 SQL Server 完全啟動
# -----------------------------------------------------------------------------
# `until ... do ... done` 是一個迴圈，它會「一直重複執行，直到 until 後面的指令成功為止」。
# 這裡我們用 sqlcmd 嘗試連線到本地端 (localhost)，並執行一個最簡單的查詢 `-Q "SELECT 1"`。
# `-U sa -P "${MSSQL_SA_PASSWORD}"`：使用最高權限的 sa 帳號與環境變數中的密碼登入。
# `-C`：(Trust Server Certificate) 告訴 sqlcmd 信任伺服器的 SSL 憑證，這在本地開發環境是必加的。
# `&> /dev/null`：把連線失敗時產生的錯誤訊息全部丟進黑洞 (隱藏起來)，保持終端機畫面乾淨。
until $SQLCMD -S localhost -U sa -P "${MSSQL_SA_PASSWORD}" -C -Q "SELECT 1" &> /dev/null
do
    echo "SQL Server is starting up..."
    sleep 2 # 暫停 2 秒後再試一次，避免過度消耗 CPU 資源
done

echo "SQL Server is up - running initialization script..."

# -----------------------------------------------------------------------------
# 4. 執行資料庫初始化腳本
# -----------------------------------------------------------------------------
# 既然 `SELECT 1` 成功了，代表 SQL Server 已經準備好接受正式的 SQL 指令。
# `-i /docker-entrypoint-initdb.d/init.sql`：代表以檔案 (-i, input) 形式匯入我們寫好的 init.sql 腳本並執行。
$SQLCMD -S localhost -U sa -P "${MSSQL_SA_PASSWORD}" -C -i /docker-entrypoint-initdb.d/init.sql

echo "Initialization finished."

# -----------------------------------------------------------------------------
# 5. 維持 Docker 容器的生命週期
# -----------------------------------------------------------------------------
# 這是 Docker 腳本中最關鍵的一步！
# Docker 容器的生命週期取決於「主程序 (PID 1)」。如果這個 bash 腳本執行完畢直接退出，
# Docker 就會認為「工作結束了」，然後把整個容器關掉 (SQL Server 也會跟著被強制關閉)。
#
# `wait $PID` 的作用是讓這個 Bash 腳本「暫停並停留在這裡」，持續監控剛才存下來的 SQL Server 程序。
# 只要 SQL Server 還在背景跑，這個腳本就不會結束，容器也就能一直保持在 Running 狀態。
wait $PID

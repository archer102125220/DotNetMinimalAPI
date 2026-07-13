# Docker Compose 使用教學

當一個應用程式包含多個服務（例如：Web 應用程式 + 資料庫 + 快取伺服器）時，如果每個都要用 `docker run` 一個個啟動會非常麻煩。

`Docker Compose` 是一個用來定義與執行多容器 Docker 應用程式的工具。我們只需要寫一份 YAML 檔案 (`docker-compose.yml`)，就可以用一個指令啟動所有的服務。

請參考 `sample-app/docker-compose.yml` 來搭配以下說明。

## 💡 關於新舊版指令的小知識 (V1 vs V2)

您可能會在網路上看到 `docker-compose` (有連字號) 和 `docker compose` (有空格) 兩種寫法：
- **`docker-compose`** (V1)：較舊的版本，通常是用 Python 寫的獨立套件。
- **`docker compose`** (V2)：現在的**官方新標準**，由 Go 語言重寫並直接整合進了 Docker CLI 中。

**所有**原本的指令都可以無縫改用有空格的新版寫法，以下教學皆以新版 (V2) 為標準。

## 常用指令

- **啟動所有服務 (並在背景執行)**
  ```bash
  docker compose up -d
  ```

- **停止並移除所有服務 (包含容器與網路)**
  ```bash
  docker compose down
  ```
  > 若要連同 Volume 裡的資料一起刪除，可加上 `-v` 參數：`docker compose down -v`

- **查看服務日誌**
  ```bash
  docker compose logs -f
  # 也可以看特定服務的： docker compose logs -f webapp
  ```

- **進入執行中的 Container (進入終端機)**
  ```bash
  # 格式：docker compose exec <服務名稱> <Shell名稱>
  docker compose exec webapp /bin/bash
  # 如果是 alpine 版本的 image，通常沒有 bash，請改用 sh：
  # docker compose exec db /bin/sh
  ```

## docker-compose.yml 結構解析

```yaml
version: '3.8' # Compose 檔案格式版本

services: # 定義要運行的各個容器
  
  web:
    build: 
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:8080" # 本機Port:容器Port
    depends_on:
      - db # 確保 db 服務先啟動
    environment:
      - ConnectionStrings__DefaultConnection=Host=db;Database=mydb;Username=myuser;Password=mypassword

  db:
    image: postgres:15-alpine
    environment:
      POSTGRES_USER: myuser
      POSTGRES_PASSWORD: mypassword
      POSTGRES_DB: mydb
    ports:
      - "5432:5432"
    volumes:
      - db_data:/var/lib/postgresql/data # 將資料庫檔案持久化保存

volumes: # 宣告有使用到的 Volume
  db_data:
```

### 關鍵概念
- **`services`**: 每一個 service 代表一個 Container。
- **`build` vs `image`**: 
  - `build`: 告訴 Docker Compose 需要根據哪個目錄的 `Dockerfile` 來現場編譯 Image。
  - `image`: 直接從 Docker Hub 或 Registry 下載現成的 Image。
- **`depends_on`**: 定義服務的啟動順序。
- **`environment`**: 設定傳入 Container 的環境變數。在 .NET 中可以用來覆寫 `appsettings.json` 的設定（例如連線字串）。
- **`volumes`**: 用來作資料持久化。如果沒有設定 volume，當 db 容器被刪除時，資料庫裡的資料也會一併消失。

# DotNetMinimalAPI 部署指南

這份指南將介紹如何將 `DotNetMinimalAPI` 專案部署到不同的環境中。本專案是一個基於 **.NET 10** 的 Minimal API 專案，並且已經啟用了 **AOT (Ahead-of-Time) 編譯** (`<PublishAot>true</PublishAot>`)，這意味著在發布時會將應用程式編譯為原生機器碼，帶來更快的啟動速度與更低的記憶體消耗。

## 1. 發布應用程式 (發布為原生 AOT)

啟用 AOT 後，發布的應用程式將是針對特定作業系統與系統架構的原生執行檔 (不需要在目標伺服器上安裝 .NET Runtime)。

### 本地發布測試 (Mac)

若要在本機直接發布測試，請在專案根目錄下執行：

```bash
dotnet publish -c Release
```

這會在其 `bin/Release/net10.0/[作業系統架構]/publish/` 資料夾下產生一個名為 `DotNetMinimalAPI` 的原生執行檔。

### 指定目標平台發布 (Cross-Compilation)

若要部署到 Linux 伺服器 (如 Ubuntu x64)，可以使用以下指令：

```bash
dotnet publish -c Release -r linux-x64 --self-contained
```
*(注意：在 macOS 或 Windows 上進行 AOT 的跨平台編譯通常需要依賴 Docker 或 WSL，因為 AOT 編譯器依賴於目標平台的 C++ 工具鏈。最簡單跨平台發布 AOT 的方式是使用 Docker 進行建置。)*

---

## 2. 透過 Docker 部署 (推薦)

Docker 是部署 .NET AOT 應用程式最一致也最方便的方法。

### 建立 `Dockerfile`

在專案根目錄下建立一個 `Dockerfile`：

```dockerfile
# 建立階段 (使用包含 AOT 建置工具鏈的 SDK 映像檔)
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
# 安裝 AOT 需要的依賴 (Alpine 環境)
RUN apk add --no-cache clang build-base zlib-dev

WORKDIR /src
COPY ["DotNetMinimalAPI.csproj", "./"]
RUN dotnet restore "./DotNetMinimalAPI.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet publish "DotNetMinimalAPI.csproj" -c Release -r linux-musl-x64 -o /app/publish

# 執行階段 (使用極小的 Alpine 映像檔，因為不需要 .NET Runtime)
FROM alpine:latest AS final
WORKDIR /app
# 安裝運行依賴
RUN apk add --no-cache libstdc++
COPY --from=build /app/publish .

# 設定非 root 使用者執行以增加安全性
RUN adduser -D myuser
USER myuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["./DotNetMinimalAPI"]
```

### 建置與執行 Docker 映像檔

```bash
# 建置 Docker 映像檔
docker build -t dotnet-minimal-api .

# 執行 Docker 容器 (將主機的 8080 port 映射到容器的 8080 port)
docker run -d -p 8080:8080 --name minimal-api dotnet-minimal-api
```

---

## 3. 傳統 Linux 伺服器部署 (如 Ubuntu + Nginx)

若不使用 Docker，直接在 Linux 伺服器上部署也是可行的。

### 步驟 1：在建置環境發布檔案
透過上述的 Docker 建置或在 Linux CI/CD (例如 GitHub Actions) 環境中執行 `dotnet publish -c Release -r linux-x64`，取得編譯好的原生執行檔。

### 步驟 2：上傳至伺服器
將 `publish` 目錄下的檔案透過 SCP 或 SFTP 上傳到 Linux 伺服器（例如 `/var/www/DotNetMinimalAPI`）。

### 步驟 3：設定 systemd 服務
為了讓應用程式能在背景持續執行並在系統重啟時自動啟動，請在 `/etc/systemd/system/` 建立一個名為 `dotnet-minimal-api.service` 的檔案：

```ini
[Unit]
Description=DotNetMinimalAPI Service

[Service]
WorkingDirectory=/var/www/DotNetMinimalAPI
ExecStart=/var/www/DotNetMinimalAPI/DotNetMinimalAPI
Restart=always
# 發生崩潰後等待 10 秒重新啟動
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=dotnet-minimal-api
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000

[Install]
WantedBy=multi-user.target
```

啟動並啟用服務：
```bash
sudo systemctl enable dotnet-minimal-api.service
sudo systemctl start dotnet-minimal-api.service
sudo systemctl status dotnet-minimal-api.service
```

### 步驟 4：設定 Nginx 反向代理 (Reverse Proxy)
編輯 Nginx 設定檔 (如 `/etc/nginx/sites-available/default`)：

```nginx
server {
    listen 80;
    server_name example.com; # 替換為你的網域或 IP

    location / {
        proxy_pass         http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }
}
```

重新載入 Nginx：
```bash
sudo nginx -t
sudo systemctl reload nginx
```

現在你的 Minimal API 已經成功部署並對外提供服務了！

---

## 注意事項

- **AOT 限制**：因為專案啟用了 Native AOT，某些依賴於執行期反射 (Runtime Reflection) 或動態程式碼生成 (Dynamic Code Generation) 的套件可能無法正常運作。部署前務必在本地進行充分測試。
- **Swagger/OpenAPI**：在 `Production` 環境下，預設通常會關閉 Swagger (`if (app.Environment.IsDevelopment())`)，請確保在正式環境的配置符合安全規範。

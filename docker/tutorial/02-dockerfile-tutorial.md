# Dockerfile 撰寫教學

`Dockerfile` 是一份文字檔，裡面包含了建立 Docker Image 所需的所有指令，也就是 Docker Image 的設定檔。我們可以把它想像成是一份「食譜」，告訴 Docker 該如何一步步建構我們的應用程式環境。

請參考 `sample-app/Dockerfile` 來搭配以下說明。

## 常見的 Dockerfile 指令

- **`FROM`**: 指定基礎映像檔 (Base Image)。每個 Dockerfile 都必須以 `FROM` 開頭。
  ```dockerfile
  FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
  ```

- **`WORKDIR`**: 設定工作目錄。後續的 `RUN`、`CMD`、`COPY` 等指令都會在這個目錄下執行。
  ```dockerfile
  WORKDIR /src
  ```

- **`COPY`**: 將本機的檔案或目錄複製到 Image 的檔案系統中。
  ```dockerfile
  COPY ["MyProject.csproj", "./"]
  COPY . .
  ```

- **`RUN`**: 在 Image 建置階段執行指令 (例如：還原 NuGet 套件、編譯程式碼)。
  ```dockerfile
  RUN dotnet restore "MyProject.csproj"
  RUN dotnet build -c Release -o /app/build
  ```

- **`EXPOSE`**: 宣告應用程式在容器內會監聽的 Port (僅為標示用途，實際對外開放仍需透過 `docker run -p`)。
  ```dockerfile
  EXPOSE 8080
  ```

- **`ENTRYPOINT` / `CMD`**: 指定容器啟動時預設要執行的指令。
  ```dockerfile
  ENTRYPOINT ["dotnet", "MyProject.dll"]
  ```

## .NET 專案的多階段建置 (Multi-stage Build)

在我們的範例中，使用了多階段建置。這是一種最佳實踐：
1. **Build Stage**: 使用包含完整 SDK 的 Image (`dotnet/sdk`) 來編譯與發布應用程式。
2. **Runtime Stage**: 使用較輕量的 Runtime Image (`dotnet/aspnet`) 作為最終產出。

這樣做可以大幅減少最終 Image 的體積，並提高安全性（因為原始碼與編譯工具不會包含在最終映像檔中）。

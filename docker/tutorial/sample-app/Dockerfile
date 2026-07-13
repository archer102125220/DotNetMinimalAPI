# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# 1. Base stage: 用於最終運行應用程式的輕量環境
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# 2. Build stage: 包含 .NET SDK，用來編譯與還原套件
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# 複製專案檔並還原套件
# 這裡先只複製 csproj 檔，利用 Docker 的 cache 機制加速建置
COPY ["SampleWebApp.csproj", "./"]
RUN dotnet restore "./SampleWebApp.csproj"

# 複製其餘原始碼
COPY . .
WORKDIR "/src/."
RUN dotnet build "./SampleWebApp.csproj" -c $BUILD_CONFIGURATION -o /app/build

# 3. Publish stage: 將編譯結果發佈
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SampleWebApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 4. Final stage: 將發佈後的檔案放入 Base Image 中
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SampleWebApp.dll"]

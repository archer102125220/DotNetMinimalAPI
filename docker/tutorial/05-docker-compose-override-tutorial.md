# 覆寫設定 (docker-compose.override.yml) 教學

在實際的團隊開發中，我們通常會有一份基礎的 `docker-compose.yml`。這份基礎檔案定義了系統運作的「最基本要求」（例如有哪些服務、網路、依賴關係），這份檔案通常是被設計為共通的基礎設定。

然而，每位開發者在自己「本機」開發時，可能會需要一些特別的設定，例如：
1. 把本機特定的 Port 映射給容器。
2. 強制開啟 Debug 模式或把環境切成 Development。
3. 掛載本機的 Source Code 來做 Hot Reload (熱重載)，而不是每次都花時間重建 Image。

為了不污染共用的 `docker-compose.yml`，Docker 提供了天然的 `override` 機制。

---

## 運作原理

當你在含有這兩個檔案的資料夾執行啟動指令時：
```bash
docker compose up -d
```

Docker 預設會**自動**去尋找以下這兩個檔案：
1. `docker-compose.yml`
2. `docker-compose.override.yml`

Docker 會將兩者的設定**自動合併 (Merge)**。如果兩邊設定了相同的屬性，**`override.yml` 的設定會覆寫掉 `docker-compose.yml` 中的同名設定**；如果是不同的設定，則會累加。

---

## 範例解析

假設在 `docker-compose.yml` 中，我們的 webapp 沒有開放對外的 Port (或者只寫了最基本的設定)：
```yaml
# docker-compose.yml
services:
  webapp:
    image: my-production-image
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
```

在我們本機的 `docker-compose.override.yml` 裡，我們就可以這樣寫：
```yaml
# docker-compose.override.yml
services:
  webapp:
    # 1. 覆寫：不使用 Image，改用本機動態 Build
    build: 
      context: .
    # 2. 覆寫：修改環境變數
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
    # 3. 擴充：額外加上 Port 映射與本機資料夾掛載
    ports:
      - "8080:8080"
    volumes:
      - ./:/src
```

---

## 手動指定覆寫檔 (進階指令)

如果你除了 `.override.yml` 之外，還有為正式機準備的 `docker-compose.prod.yml`，你可以透過 `-f` 參數，在啟動時手動指定要合併哪些檔案 (越後面的檔案，覆寫優先權越高)：

```bash
# 啟動正式環境 (先讀基礎檔，再讀 prod 檔去覆寫它)
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

---

## 最佳實踐 💡

在多人協作的開發團隊中：
- **`docker-compose.yml`**：加入 Git 版控，存放大家共通、最乾淨、最基礎的設定。
- **`docker-compose.override.yml`**：通常會加入 `.gitignore`。團隊可以提供一份 `docker-compose.override.example.yml` 給新人參考，讓每位開發者在自己的電腦上改名成 `.override.yml`，寫入自己專屬的本機開發設定。

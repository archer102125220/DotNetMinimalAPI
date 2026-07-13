# CI/CD 平台中的環境變數與機密管理

既然我們將 `.env` 檔案加入了 `.gitignore`，那就代表版控伺服器（如 GitHub、GitLab）上是沒有 `.env` 檔案的。

那麼問題來了：**當 CI/CD 系統（Runner）從 GitHub 抓下程式碼準備打包或部署時，它要怎麼知道正式環境的資料庫密碼？**

答案是：**所有的 CI/CD 平台都提供了「機密管理 (Secrets Management)」功能。** 
我們會在平台的網頁後台手動輸入這些密碼，然後在 CI/CD 腳本執行時，動態地把它們注入到環境中，或是當場產生出一個暫時的 `.env` 檔案給 Docker 使用。

---

## 前 3 大熱門 CI/CD 工具實作範例

以下用業界最常見的三大工具，示範如何將雲端後台的機密變數，動態寫入成 `.env` 檔供 Docker 使用：

### 1. GitHub Actions
**設定方式**：
到 GitHub Repository 網頁 -> `Settings` -> `Secrets and variables` -> `Actions` -> 新增一個 `Repository secret`（例如命名為 `DB_PASSWORD`）。

**腳本範例 (`.github/workflows/deploy.yml`)**：
```yaml
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout code
        uses: actions/checkout@v3

      - name: Create .env file dynamically
        # 透過 echo 將機密變數寫入暫時的 .env 檔案中
        run: |
          echo "DB_USER=postgres" >> .env
          echo "DB_PASSWORD=${{ secrets.DB_PASSWORD }}" >> .env
          echo "DB_NAME=SampleDb" >> .env

      - name: Deploy via Docker Compose
        # 此時 Runner 當前目錄已經有剛產生的 .env 了，docker compose 會自動吃進去
        run: docker compose up -d
```

### 2. GitLab CI/CD
**設定方式**：
到 GitLab Repository 網頁 -> `Settings` -> `CI/CD` -> `Variables` -> 新增變數（記得勾選 `Masked` 屬性，防止密碼不小心印在 Log 裡被看到）。

**腳本範例 (`.gitlab-ci.yml`)**：
```yaml
deploy_job:
  stage: deploy
  script:
    # GitLab CI 會自動將後台設定的變數轉為 Linux 原生環境變數
    - echo "DB_USER=postgres" >> .env
    - echo "DB_PASSWORD=$DB_PASSWORD" >> .env
    - echo "DB_NAME=SampleDb" >> .env
    - docker compose up -d
```

### 3. Jenkins
**設定方式**：
到 Jenkins 後台 -> `Manage Jenkins` -> `Credentials` -> 新增一個 `Secret text` 憑證。

**腳本範例 (`Jenkinsfile`)**：
```groovy
pipeline {
    agent any
    stages {
        stage('Deploy') {
            steps {
                // 使用 withCredentials 插件安全地提取機密，並暫時放入 DB_PASSWORD 變數中
                withCredentials([string(credentialsId: 'prod_db_password', variable: 'DB_PASSWORD')]) {
                    sh '''
                    echo "DB_USER=postgres" >> .env
                    echo "DB_PASSWORD=$DB_PASSWORD" >> .env
                    echo "DB_NAME=SampleDb" >> .env
                    docker compose up -d
                    '''
                }
            }
        }
    }
}
```

---

## 業界前 10 大熱門 CI/CD 工具排行榜

CI/CD 工具百家爭鳴，選擇通常取決於你使用的版控平台、基礎架構（如是否全上雲）以及企業授權。以下是業界最知名、市佔率最高的前 10 名工具：

1. **GitHub Actions**：近年成長最快，因為與 GitHub 深度整合，且生態系套件極多，開源專案幾乎全面採用。
2. **GitLab CI/CD**：功能極度強大且一體化，是許多企業將原始碼架設在內部地端 (On-premise) 伺服器時的首選。
3. **Jenkins**：老牌王者，開源免費、外掛生態系最龐大，能做到極致客製化，但維護設定的成本較高。
4. **Azure DevOps (Pipelines)**：微軟生態系，在企業界與 **.NET 開發圈**的市佔率極高，和 C# 專案整合度完美。
5. **CircleCI**：老牌的雲端 SaaS CI/CD 服務，以執行速度快與 YAML 設定相對容易聞名。
6. **Bitbucket Pipelines**：Atlassian 生態系，最大的優勢是可以跟自家的專案管理工具 Jira、Trello 深度連動。
7. **Travis CI**：曾經是 GitHub 開源專案的標配，但在 GitHub Actions 崛起後，市佔率有明顯下滑。
8. **AWS CodePipeline**：Amazon 原生工具，適合整間公司的基礎架構都綁定在 AWS 上的團隊。
9. **Argo CD**：這幾年異軍突起，有別於傳統 CI，它是專注於 Kubernetes (K8s) 部署的 GitOps 王牌工具。
10. **TeamCity**：JetBrains (開發 IntelliJ, Rider 的公司) 推出的企業級 CI 伺服器，介面美觀，IDE 整合與除錯功能極佳。

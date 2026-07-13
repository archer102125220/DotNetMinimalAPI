# Docker 基礎指令教學

這份文件整理了開發過程中經常會使用到的 Docker 指令。

## 1. 影像 (Image) 相關指令

- **搜尋 Image**
  ```bash
  docker search <image_name>
  ```
- **下載 Image**
  ```bash
  docker pull <image_name>:<tag>
  ```
- **列出本地端所有的 Image**
  ```bash
  docker images
  ```
- **刪除 Image**
  ```bash
  docker rmi <image_name>
  ```
- **建立 Image (需有 Dockerfile)**
  ```bash
  docker build -t <your_image_name>:<tag> .
  ```

## 2. 容器 (Container) 相關指令

- **建立並執行 Container**
  ```bash
  docker run -d --name <container_name> -p <host_port>:<container_port> <image_name>
  ```
  - `-d`: 在背景執行
  - `--name`: 指定容器名稱
  - `-p`: 綁定本機與容器的 Port

- **列出執行中的 Container**
  ```bash
  docker ps
  ```
- **列出所有的 Container (包含已停止的)**
  ```bash
  docker ps -a
  ```
- **停止 Container**
  ```bash
  docker stop <container_name_or_id>
  ```
- **啟動已停止的 Container**
  ```bash
  docker start <container_name_or_id>
  ```
- **重啟 Container**
  ```bash
  docker restart <container_name_or_id>
  ```
- **刪除 Container (必須先停止)**
  ```bash
  docker rm <container_name_or_id>
  ```
- **進入執行中的 Container**
  ```bash
  docker exec -it <container_name_or_id> /bin/bash
  # 若是 alpine 系統通常使用 /bin/sh
  ```

## 3. 系統清理與其他指令

- **查看 Docker 資源使用情況**
  ```bash
  docker stats
  ```
- **查看 Container 的 Log**
  ```bash
  docker logs -f <container_name_or_id>
  ```
- **清理所有未使用的資源 (包含已停止的容器、未被標記的影像、未使用的網路)**
  ```bash
  docker system prune
  ```

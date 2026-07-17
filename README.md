# Interface JSON Compare Platform

接口响应 JSON 比较平台，提供 JSON、接口和批量比较能力，支持规则配置、历史记录与差异报告导出。

## 本地运行

### API

```powershell
dotnet run --project backend/JsonDiffPlatform.Api/JsonDiffPlatform.Api.csproj --urls http://localhost:5297
```

API 默认地址为 `http://localhost:5297`，数据保存在 `backend/JsonDiffPlatform.Api/data`。

### Web

```powershell
cd frontend
npm install
npm run dev
```

打开 `http://localhost:5173`。

## 项目结构

- `backend/JsonDiffPlatform.Api`：ASP.NET Core 10 Web API、比较引擎、历史与报告服务。
- `frontend`：Vue 3 + TypeScript + Element Plus 工作台。
- `docker-compose.yml`：前后端容器化启动配置。

## API

主要接口：

- `POST /api/compare/json`
- `POST /api/compare/interface`
- `POST /api/compare/batch`
- `GET /api/history`
- `GET /api/history/{id}`
- `POST /api/config/profile`
- `GET /api/config/profile/{name}`
- `GET /api/report/{id}?format=html|markdown|csv|excel|pdf|json`


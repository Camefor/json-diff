# 接口响应 JSON 比较平台（Interface JSON Compare Platform）

> 需求规格说明书（SRS）V1.0

## 1. 项目背景

### 1.1 项目名称

Interface JSON Compare Platform（接口响应比较平台）

### 1.2 建设背景

在老项目迁移、微服务拆分、新旧接口切换、第三方接口升级、接口回归测试过程中，需要验证新旧接口返回
JSON 是否一致。

现有人工方式存在： - JSON 数据量大，人工核对困难 -
数组顺序变化导致误判 - 无法忽略动态字段 - 无法批量比较 -
无法导出标准报告

建设目标：开发一套专门用于接口迁移验证的 JSON 比较平台。

## 2. 技术架构

### 前端

-   Vue3
-   TypeScript
-   Element Plus
-   Pinia
-   Axios
-   Monaco Editor
-   JsonEditor

### 后端

-   ASP.NET Core 9 Web API
-   C#
-   JsonDiffPatch.Net
-   System.Text.Json
-   MemoryCache
-   Serilog
-   OpenAPI

### 数据库

-   SQLite（默认）
-   MySQL
-   SQL Server
-   PostgreSQL

### 部署

-   Docker
-   Nginx
-   Linux / Windows

## 3. 功能模块

-   JSON 比较
-   接口比较
-   批量比较
-   历史记录
-   配置中心
-   差异报告
-   系统设置

## 4. 核心功能

### 4.1 JSON 编辑器

-   JSON 校验
-   自动格式化
-   语法高亮
-   折叠/展开
-   搜索定位

### 4.2 比较规则

-   Key / Value / Type 比较
-   Null 策略
-   数值容差
-   浮点误差
-   数组顺序控制

### 4.3 忽略字段

支持 JSONPath、通配符、正则表达式。

### 4.4 字段白名单

仅比较指定字段。

### 4.5 字段映射

例如： - price → lastPrice - code → symbol

### 4.6 数组主键比较

支持按 id、code、uuid、symbol 等主键匹配数组元素，避免顺序变化误报。

### 4.7 Diff 展示

-   Tree 视图
-   Table 视图
-   Summary 统计

## 5. 接口比较

支持配置： - URL - Method - Header - Query - Body

自动调用新旧接口并生成 Diff。

## 6. 批量比较

支持： - 导入接口列表 - 批量执行 - 批量导出报告

## 7. 导出格式

-   Excel
-   CSV
-   Markdown
-   HTML
-   PDF

## 8. API 设计

  Method   API                          说明
  -------- ---------------------------- -----------
  POST     /api/compare/json            比较 JSON
  POST     /api/compare/interface       比较接口
  POST     /api/compare/batch           批量比较
  GET      /api/history                 历史记录
  GET      /api/history/{id}            历史详情
  POST     /api/config/profile          保存配置
  GET      /api/config/profile/{name}   加载配置
  GET      /api/report/{id}             下载报告

## 9. 性能要求

-   10MB JSON ≤ 1 秒
-   50MB JSON ≤ 5 秒（目标）
-   支持百万级节点比较
-   支持异步任务

## 10. V2.0 规划

-   Swagger/OpenAPI 导入
-   Postman Collection 导入
-   Mock 数据
-   CI/CD 集成
-   自定义比较规则
-   可视化统计大盘
-   Diff 引擎 NuGet 化

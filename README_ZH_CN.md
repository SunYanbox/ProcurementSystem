# 小型办公用品采购系统

基于 ASP.NET Core Web API 重写的办公用品采购系统（原版为 Django + DRF，见 `docs/django-baseline.md`）。

## 技术栈

- **框架**：ASP.NET Core Web API（.NET 10）
- **ORM**：Entity Framework Core
- **数据库**：SQLite（开发期），后续可切换 MySQL / SQL Server
- **认证**：JWT（访问令牌 + 刷新令牌轮换）
- **前端**：Vue 3 + Vite + TypeScript + Element Plus（`ProcurementSystem.Vue`）

## 目录结构

- **ProcurementSystem/** — 主项目（Web API）
  - `Controllers/` — HTTP 层：接收请求、解析参数、返回响应
  - `Models/` — 实体类：对应数据库表
  - `Data/` — 数据访问层：DbContext、EF Core 配置
  - `DTOs/` — 数据传输对象：API 输入/输出契约
  - `Services/` — 业务逻辑层：审批流、权限、库存联动等
  - `Program.cs` — 应用入口：DI 注册、请求管道配置
  - `appsettings.json` — 配置文件（连接字符串、JWT 生命周期等）
  - `appsettings.Development.json` — 开发环境配置（被 gitignore，需自行创建）
- **ProcurementSystem.Tests/** — 单元测试（xUnit + SQLite 内存库）
- **ProcurementSystem.Vue/** — 前端（Vue 3 + Vite + TS + Element Plus）
- **DevTools/** — 开发辅助工具（种子账号重置等）
- **Postman/** — Postman 接口集合
- **docs/** — `django-baseline.md`（Django 原版需求基线）

### 各目录职责

| 目录 | 职责 | 对应 Django 概念 | 示例 |
| --- | --- | --- | --- |
| `Models/` | 实体类，映射数据库表 | `models.py` | `Department.cs`、`ProcurementRequest.cs` |
| `Data/` | DbContext 及 EF Core 配置 | 数据库连接管理 | `ProcurementDbContext.cs` |
| `Controllers/` | HTTP 入口，协议转换 | `views.py`（仅 HTTP 部分） | `ProcurementRequestsController.cs` |
| `Services/` | 业务规则，可测试的纯逻辑 | `views.py` 中的业务逻辑（新版抽离） | `ProcurementRequestService.cs` |
| `DTOs/` | API 输入/输出契约，隔离内部实体 | DRF Serializer | `ProcurementRequestDto.cs` |
| `Middleware/`（规划中） | 请求管道中的横切关注点 | Django Middleware | 异常处理、审计日志 |

上表“示例”列为实际模块对应的类名。

### 分层原则

- **Controller 要薄**：只做参数解析、调用 Service、翻译 HTTP 状态码，不写业务规则。
- **业务逻辑在 Service**：审批流、RBAC 权限、状态机、库存联动等复杂规则放在 `Services/`。
- **DTO 隔离实体**：内部实体（含敏感字段）不直接暴露给客户端，通过 DTO 控制输入输出。
- **Data 只管持久化**：DbContext 只负责表映射和查询，不包含业务判断。

## 开发环境配置（appsettings.Development.json）

`appsettings.Development.json` **不会提交到 Git**（`.gitignore` 已排除），因为它包含开发用的 JWT 签名密钥。新克隆仓库后需手动创建此文件，应用才能签发和验证 JWT。

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Jwt": {
    "Key": "dev-only-super-secret-key-change-me-in-production-0123456789",
    "Issuer": "ProcurementSystem",
    "Audience": "ProcurementSystem",
    "AccessTokenMinutes": 2,
    "RefreshTokenDays": 7
  }
}
```

| 键 | 含义 |
| --- | --- |
| `Jwt:Key` | HMAC-SHA256 签名密钥，至少 32 字节；仅开发环境可见，生产密钥经环境变量注入 |
| `Jwt:Issuer` / `Jwt:Audience` | token 签发方与受众标识，与 `appsettings.json` 一致 |
| `Jwt:AccessTokenMinutes` | 访问令牌有效期（分钟）；开发 2 分钟便于观察过期行为，生产 15 分钟 |
| `Jwt:RefreshTokenDays` | 刷新令牌有效期（天）；过期后需重新登录 |

> 生产环境的 `Jwt:Key` 不要写入任何被 Git 跟踪的文件，应通过环境变量或密钥管理服务注入。

## 前端（ProcurementSystem.Vue）

- **技术栈**：Vue 3 + Vite + TypeScript + Element Plus（按需导入）
- **包管理器**：统一使用 **pnpm**（禁止 npm/yarn，见 `AGENTS.md`）
- **权限控制**：菜单、首页卡片、路由三层拦截，普通员工不显示管理页面
- **Token 处理**：axios 拦截器在 401 时自动刷新访问令牌并重试原请求

常用命令（在 `ProcurementSystem.Vue` 目录下）：

```bash
pnpm install   # 安装依赖
pnpm dev       # 开发模式（Vite proxy 将 /api 转发到 http://localhost:5074）
pnpm build     # 生产构建
```

## 数据库说明

开发期使用 SQLite，连接字符串在 `appsettings.json`：

```json
{
  "ConnectionStrings": {
    "ProcurementDb": "Data Source=procurement.db"
  }
}
```

后续切换 MySQL / SQL Server 时，仅需：
1. 安装对应 EF Core provider 包
2. 修改连接字符串
3. 重新执行迁移

## 迁移对照

| 旧版（Django） | 新版（ASP.NET Core） |
| --- | --- |
| Django app 模块化架构 | 文件夹分层（Models / Data / Services / Controllers） |
| Django ORM | EF Core |
| DRF Serializer | DTOs + JSON 序列化 |
| `views.py`（业务 + HTTP 混合） | Controller（HTTP）+ Service（业务）分离 |
| `permissions.py` 自定义权限类 | ASP.NET Core Policy / Authorization |
| `makemigrations` + `migrate` | `dotnet ef migrations` + `database update` |

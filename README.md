# 小型办公用品采购系统

基于 ASP.NET Core Web API 重写的办公用品采购系统（原版为 Django + DRF，见 `docs/django-baseline.md`）。

## 技术栈

- **框架**：ASP.NET Core Web API（.NET 10）
- **ORM**：Entity Framework Core
- **数据库**：SQLite（开发期），后续可切换 MySQL / SQL Server
- **认证**：JWT（规划中）

## 目录结构

```
ProcurementSystem/
├── ProcurementSystem/              # 主项目（Web API）
│   ├── Controllers/                # HTTP 层：接收请求、解析参数、返回响应
│   ├── Models/                     # 实体类：对应数据库表
│   ├── Data/                       # 数据访问层：DbContext、EF Core 配置
│   ├── DTOs/                       # 数据传输对象：API 输入/输出契约
│   ├── Services/                   # 业务逻辑层：审批流、权限、库存联动等
│   ├── Program.cs                  # 应用入口：DI 注册、请求管道配置
│   ├── appsettings.json            # 配置文件（连接字符串等）
│   └── ProcurementSystem.csproj
├── docs/
│   └── django-baseline.md          # Django 原版需求基线（迁移对照标准）
├── ProcurementSystem.slnx
├── .gitignore
└── README.md
```

### 各目录职责

| 目录 | 职责 | 对应 Django 概念 | 示例 |
| --- | --- | --- | --- |
| `Models/` | 实体类，映射数据库表 | `models.py` | `Department.cs`、`ProcurementRequest.cs` |
| `Data/` | DbContext 及 EF Core 配置 | 数据库连接管理 | `ProcurementDbContext.cs` |
| `Controllers/` | HTTP 入口，协议转换 | `views.py`（仅 HTTP 部分） | `ProcurementRequestsController.cs` |
| `Services/` | 业务规则，可测试的纯逻辑 | `views.py` 中的业务逻辑（新版抽离） | `ProcurementService.cs` |
| `DTOs/` | API 输入/输出契约，隔离内部实体 | DRF Serializer | `ProcurementRequestDto.cs` |
| `Middleware/`（规划中） | 请求管道中的横切关注点 | Django Middleware | 异常处理、审计日志 |

当前 Todo 分层示例对应文件：`Models/Todo.cs`、`Data/TodoDb.cs`、`Controllers/TodoController.cs`、`Services/TodoService.cs`、`DTOs/TodoDtos.cs`；上表“示例”列为后续业务模块的规划类名。

### 分层原则

- **Controller 要薄**：只做参数解析、调用 Service、翻译 HTTP 状态码，不写业务规则。
- **业务逻辑在 Service**：审批流、RBAC 权限、状态机、库存联动等复杂规则放在 `Services/`。
- **DTO 隔离实体**：内部实体（含敏感字段）不直接暴露给客户端，通过 DTO 控制输入输出。
- **Data 只管持久化**：DbContext 只负责表映射和查询，不包含业务判断。

## 开发阶段

- [x] 初始化 Web API 项目
- [x] 安装 EF Core + SQLite
- [x] 建立 Todo 示例（验证分层结构）
- [ ] 按 `docs/django-baseline.md` 优化 API 设计并定义实体类
- [ ] 建立 `ProcurementDbContext` 并执行首次迁移
- [ ] 实现用户模块（users）
- [ ] 实现库存模块（warehouse）
- [ ] 实现采购申请模块（procurement_requests）
- [ ] 实现 RBAC 权限控制
- [ ] 实现采购状态机
- [ ] 实现库存联动
- [ ] 添加单元测试与集成测试

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
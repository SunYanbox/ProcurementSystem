# API 设计总览

本文档定义小型办公用品采购系统 C# Web API 的通用约定。各模块文档（`users.md`、`warehouse.md`、`procurement-requests.md`）遵循本总览中的规则。

## 1. 技术约定

- 框架：ASP.NET Core Web API（.NET 10）
- 数据访问：Entity Framework Core
- 认证：JWT Bearer
- 序列化：System.Text.Json，默认 camelCase
- 路由：属性路由，使用复数资源名

## 2. 角色与权限

系统仅有两级角色：

| 角色 | 标识 | 说明 |
| --- | --- | --- |
| 管理员 | `Admin` | 管理物品目录与库存、审批采购申请、执行采购入库、管理员工账号 |
| 员工 | `Employee` | 提交采购申请、查看自己的申请进度、浏览物品目录 |

权限通过 ASP.NET Core Policy / Authorization 实现。除非接口另有说明，否则所有接口均要求认证；角色要求写在各接口的「权限」一栏。

## 3. 路由约定

- 统一前缀 `/api`
- 使用复数资源名，例如 `/api/users`、`/api/warehouse/items`
- **不**使用 Django 风格的尾斜杠
- 单个资源使用路径参数：`/api/warehouse/items/{id}`
- 动作用子资源或动作段表达：`/api/procurement-requests/{id}/submit`、`/api/procurement-requests/{id}/audit`

## 4. 请求与响应约定

### 4.1 请求体

- Content-Type: `application/json`
- 字段采用 camelCase
- 路径参数与查询参数同样使用 camelCase，例如 `?status=pending&from=2026-01-01`

### 4.2 成功响应

| 情况 | 状态码 | 响应体 |
| --- | --- | --- |
| 查询成功 | 200 OK | 资源或资源列表 |
| 创建成功 | 201 Created | 新建资源 |
| 更新成功 | 200 OK | 更新后的资源 |
| 操作执行成功 | 200 OK | 操作结果或资源 |
| 删除成功 | 204 No Content | 无响应体 |

### 4.3 错误响应

统一使用 RFC 7807 `ProblemDetails` 风格：

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "物品数量必须大于 0",
  "traceId": "00-..."
}
```

| 状态码 | 语义 |
| --- | --- |
| 400 Bad Request | 请求体校验失败、业务规则冲突（如状态不允许该操作） |
| 401 Unauthorized | 未认证或 token 无效/过期 |
| 403 Forbidden | 已认证但角色无权执行该操作 |
| 404 Not Found | 资源不存在 |
| 409 Conflict | 唯一性冲突（如用户名已存在） |

校验错误使用 `ValidationProblemDetails`，`errors` 为字段到错误列表的映射：

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "username": ["用户名不能为空"],
    "password": ["密码长度至少 8 位"]
  }
}
```

## 5. DTO 设计原则

DTO 是 API 的输入/输出契约，**不直接暴露数据库实体**。

- **输入 DTO**：只包含客户端可提交的字段；禁止传入服务器生成的字段（如 `id`、`createdAt`）和内部字段（如密码哈希、内部外键）。
- **输出 DTO**：只包含客户端需要且允许看到的字段；密码哈希、内部外键等敏感/内部字段一律不出现在输出 DTO 中。
- 关联对象一般展开为可读信息：例如输出 `departmentName` 而不是 `departmentId`；如确需 id，由文档明确标注。
- 输入字段必须标注必填/可选、类型、约束；输出字段标注来源与说明。

每个模块文档的「DTO 定义」小节会给出字段表，并用「可见性」列区分：

- `对外可见`：会出现在请求或响应 DTO 中
- `内部字段`：数据库中存在但不出现在 DTO 中

## 6. 认证

- 登录成功后返回 `accessToken`（短时效）与 `refreshToken`（长时效）。
- 除登录、注册、刷新 token 外，所有接口请求头携带：

```
Authorization: Bearer <accessToken>
```

- token 采用 JWT，`sub` 为用户 id，`role` 声明值为 `Admin` 或 `Employee`。

## 7. 模块文档索引

| 文档 | 内容 |
| --- | --- |
| `users.md` | 用户、登录、员工账号管理 |
| `warehouse.md` | 物品类型、物品目录、库存与出入库流水 |
| `procurement-requests.md` | 采购申请状态机、申请、审批、取消、采购入库 |

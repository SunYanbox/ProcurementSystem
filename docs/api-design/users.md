# 用户模块设计（users）

> 约定见 `overview.md`。本模块合并 Django 原版中的 `users`（认证账号）与 `workers`（员工档案）双表为单一 `User` 实体；弃用 `power` 权限表，角色用枚举表达。

## 1. 实体定义

### 1.1 Role 枚举

| 值 | 说明 |
| --- | --- |
| `Admin` | 管理员 |
| `Employee` | 员工 |

### 1.2 Department（部门）

| 字段 | 类型 | 说明 | 可见性 |
| --- | --- | --- | --- |
| Id | long | 主键 | 对外可见 |
| Name | string | 部门名称，唯一 | 对外可见 |

### 1.3 User（用户，合并账号与员工档案）

| 字段 | 类型 | 说明 | 可见性 |
| --- | --- | --- | --- |
| Id | long | 主键 | 对外可见 |
| Username | string | 用户名，唯一 | 对外可见 |
| PasswordHash | string | 密码哈希 | 内部字段，禁止出入 DTO |
| WorkId | string | 工号，唯一 | 对外可见 |
| Name | string | 姓名 | 对外可见 |
| Email | string? | 邮箱，唯一（可空） | 本人与管理员可见 |
| Phone | string? | 手机号，唯一（可空） | 本人与管理员可见 |
| DepartmentId | long | 部门外键 | 对外可见 |
| Role | Role | 角色枚举 | 对外可见 |
| Working | bool | 是否在职 | 对外可见 |
| AvatarUrl | string? | 头像地址 | 对外可见 |
| CreatedAt | DateTime | 创建时间 | 对外可见 |
| UpdatedAt | DateTime | 更新时间 | 对外可见 |

> `WorkId` 使用 `string` 而非整数：工号可能含前导零或字母，避免精度与格式问题。

## 2. DTO 定义

### 2.1 输入 DTO

#### LoginRequest

| 字段 | 必须 | 类型 | 说明 |
| --- | --- | --- | --- |
| username | 是 | string | 用户名 / 工号 / 手机号 / 邮箱 |
| password | 是 | string | 密码 |

#### RegisterRequest（员工自助注册）

| 字段 | 必须 | 类型 | 说明 |
| --- | --- | --- | --- |
| workId | 是 | string | 已存在的工号 |
| username | 是 | string | 用户名 |
| password | 是 | string | 密码 |
| passwordAgain | 是 | string | 确认密码 |

#### CreateUserRequest（管理员创建员工档案）

| 字段 | 必须 | 类型 | 说明 |
| --- | --- | --- | --- |
| workId | 是 | string | 工号 |
| name | 是 | string | 姓名 |
| email | 否 | string | 邮箱 |
| phone | 否 | string | 手机号 |
| departmentId | 是 | long | 部门 |
| role | 否 | string | `Admin` 或 `Employee`，默认 `Employee` |

#### UpdateUserRequest（管理员更新员工）

| 字段 | 必须 | 类型 | 说明 |
| --- | --- | --- | --- |
| name | 否 | string | 姓名 |
| email | 否 | string | 邮箱 |
| phone | 否 | string | 手机号 |
| departmentId | 否 | long | 部门 |
| role | 否 | string | 角色 |
| working | 否 | bool | 是否在职 |

> `workId` 创建后不可修改。

#### ChangePasswordRequest

| 字段 | 必须 | 类型 | 说明 |
| --- | --- | --- | --- |
| oldPassword | 是 | string | 原密码 |
| newPassword | 是 | string | 新密码 |

#### BindPhoneRequest

| 字段 | 必须 | 类型 | 说明 |
| --- | --- | --- | --- |
| phone | 是 | string | 手机号 |

### 2.2 输出 DTO

#### UserDto

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| id | long | 用户 id |
| username | string | 用户名 |
| workId | string | 工号 |
| name | string | 姓名 |
| email | string? | 邮箱（本人与管理员可见） |
| phone | string? | 手机号（本人与管理员可见） |
| departmentId | long | 部门 id |
| departmentName | string | 部门名称 |
| role | string | 角色 |
| working | bool | 是否在职 |
| avatarUrl | string? | 头像地址 |
| createdAt | string | 创建时间（ISO 8601） |

#### AuthResponse（登录响应）

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| accessToken | string | 访问令牌 |
| refreshToken | string | 刷新令牌 |
| user | UserDto | 当前用户信息 |

## 3. 接口规格

### 3.1 登录

- **POST** `/api/auth/login`
- **权限**：公开
- **请求体**：`LoginRequest`
- **成功**：200 + `AuthResponse`
- **失败**：400（用户名不存在或密码错误）

### 3.2 刷新 token

- **POST** `/api/auth/refresh`
- **权限**：公开
- **请求体**：`{ "refreshToken": "..." }`
- **成功**：200 + `{ "accessToken": "...", "refreshToken": "..." }`
- **失败**：401（token 无效或过期）

### 3.3 验证 token

- **POST** `/api/auth/verify`
- **权限**：公开
- **请求体**：`{ "token": "..." }`
- **成功**：200 + 空对象 `{}`
- **失败**：401

### 3.4 获取当前用户信息

- **GET** `/api/users/me`
- **权限**：已认证
- **成功**：200 + `UserDto`

### 3.5 员工自助注册

- **POST** `/api/auth/register`
- **权限**：公开
- **请求体**：`RegisterRequest`
- **成功**：201 + `UserDto`（不含 token）
- **失败**：400（密码不一致）、404（工号不存在）、409（用户名已存在、工号已绑定账号）
- **业务规则**：
  - 工号必须已由管理员创建
  - 该工号尚未绑定登录账号
  - 用户名唯一

### 3.6 管理员创建员工档案

- **POST** `/api/users`
- **权限**：`Admin`
- **请求体**：`CreateUserRequest`
- **成功**：201 + `UserDto`
- **失败**：400（参数校验）、409（工号 / 邮箱 / 手机号已存在）
- **业务规则**：创建后员工可凭工号自助注册账号（见 3.5）。

### 3.7 管理员获取员工列表

- **GET** `/api/users`
- **权限**：`Admin`
- **查询参数**：

| 参数 | 必须 | 类型 | 说明 |
| --- | --- | --- | --- |
| departmentId | 否 | long | 按部门筛选 |
| role | 否 | string | 按角色筛选 |
| working | 否 | bool | 按在职状态筛选 |
| search | 否 | string | 在姓名、工号、用户名、邮箱、手机号中搜索 |

- **成功**：200 + `UserDto[]`

### 3.8 管理员获取指定用户

- **GET** `/api/users/{id}`
- **权限**：`Admin`（员工仅能通过 3.4 查看自己）
- **成功**：200 + `UserDto`
- **失败**：404

### 3.9 管理员更新用户

- **PUT** `/api/users/{id}`
- **权限**：`Admin`
- **请求体**：`UpdateUserRequest`
- **成功**：200 + `UserDto`
- **失败**：400、404、409（邮箱 / 手机号冲突）

### 3.10 修改自己的密码

- **PUT** `/api/users/me/password`
- **权限**：已认证
- **请求体**：`ChangePasswordRequest`
- **成功**：204
- **失败**：400（原密码错误、新密码不满足复杂度）

### 3.11 绑定 / 修改手机号

- **PUT** `/api/users/me/phone`
- **权限**：已认证
- **请求体**：`BindPhoneRequest`
- **成功**：200 + `UserDto`
- **失败**：409（手机号已被占用）

### 3.12 解绑手机号

- **DELETE** `/api/users/me/phone`
- **权限**：已认证
- **成功**：200 + `UserDto`

### 3.13 上传头像

- **POST** `/api/users/me/avatar`
- **权限**：已认证
- **请求体**：`multipart/form-data`，字段 `file`（图片）
- **成功**：200 + `UserDto`
- **失败**：400（文件类型 / 大小不合法）

### 3.14 获取部门列表

- **GET** `/api/departments`
- **权限**：已认证
- **成功**：200 + `[{ "id": 1, "name": "财务部" }, ...]`

## 4. 权限矩阵

| 操作 | 员工 | 管理员 |
| --- | --- | --- |
| 登录 / 注册 / 刷新 / 验证 token | ✓ | ✓ |
| 查看自己信息、改密码、绑定 / 解绑手机号、传头像 | ✓（仅自己） | ✓（仅自己） |
| 查看 / 创建 / 更新员工 | ✗ | ✓ |
| 查看部门列表 | ✓ | ✓ |

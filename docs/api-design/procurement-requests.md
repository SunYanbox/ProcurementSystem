# 采购申请模块设计（procurement-requests）

> 约定见 `overview.md`。本模块定义采购申请的状态机、申请与审批流程、采购入库联动。

## 1. 状态机

```
Draft ──submit──▶ Pending ──approve──▶ Approved ──purchase──▶ Purchased
                   │                      │
                   │reject                │（可取消）
                   ▼                      ▼
                Rejected               Cancelled

Draft ──cancel──▶ Cancelled
Pending ──cancel──▶ Cancelled
```

| 状态 | 值 | 说明 |
| --- | --- | --- |
| 草稿 | `Draft` | 创建但未提交 |
| 申请中 | `Pending` | 已提交，等待审批 |
| 已通过 | `Approved` | 审批通过，待采购 |
| 已驳回 | `Rejected` | 审批驳回 |
| 已采购 | `Purchased` | 采购完成并已入库 |
| 已取消 | `Cancelled` | 员工自行取消 |

**允许的转换**：

| 当前状态 | 可转换到 | 触发者 |
| --- | --- | --- |
| `Draft` | `Pending`（提交）、`Cancelled`（取消） | 员工 |
| `Pending` | `Approved`、`Rejected`（审批）、`Cancelled`（取消） | 管理员 / 员工 |
| `Approved` | `Purchased`（采购入库）、`Cancelled`（取消） | 管理员 / 员工 |
| `Rejected` | —（终态） | — |
| `Purchased` | —（终态） | — |
| `Cancelled` | —（终态） | — |

> 取消不物理删除记录，保留审计痕迹。

## 2. 实体定义

### 2.1 ProcurementRequest（采购申请）

| 字段 | 类型 | 说明 | 可见性 |
| --- | --- | --- | --- |
| Id | long | 主键 | 对外可见 |
| SourceId | long | 申请人用户 id | 对外可见 |
| ItemId | long? | 目录内物品 id（目录外为 null） | 对外可见 |
| CustomItemName | string? | 目录外物品名称（目录内为 null） | 对外可见 |
| CustomSpecification | string? | 目录外物品规格（目录内为 null） | 对外可见 |
| Quantity | int | 申请数量，正整数 | 对外可见 |
| Purpose | string | 用途 | 对外可见 |
| Status | RequestStatus | 状态枚举 | 对外可见 |
| AuditedById | long? | 审核人 id | 对外可见 |
| AuditedAt | DateTime? | 审核时间 | 对外可见 |
| RefusalReason | string? | 驳回原因 | 对外可见 |
| PurchasedById | long? | 采购执行人 id | 对外可见 |
| PurchasedAt | DateTime? | 采购入库时间 | 对外可见 |
| CancelledAt | DateTime? | 取消时间 | 对外可见 |
| RequestedAt | DateTime | 提交时间（草稿为创建时间） | 对外可见 |
| UpdatedAt | DateTime | 最后更新时间 | 对外可见 |

> 目录内与目录外二选一：`ItemId` 非空时 `CustomItemName`、`CustomSpecification` 必须为空；反之亦然。

### 2.2 RequestStatus 枚举

| 值 | 说明 |
| --- | --- |
| `Draft` | 草稿 |
| `Pending` | 申请中 |
| `Approved` | 已通过 |
| `Rejected` | 已驳回 |
| `Purchased` | 已采购 |
| `Cancelled` | 已取消 |

## 3. DTO 定义

### 3.1 输入 DTO

#### CreateProcurementRequestRequest（创建草稿）

| 字段 | 必须 | 类型 | 说明 |
| --- | --- | --- | --- |
| itemId | 条件必填 | long? | 目录内物品 id（目录外时省略） |
| customItemName | 条件必填 | string? | 目录外物品名称（目录内时省略） |
| customSpecification | 条件必填 | string? | 目录外规格（目录内时省略） |
| quantity | 是 | int | 数量，> 0 |
| purpose | 是 | string | 用途 |

#### UpdateProcurementRequestRequest（编辑草稿）

| 字段 | 必须 | 类型 | 说明 |
| --- | --- | --- | --- |
| itemId | 否 | long? | 更换物品 |
| customItemName | 否 | string? | 目录外名称 |
| customSpecification | 否 | string? | 目录外规格 |
| quantity | 否 | int | 数量，> 0 |
| purpose | 否 | string | 用途 |

#### AuditProcurementRequestRequest（审批）

| 字段 | 必须 | 类型 | 说明 |
| --- | --- | --- | --- |
| decision | 是 | string | `approve` / `reject` |
| refusalReason | 条件必填 | string? | 驳回时必须填写 |

### 3.2 输出 DTO

#### ProcurementRequestDto

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| id | long | 申请 id |
| sourceId | long | 申请人 id |
| sourceName | string | 申请人姓名 |
| itemId | long? | 目录内物品 id |
| itemName | string? | 物品名称（目录内） |
| customItemName | string? | 目录外物品名称 |
| customSpecification | string? | 目录外规格 |
| quantity | int | 数量 |
| purpose | string | 用途 |
| status | string | 状态 |
| auditedByName | string? | 审核人姓名 |
| auditedAt | string? | 审核时间 |
| refusalReason | string? | 驳回原因 |
| purchasedByName | string? | 采购执行人姓名 |
| purchasedAt | string? | 采购入库时间 |
| requestedAt | string | 提交时间 |
| cancelledAt | string? | 取消时间 |

## 4. 接口规格

### 4.1 获取状态枚举

- **GET** `/api/procurement-requests/statuses`
- **权限**：公开
- **成功**：200 + `[ "Draft", "Pending", "Approved", "Rejected", "Purchased", "Cancelled" ]`

### 4.2 创建草稿

- **POST** `/api/procurement-requests`
- **权限**：已认证（员工 / 管理员）
- **请求体**：`CreateProcurementRequestRequest`
- **成功**：201 + `ProcurementRequestDto`
- **失败**：400（itemId 与 custom 字段同时缺失或同时提供、quantity ≤ 0）

### 4.3 编辑草稿

- **PUT** `/api/procurement-requests/{id}`
- **权限**：申请人本人
- **请求体**：`UpdateProcurementRequestRequest`
- **成功**：200 + `ProcurementRequestDto`
- **失败**：400（仅 `Draft` 可编辑）、403（非本人）、404

### 4.4 提交申请

- **POST** `/api/procurement-requests/{id}/submit`
- **权限**：申请人本人
- **成功**：200 + `ProcurementRequestDto`
- **失败**：400（仅 `Draft` 可提交）、403、404

### 4.5 取消申请

- **POST** `/api/procurement-requests/{id}/cancel`
- **权限**：申请人本人
- **成功**：200 + `ProcurementRequestDto`
- **失败**：400（`Draft` / `Pending` / `Approved` 可取消，其余状态不可）、403、404

### 4.6 查看自己的申请

- **GET** `/api/procurement-requests/mine`
- **权限**：已认证
- **查询参数**：

| 参数 | 必须 | 类型 | 说明 |
| --- | --- | --- | --- |
| status | 否 | string | 按状态筛选 |
| from | 否 | string | 起始时间 |
| to | 否 | string | 截止时间 |
| search | 否 | string | 在物品名称、用途中搜索 |

- **成功**：200 + `ProcurementRequestDto[]`

### 4.7 查看申请详情

- **GET** `/api/procurement-requests/{id}`
- **权限**：申请人本人或 `Admin`
- **成功**：200 + `ProcurementRequestDto`
- **失败**：403、404

### 4.8 管理员查看所有申请

- **GET** `/api/procurement-requests`
- **权限**：`Admin`
- **查询参数**：

| 参数 | 必须 | 类型 | 说明 |
| --- | --- | --- | --- |
| status | 否 | string | 按状态筛选 |
| sourceId | 否 | long | 按申请人筛选 |
| from | 否 | string | 起始时间 |
| to | 否 | string | 截止时间 |
| search | 否 | string | 在物品名称、用途、申请人姓名中搜索 |

- **成功**：200 + `ProcurementRequestDto[]`

### 4.9 审批申请

- **POST** `/api/procurement-requests/{id}/audit`
- **权限**：`Admin`
- **请求体**：`AuditProcurementRequestRequest`
- **成功**：200 + `ProcurementRequestDto`
- **失败**：400（仅 `Pending` 可审批、reject 未填 refusalReason）、404
- **业务规则**：通过后状态为 `Approved`；驳回后状态为 `Rejected`。

### 4.10 采购入库

- **POST** `/api/procurement-requests/{id}/purchase`
- **权限**：`Admin`
- **成功**：200 + `ProcurementRequestDto`
- **失败**：400（仅 `Approved` 可采购）、404
- **业务规则**：
  - 状态变为 `Purchased`，记录采购人与时间
  - 若为目录内物品：在同一事务中向 `StockTransaction` 写入 `ProcurementInbound` 流水并增加对应 `StockItem.Quantity`
  - 若为目录外物品：采购入库的同时由管理员在库存模块创建新物品（或手动调整库存），本接口可接受 `createItem` 可选参数用于一并建档入库，具体在实现阶段细化

## 5. 权限矩阵

| 操作 | 员工 | 管理员 |
| --- | --- | --- |
| 创建草稿 / 编辑 / 提交 / 取消自己的申请 | ✓ | ✓（自己的） |
| 查看自己的申请 | ✓ | ✓ |
| 查看所有人的申请 | ✗ | ✓ |
| 审批 | ✗ | ✓ |
| 采购入库 | ✗ | ✓ |

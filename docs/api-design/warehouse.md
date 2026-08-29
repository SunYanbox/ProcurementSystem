# 库存模块设计（warehouse）

> 约定见 `overview.md`。库存为全公司统一库存池，不分部门。库存数量只通过流水变更，不直接覆盖快照值。

## 1. 实体定义

### 1.1 ItemType（办公用品类型）

| 字段 | 类型 | 说明 | 可见性 |
| --- | --- | --- | --- |
| Id | long | 主键 | 对外可见 |
| Name | string | 类型名称，唯一 | 对外可见 |
| Description | string? | 类型描述 | 对外可见 |

### 1.2 Item（办公用品目录）

| 字段 | 类型 | 说明 | 可见性 |
| --- | --- | --- | --- |
| Id | long | 主键 | 对外可见 |
| Name | string | 物品名称 | 对外可见 |
| TypeId | long | 类型外键 | 对外可见 |
| Description | string? | 物品描述 | 对外可见 |
| Specification | string | 物品规格 | 对外可见 |
| Unit | string | 计量单位 | 对外可见 |
| Price | decimal | 单价 | 对外可见 |
| IsActive | bool | 是否启用（停用后不可新申请） | 对外可见 |
| CreatedAt | DateTime | 创建时间 | 对外可见 |
| UpdatedAt | DateTime | 更新时间 | 对外可见 |

### 1.3 StockItem（库存快照）

| 字段 | 类型 | 说明 | 可见性 |
| --- | --- | --- | --- |
| Id | long | 主键 | 对外可见 |
| ItemId | long | 物品外键，唯一 | 对外可见 |
| Quantity | int | 当前库存数量（非负） | 对外可见 |
| Version | byte[] | 并发控制 rowversion | 内部字段 |
| UpdatedAt | DateTime | 最后变更时间 | 对外可见 |

> 库存快照不直接由客户端修改，变更必须经过 `StockTransaction` 流水。

### 1.4 StockTransaction（库存流水）

| 字段 | 类型 | 说明 | 可见性 |
| --- | --- | --- | --- |
| Id | long | 主键 | 对外可见 |
| ItemId | long | 物品外键 | 对外可见 |
| QuantityChange | int | 变动数量（正为入库，负为出库） | 对外可见 |
| Type | TransactionType | 流水类型枚举 | 对外可见 |
| ReferenceType | string? | 关联业务类型（如 `ProcurementRequest`） | 对外可见 |
| ReferenceId | long? | 关联业务 id | 对外可见 |
| Note | string? | 备注 | 对外可见 |
| OperatorId | long | 操作人用户 id | 对外可见 |
| CreatedAt | DateTime | 发生时间 | 对外可见 |

### 1.5 TransactionType 枚举

| 值 | 说明 |
| --- | --- |
| `ManualInbound` | 手动入库 |
| `ManualOutbound` | 手动出库 |
| `ProcurementInbound` | 采购入库 |
| `Adjustment` | 盘点调整 |

## 2. DTO 定义

### 2.1 输入 DTO

#### CreateItemTypeRequest

| 字段 | 必须 | 类型 | 说明 |
| --- | --- | --- | --- |
| name | 是 | string | 类型名称 |
| description | 否 | string | 描述 |

#### UpdateItemTypeRequest

| 字段 | 必须 | 类型 | 说明 |
| --- | --- | --- | --- |
| name | 是 | string | 类型名称 |
| description | 否 | string | 描述 |

#### CreateItemRequest

| 字段 | 必须 | 类型 | 说明 |
| --- | --- | --- | --- |
| name | 是 | string | 物品名称 |
| typeId | 是 | long | 类型 id |
| description | 否 | string | 描述 |
| specification | 是 | string | 规格 |
| unit | 是 | string | 计量单位 |
| price | 是 | decimal | 单价 |

#### UpdateItemRequest

| 字段 | 必须 | 类型 | 说明 |
| --- | --- | --- | --- |
| name | 否 | string | 名称 |
| typeId | 否 | long | 类型 id |
| description | 否 | string | 描述 |
| specification | 否 | string | 规格 |
| unit | 否 | string | 计量单位 |
| price | 否 | decimal | 单价 |
| isActive | 否 | bool | 是否启用 |

#### CreateTransactionRequest（手动调整库存）

| 字段 | 必须 | 类型 | 说明 |
| --- | --- | --- | --- |
| type | 是 | string | `ManualInbound` / `ManualOutbound` / `Adjustment` |
| quantityChange | 是 | int | 变动数量，非零整数 |
| note | 否 | string | 备注 |

### 2.2 输出 DTO

#### ItemTypeDto

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| id | long | 类型 id |
| name | string | 类型名称 |
| description | string? | 描述 |

#### ItemDto

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| id | long | 物品 id |
| name | string | 名称 |
| typeId | long | 类型 id |
| typeName | string | 类型名称 |
| description | string? | 描述 |
| specification | string | 规格 |
| unit | string | 计量单位 |
| price | decimal | 单价 |
| stockQuantity | int? | 当前库存（仅库存查询接口返回） |
| isActive | bool | 是否启用 |

#### StockItemDto

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| id | long | 快照 id |
| itemId | long | 物品 id |
| itemName | string | 物品名称 |
| itemSpecification | string | 规格 |
| unit | string | 计量单位 |
| quantity | int | 当前库存 |
| updatedAt | string | 最后变更时间 |

#### StockTransactionDto

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| id | long | 流水 id |
| itemId | long | 物品 id |
| itemName | string | 物品名称 |
| quantityChange | int | 变动数量 |
| type | string | 流水类型 |
| referenceType | string? | 关联业务类型 |
| referenceId | long? | 关联业务 id |
| note | string? | 备注 |
| operatorName | string | 操作人姓名 |
| createdAt | string | 发生时间 |

## 3. 接口规格

### 3.1 获取物品类型列表

- **GET** `/api/warehouse/item-types`
- **权限**：已认证
- **成功**：200 + `ItemTypeDto[]`

### 3.2 创建物品类型

- **POST** `/api/warehouse/item-types`
- **权限**：`Admin`
- **请求体**：`CreateItemTypeRequest`
- **成功**：201 + `ItemTypeDto`
- **失败**：409（类型名已存在）

### 3.3 更新物品类型

- **PUT** `/api/warehouse/item-types/{id}`
- **权限**：`Admin`
- **请求体**：`UpdateItemTypeRequest`
- **成功**：200 + `ItemTypeDto`
- **失败**：404、409

### 3.4 获取物品目录

- **GET** `/api/warehouse/items`
- **权限**：已认证
- **查询参数**：

| 参数 | 必须 | 类型 | 说明 |
| --- | --- | --- | --- |
| search | 否 | string | 在名称、类型名、描述、规格中搜索 |
| typeId | 否 | long | 按类型筛选 |
| isActive | 否 | bool | 是否启用，默认 true |
| ordering | 否 | string | `name` / `price`，前缀 `-` 反序 |

- **成功**：200 + `ItemDto[]`（不含 `stockQuantity`）

### 3.5 创建物品

- **POST** `/api/warehouse/items`
- **权限**：`Admin`
- **请求体**：`CreateItemRequest`
- **成功**：201 + `ItemDto`

### 3.6 更新物品

- **PUT** `/api/warehouse/items/{id}`
- **权限**：`Admin`
- **请求体**：`UpdateItemRequest`
- **成功**：200 + `ItemDto`
- **失败**：404

### 3.7 查询库存列表

- **GET** `/api/warehouse/stocks`
- **权限**：已认证
- **查询参数**：

| 参数 | 必须 | 类型 | 说明 |
| --- | --- | --- | --- |
| search | 否 | string | 在物品名称、类型名、规格中搜索 |
| typeId | 否 | long | 按类型筛选 |
| lowStock | 否 | bool | `true` 时仅返回库存为 0 或低于阈值的物品 |

- **成功**：200 + `StockItemDto[]`

### 3.8 查询单个物品库存

- **GET** `/api/warehouse/stocks/{itemId}`
- **权限**：已认证
- **成功**：200 + `StockItemDto`
- **失败**：404

### 3.9 手动调整库存

- **POST** `/api/warehouse/stocks/{itemId}/transactions`
- **权限**：`Admin`
- **请求体**：`CreateTransactionRequest`
- **成功**：201 + `StockTransactionDto`
- **失败**：400（库存不足、quantityChange 为 0）、404
- **业务规则**：在同一事务中写入流水并更新快照；出库时不得使快照数量为负。

### 3.10 查询库存流水

- **GET** `/api/warehouse/transactions`
- **权限**：`Admin`
- **查询参数**：

| 参数 | 必须 | 类型 | 说明 |
| --- | --- | --- | --- |
| itemId | 否 | long | 按物品筛选 |
| type | 否 | string | 按流水类型筛选 |
| from | 否 | string | 起始时间（ISO 8601） |
| to | 否 | string | 截止时间 |

- **成功**：200 + `StockTransactionDto[]`

## 4. 权限矩阵

| 操作 | 员工 | 管理员 |
| --- | --- | --- |
| 查看类型 / 目录 / 库存 | ✓ | ✓ |
| 创建 / 更新物品类型 | ✗ | ✓ |
| 创建 / 更新物品 | ✗ | ✓ |
| 手动调整库存 | ✗ | ✓ |
| 查看库存流水 | ✗ | ✓ |

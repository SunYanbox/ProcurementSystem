export interface DepartmentDto {
  id: number
  name: string
}

export interface UserDto {
  id: number
  username: string | null
  workId: string
  name: string
  email: string | null
  phone: string | null
  departmentId: number
  departmentName: string
  role: string
  working: boolean
  avatarUrl: string | null
  createdAt: string
}

export interface CreateUserRequest {
  workId: string
  name: string
  email?: string | null
  phone?: string | null
  departmentId: number
  role?: string | null
}

export interface UpdateUserRequest {
  name?: string | null
  email?: string | null
  phone?: string | null
  departmentId?: number | null
  role?: string | null
  working?: boolean | null
}

export interface StockItemDto {
  id: number
  itemId: number
  itemName: string
  itemSpecification: string
  unit: string
  quantity: number
  updatedAt: string
}

export interface StockTransactionDto {
  id: number
  itemId: number
  itemName: string
  quantityChange: number
  type: string
  referenceType: string | null
  referenceId: number | null
  note: string | null
  operatorName: string
  createdAt: string
}

export interface CreateTransactionRequest {
  // 类型以字符串传输，使 HTTP 契约不暴露 C# 枚举，非法值由后端拒绝
  type: string
  quantityChange: number
  note?: string | null
}

export interface ItemDto {
  id: number
  name: string
  typeId: number
  typeName: string
  description: string | null
  specification: string
  unit: string
  price: number
  stockQuantity: number | null
  isActive: boolean
}

export interface ProcurementRequestDto {
  id: number
  sourceId: number
  sourceName: string
  itemId: number | null
  itemName: string | null
  customItemName: string | null
  customSpecification: string | null
  quantity: number
  purpose: string
  status: string
  auditedByName: string | null
  auditedAt: string | null
  refusalReason: string | null
  purchasedByName: string | null
  purchasedAt: string | null
  requestedAt: string
  cancelledAt: string | null
}

export interface CreateProcurementRequestRequest {
  // 目录物料与自定义物料互斥，由后端校验；null 表示使用自定义字段
  itemId?: number | null
  customItemName?: string | null
  customSpecification?: string | null
  quantity: number
  purpose: string
}

export interface UpdateProcurementRequestRequest {
  itemId?: number | null
  customItemName?: string | null
  customSpecification?: string | null
  quantity?: number | null
  purpose?: string | null
}

export interface AuditProcurementRequestRequest {
  // "approve" / "reject"，拒绝时必须有 refusalReason
  decision: string
  refusalReason?: string | null
}

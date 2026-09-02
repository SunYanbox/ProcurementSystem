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

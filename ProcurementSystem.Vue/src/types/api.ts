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

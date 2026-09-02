import http from './http'
import type {
  CreateUserRequest,
  UpdateUserRequest,
  UserDto,
} from '../types/api'

export async function getMe(): Promise<UserDto> {
  const { data } = await http.get<UserDto>('/users/me')
  return data
}

export async function listUsers(params?: {
  departmentId?: number
  role?: string
  working?: boolean
  search?: string
}): Promise<UserDto[]> {
  const { data } = await http.get<UserDto[]>('/users', { params })
  return data
}

export async function createUser(dto: CreateUserRequest): Promise<UserDto> {
  const { data } = await http.post<UserDto>('/users', dto)
  return data
}

export async function updateUser(
  id: number,
  dto: UpdateUserRequest,
): Promise<UserDto> {
  const { data } = await http.put<UserDto>(`/users/${id}`, dto)
  return data
}

// 头像上传是 multipart/form-data，需显式设置请求头让 axios 识别边界
// axios 会自动为 FormData 生成正确的 Content-Type 并附带 boundary

export async function uploadAvatar(file: File): Promise<UserDto> {
  const formData = new FormData()
  formData.append('file', file)
  const { data } = await http.post<UserDto>('/users/me/avatar', formData)
  return data
}

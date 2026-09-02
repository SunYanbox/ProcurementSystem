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

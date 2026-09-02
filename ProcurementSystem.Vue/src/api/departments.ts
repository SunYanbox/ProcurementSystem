import http from './http'
import type { CreateDepartmentRequest, DepartmentDto } from '../types/api'

export async function listDepartments(): Promise<DepartmentDto[]> {
  const { data } = await http.get<DepartmentDto[]>('/departments')
  return data
}

export async function createDepartment(
  dto: CreateDepartmentRequest,
): Promise<DepartmentDto> {
  const { data } = await http.post<DepartmentDto>('/departments', dto)
  return data
}

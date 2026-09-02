import http from './http'
import type { DepartmentDto } from '../types/api'

export async function listDepartments(): Promise<DepartmentDto[]> {
  const { data } = await http.get<DepartmentDto[]>('/departments')
  return data
}

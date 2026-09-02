import http from './http'
import type {
  AuditProcurementRequestRequest,
  CreateProcurementRequestRequest,
  ProcurementRequestDto,
  UpdateProcurementRequestRequest,
} from '../types/api'

export async function listStatuses(): Promise<string[]> {
  const { data } = await http.get<string[]>('/procurement-requests/statuses')
  return data
}

export async function listMine(params?: {
  status?: string
  from?: string
  to?: string
  search?: string
}): Promise<ProcurementRequestDto[]> {
  const { data } = await http.get<ProcurementRequestDto[]>('/procurement-requests/mine', { params })
  return data
}

export async function listAll(params?: {
  status?: string
  sourceId?: number
  from?: string
  to?: string
  search?: string
}): Promise<ProcurementRequestDto[]> {
  const { data } = await http.get<ProcurementRequestDto[]>('/procurement-requests', { params })
  return data
}

export async function createDraft(
  dto: CreateProcurementRequestRequest,
): Promise<ProcurementRequestDto> {
  const { data } = await http.post<ProcurementRequestDto>('/procurement-requests', dto)
  return data
}

export async function editDraft(
  id: number,
  dto: UpdateProcurementRequestRequest,
): Promise<ProcurementRequestDto> {
  const { data } = await http.put<ProcurementRequestDto>(`/procurement-requests/${id}`, dto)
  return data
}

export async function submit(id: number): Promise<ProcurementRequestDto> {
  const { data } = await http.post<ProcurementRequestDto>(`/procurement-requests/${id}/submit`)
  return data
}

export async function cancel(id: number): Promise<ProcurementRequestDto> {
  const { data } = await http.post<ProcurementRequestDto>(`/procurement-requests/${id}/cancel`)
  return data
}

export async function audit(
  id: number,
  dto: AuditProcurementRequestRequest,
): Promise<ProcurementRequestDto> {
  const { data } = await http.post<ProcurementRequestDto>(`/procurement-requests/${id}/audit`, dto)
  return data
}

export async function purchase(id: number): Promise<ProcurementRequestDto> {
  const { data } = await http.post<ProcurementRequestDto>(`/procurement-requests/${id}/purchase`)
  return data
}

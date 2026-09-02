import http from './http'
import type {
  CreateItemRequest,
  CreateItemTypeRequest,
  CreateTransactionRequest,
  ItemDto,
  ItemTypeDto,
  StockItemDto,
  StockTransactionDto,
} from '../types/api'

export async function listItemTypes(): Promise<ItemTypeDto[]> {
  const { data } = await http.get<ItemTypeDto[]>('/warehouse/item-types')
  return data
}

export async function createItemType(
  dto: CreateItemTypeRequest,
): Promise<ItemTypeDto> {
  const { data } = await http.post<ItemTypeDto>('/warehouse/item-types', dto)
  return data
}

export async function createItem(dto: CreateItemRequest): Promise<ItemDto> {
  const { data } = await http.post<ItemDto>('/warehouse/items', dto)
  return data
}

export async function listItems(params?: {
  search?: string
  typeId?: number
  isActive?: boolean
  ordering?: string
}): Promise<ItemDto[]> {
  const { data } = await http.get<ItemDto[]>('/warehouse/items', { params })
  return data
}

export async function listStocks(params?: {
  search?: string
  typeId?: number
  lowStock?: boolean
}): Promise<StockItemDto[]> {
  const { data } = await http.get<StockItemDto[]>('/warehouse/stocks', { params })
  return data
}

export async function createTransaction(
  itemId: number,
  dto: CreateTransactionRequest,
): Promise<StockTransactionDto> {
  const { data } = await http.post<StockTransactionDto>(
    `/warehouse/stocks/${itemId}/transactions`,
    dto,
  )
  return data
}

export async function listTransactions(params?: {
  itemId?: number
  type?: string
  from?: string
  to?: string
}): Promise<StockTransactionDto[]> {
  const { data } = await http.get<StockTransactionDto[]>('/warehouse/transactions', { params })
  return data
}

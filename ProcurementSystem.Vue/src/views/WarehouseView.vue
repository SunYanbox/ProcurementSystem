<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import {
  createItem,
  createItemType,
  createTransaction,
  listItemTypes,
  listStocks,
} from '../api/warehouse'
import type {
  CreateItemRequest,
  CreateItemTypeRequest,
  CreateTransactionRequest,
  ItemTypeDto,
  StockItemDto,
} from '../types/api'

const auth = useAuthStore()
const router = useRouter()

const stocks = ref<StockItemDto[]>([])
const itemTypes = ref<ItemTypeDto[]>([])
const loading = ref(false)
const search = ref('')
const lowStockOnly = ref(false)

// 全站内联提示：错误/警告/成功统一在页面内展示，避免全局弹窗出现在角落
const notice = reactive({ type: '', message: '' })
function setNotice(type: 'success' | 'warning' | 'error', message: string) {
  notice.type = type
  notice.message = message
}

const txVisible = ref(false)
const currentStock = ref<StockItemDto | null>(null)
const txForm = reactive<CreateTransactionRequest>({
  type: 'ManualInbound',
  quantityChange: 1,
  note: null,
})

// 物料类型创建弹窗状态
const typeVisible = ref(false)
const typeForm = reactive<CreateItemTypeRequest>({ name: '', description: null })

// 物料创建弹窗状态
const itemVisible = ref(false)
const itemForm = reactive<CreateItemRequest>({
  name: '',
  typeId: 0,
  description: null,
  specification: '',
  unit: '',
  price: 0,
})

async function load() {
  loading.value = true
  try {
    const [stockRows, typeRows] = await Promise.all([
      listStocks({
        search: search.value || undefined,
        lowStock: lowStockOnly.value || undefined,
      }),
      listItemTypes(),
    ])
    stocks.value = stockRows
    itemTypes.value = typeRows
  } finally {
    loading.value = false
  }
}

function openTransaction(stock: StockItemDto, type: 'ManualInbound' | 'ManualOutbound') {
  currentStock.value = stock
  txForm.type = type
  txForm.quantityChange = 1
  txForm.note = null
  notice.message = ''
  txVisible.value = true
}

async function submitTransaction() {
  if (!currentStock.value) return
  notice.message = ''
  // 出库数量必须是负数，入库必须是正数；与后端 WarehouseService 的校验保持一致
  const direction = txForm.type === 'ManualInbound' ? 1 : -1
  const quantity = Math.abs(txForm.quantityChange)
  try {
    await createTransaction(currentStock.value.itemId, {
      type: txForm.type,
      quantityChange: direction * quantity,
      note: txForm.note,
    })
    setNotice('success', '库存变动已记录')
    txVisible.value = false
    await load()
  } catch (e: any) {
    setNotice('error', e?.response?.data?.detail ?? '操作失败')
  }
}

function openTypeCreate() {
  typeForm.name = ''
  typeForm.description = null
  notice.message = ''
  typeVisible.value = true
}

async function submitType() {
  notice.message = ''
  if (!typeForm.name.trim()) {
    setNotice('warning', '类型名称不能为空')
    return
  }
  try {
    await createItemType({ name: typeForm.name.trim(), description: typeForm.description })
    setNotice('success', '物料类型已创建')
    typeVisible.value = false
    await load()
  } catch (e: any) {
    setNotice('error', e?.response?.data?.detail ?? '创建失败，可能是名称已存在')
  }
}

function openItemCreate() {
  Object.assign(itemForm, {
    name: '',
    typeId: itemTypes.value[0]?.id ?? 0,
    description: null,
    specification: '',
    unit: '',
    price: 0,
  })
  notice.message = ''
  itemVisible.value = true
}

async function submitItem() {
  notice.message = ''
  if (!itemForm.name.trim() || !itemForm.typeId) {
    setNotice('warning', '物料名称和类型必填')
    return
  }
  try {
    await createItem(itemForm)
    setNotice('success', '物料已创建')
    itemVisible.value = false
    await load()
  } catch (e: any) {
    setNotice('error', e?.response?.data?.detail ?? '创建失败')
  }
}

function goToTransactions() {
  router.push({ name: 'transactions' })
}

onMounted(load)
</script>

<template>
  <div class="page">
    <el-alert
      v-if="notice.message"
      :type="notice.type"
      :title="notice.message"
      show-icon
      :closable="false"
    />
    <el-card shadow="never" class="page-card">
      <template #header>
        <div class="page-header">
          <span class="page-title">仓库库存</span>
          <div class="page-tools">
            <el-switch
              v-model="lowStockOnly"
              active-text="仅看缺货"
              @change="load"
            />
            <el-input
              v-model="search"
              placeholder="搜索物料名称/规格"
              clearable
              style="width: 240px"
              @keyup.enter="load"
              @clear="load"
            >
              <template #append>
                <el-button @click="load">搜索</el-button>
              </template>
            </el-input>
            <el-button v-if="auth.isAdmin" @click="openTypeCreate">新建物料类型</el-button>
            <el-button v-if="auth.isAdmin" type="primary" @click="openItemCreate">新建物料</el-button>
            <el-button v-if="auth.isAdmin" @click="goToTransactions">变动记录</el-button>
          </div>
        </div>
      </template>

      <el-table :data="stocks" v-loading="loading" stripe>
        <el-table-column prop="itemName" label="物料名称" min-width="160" />
        <el-table-column prop="itemSpecification" label="规格" min-width="160" />
        <el-table-column prop="unit" label="单位" width="90" />
        <el-table-column label="库存数量" width="120" align="center">
          <template #default="{ row }">
            <el-tag :type="row.quantity === 0 ? 'danger' : 'success'">
              {{ row.quantity }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="updatedAt" label="更新时间" min-width="180">
          <template #default="{ row }">
            {{ new Date(row.updatedAt).toLocaleString() }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="260" align="center">
          <template #default="{ row }">
            <el-button
              v-if="auth.isAdmin"
              link
              type="success"
              @click="openTransaction(row, 'ManualInbound')"
            >
              入库
            </el-button>
            <el-button
              v-if="auth.isAdmin && row.quantity > 0"
              link
              type="warning"
              @click="openTransaction(row, 'ManualOutbound')"
            >
              出库
            </el-button>
            <el-button v-if="auth.isAdmin" link type="primary" @click="goToTransactions">明细</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog
      v-model="txVisible"
      :title="txForm.type === 'ManualInbound' ? '物料入库' : '物料出库'"
      width="420px"
    >
      <el-form label-width="90px">
        <el-form-item label="物料">
          <el-input :model-value="currentStock?.itemName" disabled />
        </el-form-item>
        <el-form-item label="当前库存">
          <el-input :model-value="currentStock?.quantity" disabled />
        </el-form-item>
        <el-form-item label="数量">
          <el-input-number v-model="txForm.quantityChange" :min="1" />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="txForm.note" placeholder="可选" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="txVisible = false">取消</el-button>
        <el-button type="primary" @click="submitTransaction">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="typeVisible" title="新建物料类型" width="420px">
      <el-form label-width="90px">
        <el-form-item label="类型名称">
          <el-input v-model="typeForm.name" placeholder="必填" />
        </el-form-item>
        <el-form-item label="描述">
          <el-input v-model="typeForm.description" placeholder="可选" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="typeVisible = false">取消</el-button>
        <el-button type="primary" @click="submitType">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="itemVisible" title="新建物料" width="480px">
      <el-form label-width="90px">
        <el-form-item label="物料名称">
          <el-input v-model="itemForm.name" placeholder="必填" />
        </el-form-item>
        <el-form-item label="物料类型">
          <el-select v-model="itemForm.typeId" style="width: 100%">
            <el-option
              v-for="type in itemTypes"
              :key="type.id"
              :label="type.name"
              :value="type.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="规格">
          <el-input v-model="itemForm.specification" placeholder="例如 500张/包" />
        </el-form-item>
        <el-form-item label="单位">
          <el-input v-model="itemForm.unit" placeholder="例如 包" />
        </el-form-item>
        <el-form-item label="单价">
          <el-input-number v-model="itemForm.price" :min="0" :precision="2" />
        </el-form-item>
        <el-form-item label="描述">
          <el-input v-model="itemForm.description" placeholder="可选" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="itemVisible = false">取消</el-button>
        <el-button type="primary" @click="submitItem">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.page {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.page-card {
  border-radius: 8px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.page-title {
  font-size: 16px;
  font-weight: 600;
}

.page-tools {
  display: flex;
  align-items: center;
  gap: 12px;
}
</style>

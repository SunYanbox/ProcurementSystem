<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { useAuthStore } from '../stores/auth'
import { createTransaction, listStocks, listTransactions } from '../api/warehouse'
import type { CreateTransactionRequest, StockItemDto, StockTransactionDto } from '../types/api'

const auth = useAuthStore()

const stocks = ref<StockItemDto[]>([])
const loading = ref(false)
const search = ref('')
const lowStockOnly = ref(false)

const txVisible = ref(false)
const txLoading = ref(false)
const currentStock = ref<StockItemDto | null>(null)
const txForm = reactive<CreateTransactionRequest>({
  type: 'ManualInbound',
  quantityChange: 1,
  note: null,
})

const ledgerVisible = ref(false)
const ledgerLoading = ref(false)
const transactions = ref<StockTransactionDto[]>([])
const ledgerItemId = ref<number | undefined>(undefined)

async function load() {
  loading.value = true
  try {
    stocks.value = await listStocks({
      search: search.value || undefined,
      lowStock: lowStockOnly.value || undefined,
    })
  } finally {
    loading.value = false
  }
}

function openTransaction(stock: StockItemDto, type: 'ManualInbound' | 'ManualOutbound') {
  currentStock.value = stock
  txForm.type = type
  txForm.quantityChange = 1
  txForm.note = null
  txVisible.value = true
}

async function submitTransaction() {
  if (!currentStock.value) return
  // 出库数量必须是负数，入库必须是正数；与后端 WarehouseService 的校验保持一致
  const direction = txForm.type === 'ManualInbound' ? 1 : -1
  const quantity = Math.abs(txForm.quantityChange)
  try {
    await createTransaction(currentStock.value.itemId, {
      type: txForm.type,
      quantityChange: direction * quantity,
      note: txForm.note,
    })
    ElMessage.success('库存变动已记录')
    txVisible.value = false
    await load()
  } catch (e: any) {
    ElMessage.error(e?.response?.data?.detail ?? '操作失败')
  }
}

async function openLedger(stock: StockItemDto) {
  ledgerItemId.value = stock.itemId
  ledgerVisible.value = true
  ledgerLoading.value = true
  try {
    transactions.value = await listTransactions({ itemId: stock.itemId })
  } finally {
    ledgerLoading.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="page">
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
            <el-button v-if="auth.isAdmin" link type="primary" @click="openLedger(row)">明细</el-button>
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

    <el-drawer v-model="ledgerVisible" title="库存变动明细" size="520px">
      <el-table :data="transactions" v-loading="ledgerLoading" stripe>
        <el-table-column label="变动" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="row.quantityChange > 0 ? 'success' : 'warning'">
              {{ row.quantityChange > 0 ? '+' : '' }}{{ row.quantityChange }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="type" label="类型" width="130" />
        <el-table-column prop="operatorName" label="操作人" width="110" />
        <el-table-column prop="note" label="备注" min-width="120" />
        <el-table-column label="时间" width="170">
          <template #default="{ row }">
            {{ new Date(row.createdAt).toLocaleString() }}
          </template>
        </el-table-column>
      </el-table>
    </el-drawer>
  </div>
</template>

<style scoped>
.page {
  display: flex;
  flex-direction: column;
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

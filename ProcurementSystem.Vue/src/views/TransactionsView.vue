<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { listTransactions } from '../api/warehouse'
import type { StockTransactionDto } from '../types/api'

const transactions = ref<StockTransactionDto[]>([])
const loading = ref(false)
const typeFilter = ref('')

const typeLabels: Record<string, string> = {
  ManualInbound: '手动入库',
  ManualOutbound: '手动出库',
  ProcurementInbound: '采购入库',
  Adjustment: '调整',
}

const typeOptions = [
  { value: '', label: '全部类型' },
  { value: 'ManualInbound', label: '手动入库' },
  { value: 'ManualOutbound', label: '手动出库' },
  { value: 'ProcurementInbound', label: '采购入库' },
  { value: 'Adjustment', label: '调整' },
]

async function load() {
  loading.value = true
  try {
    transactions.value = await listTransactions({
      type: typeFilter.value || undefined,
    })
  } finally {
    loading.value = false
  }
}

function formatTime(value: string) {
  return new Date(value).toLocaleString()
}

onMounted(load)
</script>

<template>
  <div class="page">
    <el-card shadow="never" class="page-card">
      <template #header>
        <div class="page-header">
          <span class="page-title">库存变动记录</span>
          <div class="page-tools">
            <el-select v-model="typeFilter" style="width: 140px" @change="load">
              <el-option
                v-for="t in typeOptions"
                :key="t.value"
                :label="t.label"
                :value="t.value"
              />
            </el-select>
          </div>
        </div>
      </template>

      <el-table :data="transactions" v-loading="loading" stripe>
        <el-table-column prop="itemName" label="物料名称" min-width="160" />
        <el-table-column label="变动" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="row.quantityChange > 0 ? 'success' : 'warning'">
              {{ row.quantityChange > 0 ? '+' : '' }}{{ row.quantityChange }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="类型" width="130">
          <template #default="{ row }">{{ typeLabels[row.type] ?? row.type }}</template>
        </el-table-column>
        <el-table-column prop="operatorName" label="操作人" width="120" />
        <el-table-column label="参考单据" min-width="140">
          <template #default="{ row }">
            {{ row.referenceType ? `${row.referenceType}#${row.referenceId}` : '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="note" label="备注" min-width="140" show-overflow-tooltip />
        <el-table-column label="时间" width="170">
          <template #default="{ row }">{{ formatTime(row.createdAt) }}</template>
        </el-table-column>
      </el-table>
    </el-card>
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

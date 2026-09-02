<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { useAuthStore } from '../stores/auth'
import {
  cancel,
  createDraft,
  editDraft,
  listAll,
  listMine,
  purchase,
  submit,
  audit,
} from '../api/procurements'
import { listItems } from '../api/warehouse'
import type {
  CreateProcurementRequestRequest,
  ItemDto,
  ProcurementRequestDto,
} from '../types/api'

const auth = useAuthStore()

const requests = ref<ProcurementRequestDto[]>([])
const items = ref<ItemDto[]>([])
const loading = ref(false)
const search = ref('')
const statusFilter = ref('')

const statuses = [
  { value: '', label: '全部状态' },
  { value: 'Draft', label: '草稿' },
  { value: 'Pending', label: '待审批' },
  { value: 'Approved', label: '已批准' },
  { value: 'Rejected', label: '已拒绝' },
  { value: 'Purchased', label: '已采购' },
  { value: 'Cancelled', label: '已取消' },
]

// 状态标签样式映射，Element Plus tag 类型
const statusTagType: Record<string, 'info' | 'warning' | 'success' | 'danger' | 'primary'> = {
  Draft: 'info',
  Pending: 'warning',
  Approved: 'success',
  Rejected: 'danger',
  Purchased: 'primary',
  Cancelled: 'info',
}

const statusLabel: Record<string, string> = {
  Draft: '草稿',
  Pending: '待审批',
  Approved: '已批准',
  Rejected: '已拒绝',
  Purchased: '已采购',
  Cancelled: '已取消',
}

const formVisible = ref(false)
const isEdit = ref(false)
const currentId = ref<number | null>(null)
const form = reactive<CreateProcurementRequestRequest>({
  itemId: null,
  customItemName: null,
  customSpecification: null,
  quantity: 1,
  purpose: '',
})

const auditVisible = ref(false)
const auditTarget = ref<ProcurementRequestDto | null>(null)
const auditDecision = ref('approve')
const refusalReason = ref('')

async function load() {
  loading.value = true
  try {
    const params = {
      search: search.value || undefined,
      status: statusFilter.value || undefined,
    }
    // 管理员查看全部申请，普通员工只看自己的
    requests.value = auth.isAdmin ? await listAll(params) : await listMine(params)
  } finally {
    loading.value = false
  }
}

async function loadItems() {
  try {
    items.value = await listItems({ isActive: true })
  } catch {
    // 目录物料加载失败时仍允许自定义物料申请
  }
}

function openCreate() {
  isEdit.value = false
  currentId.value = null
  Object.assign(form, {
    itemId: null,
    customItemName: null,
    customSpecification: null,
    quantity: 1,
    purpose: '',
  })
  formVisible.value = true
}

function openEdit(row: ProcurementRequestDto) {
  isEdit.value = true
  currentId.value = row.id
  Object.assign(form, {
    itemId: row.itemId,
    customItemName: row.customItemName,
    customSpecification: row.customSpecification,
    quantity: row.quantity,
    purpose: row.purpose,
  })
  formVisible.value = true
}

async function submitForm() {
  // 目录物料与自定义物料互斥：选择了目录物料则清空自定义字段
  const payload: CreateProcurementRequestRequest = {
    itemId: form.itemId,
    customItemName: form.itemId ? null : form.customItemName,
    customSpecification: form.itemId ? null : form.customSpecification,
    quantity: form.quantity,
    purpose: form.purpose,
  }
  try {
    if (isEdit.value && currentId.value !== null) {
      await editDraft(currentId.value, payload)
      ElMessage.success('申请已更新')
    } else {
      await createDraft(payload)
      ElMessage.success('草稿已创建')
    }
    formVisible.value = false
    await load()
  } catch (e: any) {
    ElMessage.error(e?.response?.data?.detail ?? '操作失败')
  }
}

async function doSubmit(row: ProcurementRequestDto) {
  try {
    await submit(row.id)
    ElMessage.success('已提交审批')
    await load()
  } catch (e: any) {
    ElMessage.error(e?.response?.data?.detail ?? '操作失败')
  }
}

async function doCancel(row: ProcurementRequestDto) {
  try {
    await cancel(row.id)
    ElMessage.success('已取消申请')
    await load()
  } catch (e: any) {
    ElMessage.error(e?.response?.data?.detail ?? '操作失败')
  }
}

async function doPurchase(row: ProcurementRequestDto) {
  try {
    await purchase(row.id)
    ElMessage.success('已入库')
    await load()
  } catch (e: any) {
    ElMessage.error(e?.response?.data?.detail ?? '操作失败')
  }
}

function openAudit(row: ProcurementRequestDto) {
  auditTarget.value = row
  auditDecision.value = 'approve'
  refusalReason.value = ''
  auditVisible.value = true
}

async function doAudit() {
  if (!auditTarget.value) return
  // 后端要求在拒绝时必须提供原因
  if (auditDecision.value === 'reject' && !refusalReason.value.trim()) {
    ElMessage.warning('拒绝时必须填写原因')
    return
  }
  try {
    await audit(auditTarget.value.id, {
      decision: auditDecision.value,
      refusalReason: auditDecision.value === 'reject' ? refusalReason.value : null,
    })
    ElMessage.success('审批完成')
    auditVisible.value = false
    await load()
  } catch (e: any) {
    ElMessage.error(e?.response?.data?.detail ?? '操作失败')
  }
}

function itemLabel(row: ProcurementRequestDto) {
  // 目录物料显示物料名，自定义物料显示自定义名称
  return row.itemName ?? row.customItemName ?? '未指定'
}

function formatTime(value: string | null) {
  return value ? new Date(value).toLocaleString() : '-'
}

onMounted(async () => {
  await loadItems()
  await load()
})
</script>

<template>
  <div class="page">
    <el-card shadow="never" class="page-card">
      <template #header>
        <div class="page-header">
          <span class="page-title">采购申请</span>
          <div class="page-tools">
            <el-select v-model="statusFilter" style="width: 140px" @change="load">
              <el-option
                v-for="s in statuses"
                :key="s.value"
                :label="s.label"
                :value="s.value"
              />
            </el-select>
            <el-input
              v-model="search"
              placeholder="搜索用途/物料/申请人"
              clearable
              style="width: 240px"
              @keyup.enter="load"
              @clear="load"
            >
              <template #append>
                <el-button @click="load">搜索</el-button>
              </template>
            </el-input>
            <el-button type="primary" @click="openCreate">新建申请</el-button>
          </div>
        </div>
      </template>

      <el-table :data="requests" v-loading="loading" stripe>
        <el-table-column label="物料" min-width="160">
          <template #default="{ row }">{{ itemLabel(row) }}</template>
        </el-table-column>
        <el-table-column prop="quantity" label="数量" width="90" align="center" />
        <el-table-column prop="purpose" label="用途" min-width="180" show-overflow-tooltip />
        <el-table-column prop="sourceName" label="申请人" width="120" />
        <el-table-column label="状态" width="110" align="center">
          <template #default="{ row }">
            <el-tag :type="statusTagType[row.status] ?? 'info'">
              {{ statusLabel[row.status] ?? row.status }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="申请时间" width="170">
          <template #default="{ row }">{{ formatTime(row.requestedAt) }}</template>
        </el-table-column>
        <el-table-column v-if="auth.isAdmin" label="审批人" width="110">
          <template #default="{ row }">{{ row.auditedByName ?? '-' }}</template>
        </el-table-column>
        <el-table-column label="操作" width="240" align="center" fixed="right">
          <template #default="{ row }">
            <!-- 员工：草稿可编辑/提交/取消 -->
            <template v-if="!auth.isAdmin && row.status === 'Draft'">
              <el-button link type="primary" @click="openEdit(row)">编辑</el-button>
              <el-button link type="success" @click="doSubmit(row)">提交</el-button>
              <el-button link type="warning" @click="doCancel(row)">取消</el-button>
            </template>
            <!-- 员工：待审批可取消 -->
            <el-button
              v-else-if="!auth.isAdmin && row.status === 'Pending'"
              link
              type="warning"
              @click="doCancel(row)"
            >
              取消
            </el-button>
            <!-- 管理员：待审批可审批 -->
            <el-button
              v-if="auth.isAdmin && row.status === 'Pending'"
              link
              type="primary"
              @click="openAudit(row)"
            >
              审批
            </el-button>
            <!-- 管理员：已批准可购买入库 -->
            <el-button
              v-if="auth.isAdmin && row.status === 'Approved'"
              link
              type="success"
              @click="doPurchase(row)"
            >
              采购入库
            </el-button>
            <el-tag v-if="row.status === 'Rejected'" type="danger" size="small">
              {{ row.refusalReason ?? '已拒绝' }}
            </el-tag>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog
      v-model="formVisible"
      :title="isEdit ? '编辑申请' : '新建申请'"
      width="520px"
    >
      <el-form label-width="90px">
        <el-form-item label="物料类型">
          <el-radio-group v-model="form.itemId">
            <el-radio :value="null">自定义</el-radio>
          </el-radio-group>
          <el-select
            v-model="form.itemId"
            placeholder="选择目录物料"
            clearable
            style="width: 100%; margin-top: 8px"
          >
            <el-option
              v-for="item in items"
              :key="item.id"
              :label="`${item.name}（${item.unit}）`"
              :value="item.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item v-if="!form.itemId" label="物料名称">
          <el-input v-model="form.customItemName" placeholder="自定义物料名称" />
        </el-form-item>
        <el-form-item v-if="!form.itemId" label="规格">
          <el-input v-model="form.customSpecification" placeholder="自定义规格" />
        </el-form-item>
        <el-form-item label="数量">
          <el-input-number v-model="form.quantity" :min="1" />
        </el-form-item>
        <el-form-item label="用途">
          <el-input v-model="form.purpose" type="textarea" :rows="3" placeholder="请说明采购用途" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="formVisible = false">取消</el-button>
        <el-button type="primary" @click="submitForm">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="auditVisible" title="审批申请" width="480px">
      <el-form label-width="90px">
        <el-form-item label="决策">
          <el-radio-group v-model="auditDecision">
            <el-radio value="approve">通过</el-radio>
            <el-radio value="reject">拒绝</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item v-if="auditDecision === 'reject'" label="拒绝原因">
          <el-input v-model="refusalReason" type="textarea" :rows="3" placeholder="必填" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="auditVisible = false">取消</el-button>
        <el-button type="primary" @click="doAudit">确认</el-button>
      </template>
    </el-dialog>
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

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { createUser, listUsers, updateUser } from '../api/users'
import { listDepartments } from '../api/departments'
import type { CreateUserRequest, DepartmentDto, UserDto } from '../types/api'

const users = ref<UserDto[]>([])
const departments = ref<DepartmentDto[]>([])
const loading = ref(false)
const dialogVisible = ref(false)
const isEdit = ref(false)
const currentId = ref<number | null>(null)

const search = ref('')
const departmentFilter = ref<number | undefined>(undefined)

const form = reactive<{
  workId: string
  name: string
  email: string | null
  phone: string | null
  departmentId: number
  role: string
  working: boolean
}>({
  workId: '',
  name: '',
  email: null,
  phone: null,
  departmentId: 0,
  role: 'Employee',
  working: true,
})

async function load() {
  loading.value = true
  try {
    const [userRows, deptRows] = await Promise.all([
      listUsers({
        search: search.value || undefined,
        departmentId: departmentFilter.value,
      }),
      listDepartments(),
    ])
    users.value = userRows
    departments.value = deptRows
  } finally {
    loading.value = false
  }
}

function openCreate() {
  isEdit.value = false
  currentId.value = null
  Object.assign(form, {
    workId: '',
    name: '',
    email: null,
    phone: null,
    departmentId: departments.value[0]?.id ?? 0,
    role: 'Employee',
    working: true,
  })
  dialogVisible.value = true
}

function openEdit(user: UserDto) {
  isEdit.value = true
  currentId.value = user.id
  Object.assign(form, {
    workId: user.workId,
    name: user.name,
    email: user.email,
    phone: user.phone,
    departmentId: user.departmentId,
    role: user.role,
    working: user.working,
  })
  dialogVisible.value = true
}

async function submit() {
  try {
    if (isEdit.value && currentId.value !== null) {
      await updateUser(currentId.value, {
        name: form.name,
        email: form.email,
        phone: form.phone,
        departmentId: form.departmentId,
        role: form.role,
        working: form.working,
      })
      ElMessage.success('用户已更新')
    } else {
      await createUser({
        workId: form.workId,
        name: form.name,
        email: form.email,
        phone: form.phone,
        departmentId: form.departmentId,
        role: form.role,
      })
      ElMessage.success('用户已创建')
    }
    dialogVisible.value = false
    await load()
  } catch (e: any) {
    ElMessage.error(e?.response?.data?.detail ?? '操作失败')
  }
}

onMounted(load)
</script>

<template>
  <div class="page">
    <el-card shadow="never" class="page-card">
      <template #header>
        <div class="page-header">
          <span class="page-title">用户管理</span>
          <div class="page-tools">
            <el-select
              v-model="departmentFilter"
              placeholder="按部门筛选"
              clearable
              style="width: 180px"
              @change="load"
            >
              <el-option
                v-for="dept in departments"
                :key="dept.id"
                :label="dept.name"
                :value="dept.id"
              />
            </el-select>
            <el-input
              v-model="search"
              placeholder="搜索工号/姓名/账号"
              clearable
              style="width: 220px"
              @keyup.enter="load"
              @clear="load"
            >
              <template #append>
                <el-button @click="load">搜索</el-button>
              </template>
            </el-input>
            <el-button type="primary" @click="openCreate">新建用户</el-button>
          </div>
        </div>
      </template>

      <el-table :data="users" v-loading="loading" stripe>
        <el-table-column prop="workId" label="工号" width="140" />
        <el-table-column prop="name" label="姓名" min-width="120" />
        <el-table-column label="账号" min-width="140">
          <template #default="{ row }">{{ row.username ?? '-' }}</template>
        </el-table-column>
        <el-table-column prop="departmentName" label="部门" min-width="140" />
        <el-table-column label="角色" width="110">
          <template #default="{ row }">
            <el-tag :type="row.role === 'Admin' ? 'danger' : 'info'">
              {{ row.role === 'Admin' ? '管理员' : '员工' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="在职" width="90" align="center">
          <template #default="{ row }">
            <el-tag :type="row.working ? 'success' : 'warning'">
              {{ row.working ? '在职' : '离职' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="100" align="center">
          <template #default="{ row }">
            <el-button link type="primary" @click="openEdit(row)">编辑</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑用户' : '新建用户'" width="480px">
      <el-form label-width="90px">
        <el-form-item label="工号">
          <el-input v-model="form.workId" :disabled="isEdit" />
        </el-form-item>
        <el-form-item label="姓名">
          <el-input v-model="form.name" />
        </el-form-item>
        <el-form-item label="邮箱">
          <el-input v-model="form.email" />
        </el-form-item>
        <el-form-item label="电话">
          <el-input v-model="form.phone" />
        </el-form-item>
        <el-form-item label="部门">
          <el-select v-model="form.departmentId" style="width: 100%">
            <el-option
              v-for="dept in departments"
              :key="dept.id"
              :label="dept.name"
              :value="dept.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="角色">
          <el-select v-model="form.role" style="width: 100%">
            <el-option label="员工" value="Employee" />
            <el-option label="管理员" value="Admin" />
          </el-select>
        </el-form-item>
        <el-form-item v-if="isEdit" label="在职">
          <el-switch v-model="form.working" active-text="在职" inactive-text="离职" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submit">保存</el-button>
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

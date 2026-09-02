<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { createDepartment, listDepartments } from '../api/departments'
import type { DepartmentDto } from '../types/api'

const departments = ref<DepartmentDto[]>([])
const loading = ref(false)
const dialogVisible = ref(false)
const form = reactive<{ name: string }>({ name: '' })

// 全站内联提示：错误/警告/成功统一在页面内展示，避免全局弹窗出现在角落
const notice = reactive({ type: '', message: '' })
function setNotice(type: 'success' | 'warning' | 'error', message: string) {
  notice.type = type
  notice.message = message
}

async function load() {
  loading.value = true
  try {
    departments.value = await listDepartments()
  } finally {
    loading.value = false
  }
}

function openCreate() {
  form.name = ''
  notice.message = ''
  dialogVisible.value = true
}

async function submit() {
  notice.message = ''
  if (!form.name.trim()) {
    setNotice('warning', '部门名称不能为空')
    return
  }
  try {
    await createDepartment({ name: form.name.trim() })
    setNotice('success', '部门已创建')
    dialogVisible.value = false
    await load()
  } catch (e: any) {
    setNotice('error', e?.response?.data?.detail ?? '创建失败，可能是部门名称已存在')
  }
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
          <span class="page-title">部门列表</span>
          <el-button type="primary" @click="openCreate">新建部门</el-button>
        </div>
      </template>

      <el-table :data="departments" v-loading="loading" stripe>
        <el-table-column prop="id" label="ID" width="100" />
        <el-table-column prop="name" label="部门名称" />
      </el-table>
    </el-card>

    <el-dialog v-model="dialogVisible" title="新建部门" width="420px">
      <el-form label-width="90px">
        <el-form-item label="部门名称">
          <el-input v-model="form.name" placeholder="请输入部门名称" @keyup.enter="submit" />
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
</style>

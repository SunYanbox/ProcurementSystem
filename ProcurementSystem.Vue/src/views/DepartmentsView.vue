<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { listDepartments } from '../api/departments'
import type { DepartmentDto } from '../types/api'

const departments = ref<DepartmentDto[]>([])
const loading = ref(false)

onMounted(async () => {
  loading.value = true
  try {
    departments.value = await listDepartments()
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div class="page">
    <el-card shadow="never" class="page-card">
      <template #header>
        <span class="page-title">部门列表</span>
      </template>

      <el-table :data="departments" v-loading="loading" stripe>
        <el-table-column prop="id" label="ID" width="100" />
        <el-table-column prop="name" label="部门名称" />
      </el-table>
    </el-card>
  </div>
</template>

<style scoped>
.page-card {
  border-radius: 8px;
}

.page-title {
  font-size: 16px;
  font-weight: 600;
}
</style>

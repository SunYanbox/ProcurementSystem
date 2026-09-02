<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { getMe } from '../api/users'
import type { UserDto } from '../types/api'

const me = ref<UserDto | null>(null)
const loading = ref(false)

onMounted(async () => {
  loading.value = true
  try {
    me.value = await getMe()
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div class="page">
    <el-card shadow="never" class="page-card">
      <template #header>
        <span class="page-title">个人中心</span>
      </template>

      <div v-loading="loading" class="profile-body">
        <div class="profile-avatar">
          <el-avatar :size="72" :src="me?.avatarUrl ?? undefined">
            {{ me?.name?.charAt(0) ?? 'U' }}
          </el-avatar>
          <div class="profile-name">{{ me?.name ?? '-' }}</div>
          <div class="profile-role">
            <el-tag :type="me?.role === 'Admin' ? 'danger' : 'info'">
              {{ me?.role === 'Admin' ? '管理员' : '员工' }}
            </el-tag>
          </div>
        </div>

        <el-descriptions v-if="me" :column="1" border class="profile-desc">
          <el-descriptions-item label="工号">{{ me.workId }}</el-descriptions-item>
          <el-descriptions-item label="姓名">{{ me.name }}</el-descriptions-item>
          <el-descriptions-item label="部门">{{ me.departmentName }}</el-descriptions-item>
          <el-descriptions-item label="邮箱">{{ me.email ?? '-' }}</el-descriptions-item>
          <el-descriptions-item label="电话">{{ me.phone ?? '-' }}</el-descriptions-item>
        </el-descriptions>
      </div>
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

.profile-body {
  display: flex;
  gap: 32px;
  align-items: flex-start;
}

.profile-avatar {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 10px;
  min-width: 120px;
}

.profile-name {
  font-size: 16px;
  font-weight: 600;
  color: #1f2937;
}

.profile-desc {
  flex: 1;
}
</style>

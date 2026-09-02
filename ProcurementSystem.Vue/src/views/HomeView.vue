<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useAuthStore } from '../stores/auth'
import { getMe } from '../api/users'
import type { UserDto } from '../types/api'
import { User, OfficeBuilding, Postcard } from '@element-plus/icons-vue'

const auth = useAuthStore()

const me = ref<UserDto | null>(null)

onMounted(async () => {
  try {
    me.value = await getMe()
  } catch {
    // 忽略个人信息加载失败，首页仍可正常展示
  }
})
</script>

<template>
  <div class="home">
    <el-card shadow="never" class="welcome-card">
      <template #header>
        <span class="card-title">欢迎回来</span>
      </template>
      <div class="welcome-body">
        <el-avatar :size="64" :src="me?.avatarUrl ?? undefined">
          {{ me?.name?.charAt(0) ?? 'U' }}
        </el-avatar>
        <div class="welcome-text">
          <h2>{{ me?.name ?? '用户' }}</h2>
          <p>{{ me?.departmentName ?? '未分配部门' }} · {{ me?.role === 'Admin' ? '管理员' : '员工' }}</p>
        </div>
      </div>
    </el-card>

    <div class="quick-links">
      <el-card v-if="auth.isAdmin" shadow="hover" class="quick-card" @click="$router.push('/users')">
        <el-icon class="quick-icon" color="#409eff"><User /></el-icon>
        <div class="quick-label">用户管理</div>
        <div class="quick-desc">维护系统用户信息</div>
      </el-card>

      <el-card v-if="auth.isAdmin" shadow="hover" class="quick-card" @click="$router.push('/departments')">
        <el-icon class="quick-icon" color="#67c23a"><OfficeBuilding /></el-icon>
        <div class="quick-label">部门列表</div>
        <div class="quick-desc">查看部门组织架构</div>
      </el-card>

      <el-card shadow="hover" class="quick-card" @click="$router.push('/profile')">
        <el-icon class="quick-icon" color="#e6a23c"><Postcard /></el-icon>
        <div class="quick-label">个人中心</div>
        <div class="quick-desc">查看个人资料</div>
      </el-card>
    </div>
  </div>
</template>

<style scoped>
.home {
  display: grid;
  gap: 20px;
}

.card-title {
  font-size: 15px;
  font-weight: 600;
}

.welcome-card {
  border-radius: 8px;
}

.welcome-body {
  display: flex;
  align-items: center;
  gap: 20px;
}

.welcome-text h2 {
  margin: 0 0 6px;
  font-size: 20px;
  color: #1f2937;
}

.welcome-text p {
  margin: 0;
  color: #6b7280;
  font-size: 14px;
}

.quick-links {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
  gap: 20px;
}

.quick-card {
  border-radius: 8px;
  cursor: pointer;
  transition: transform 0.15s ease;
}

.quick-card:hover {
  transform: translateY(-3px);
}

.quick-icon {
  font-size: 32px;
  margin-bottom: 12px;
}

.quick-label {
  font-size: 15px;
  font-weight: 600;
  color: #1f2937;
  margin-bottom: 4px;
}

.quick-desc {
  font-size: 13px;
  color: #9ca3af;
}
</style>

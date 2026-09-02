<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { getMe } from '../api/users'
import type { UserDto } from '../types/api'
import {
  HomeFilled,
  User,
  OfficeBuilding,
  Goods,
  DocumentAdd,
  Postcard,
  ArrowDown,
} from '@element-plus/icons-vue'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()
const me = ref<UserDto | null>(null)

const activeMenu = computed(() => route.path)

onMounted(async () => {
  try {
    me.value = await getMe()
  } catch {
    // 当前用户信息加载失败时忽略，顶栏仅显示登录状态
  }
})

function handleLogout() {
  auth.logout()
  router.push({ name: 'login' })
}
</script>

<template>
  <el-container class="app-shell">
    <el-aside width="220px" class="app-aside">
      <div class="brand">
        <span class="brand-mark">P</span>
        <span class="brand-name">采购系统</span>
      </div>
      <el-menu :default-active="activeMenu" router class="app-menu">
        <el-menu-item index="/">
          <el-icon><HomeFilled /></el-icon>
          <span>首页</span>
        </el-menu-item>
        <el-menu-item v-if="auth.isAdmin" index="/users">
          <el-icon><User /></el-icon>
          <span>用户管理</span>
        </el-menu-item>
        <el-menu-item v-if="auth.isAdmin" index="/departments">
          <el-icon><OfficeBuilding /></el-icon>
          <span>部门列表</span>
        </el-menu-item>
        <el-menu-item index="/warehouse">
          <el-icon><Goods /></el-icon>
          <span>仓库库存</span>
        </el-menu-item>
        <el-menu-item index="/procurements">
          <el-icon><DocumentAdd /></el-icon>
          <span>采购申请</span>
        </el-menu-item>
        <el-menu-item index="/profile">
          <el-icon><Postcard /></el-icon>
          <span>个人中心</span>
        </el-menu-item>
      </el-menu>
    </el-aside>

    <el-container>
      <el-header class="app-header">
        <div class="header-title">
          {{ route.meta.title ?? '采购管理系统' }}
        </div>
        <el-dropdown trigger="click" @command="handleLogout">
          <span class="user-badge">
            <el-avatar :size="28" :src="me?.avatarUrl ?? undefined">
              {{ me?.name?.charAt(0) ?? 'U' }}
            </el-avatar>
            <span class="user-name">{{ me?.name ?? '用户' }}</span>
            <el-icon><ArrowDown /></el-icon>
          </span>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item command="logout">退出登录</el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </el-header>

      <el-main class="app-main">
        <router-view />
      </el-main>
    </el-container>
  </el-container>
</template>

<style scoped>
.app-shell {
  min-height: 100vh;
}

.app-aside {
  background: #001529;
  color: #fff;
}

.brand {
  display: flex;
  align-items: center;
  gap: 10px;
  height: 60px;
  padding: 0 20px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
}

.brand-mark {
  display: grid;
  place-items: center;
  width: 28px;
  height: 28px;
  border-radius: 6px;
  background: #409eff;
  color: #fff;
  font-weight: 700;
}

.brand-name {
  font-size: 16px;
  font-weight: 600;
  letter-spacing: 1px;
}

.app-menu {
  border-right: none;
  background: transparent;
}

.app-menu :deep(.el-menu-item) {
  color: rgba(255, 255, 255, 0.75);
}

.app-menu :deep(.el-menu-item:hover) {
  background: rgba(255, 255, 255, 0.08);
  color: #fff;
}

.app-menu :deep(.el-menu-item.is-active) {
  background: #409eff;
  color: #fff;
}

.app-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  background: #fff;
  border-bottom: 1px solid #eef0f4;
  padding: 0 24px;
}

.header-title {
  font-size: 16px;
  font-weight: 600;
  color: #1f2937;
}

.user-badge {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  outline: none;
}

.user-name {
  font-size: 14px;
  color: #374151;
}

.app-main {
  background: #f5f7fa;
  padding: 24px;
}
</style>

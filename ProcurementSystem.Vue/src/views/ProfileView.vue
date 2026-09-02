<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useAuthStore } from '../stores/auth'
import { uploadAvatar } from '../api/users'
import type { UserDto } from '../types/api'

const auth = useAuthStore()
// 与顶栏共享 store 中的用户信息
const me = computed(() => auth.me)
const loading = ref(false)
const uploading = ref(false)

// 全站内联提示：错误/警告/成功统一在页面内展示，避免全局弹窗出现在角落
const notice = reactive({ type: '', message: '' })
function setNotice(type: 'success' | 'warning' | 'error', message: string) {
  notice.type = type
  notice.message = message
}

async function load() {
  loading.value = true
  try {
    await auth.loadMe()
  } finally {
    loading.value = false
  }
}

async function handleAvatarChange(e: Event) {
  const input = e.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file) return
  // 仅接受 jpg/png，与后端 UploadAvatarAsync 支持的 Content-Type 保持一致
  if (!['image/jpeg', 'image/png'].includes(file.type)) {
    setNotice('warning', '仅支持 JPG/PNG 图片')
    input.value = ''
    return
  }
  uploading.value = true
  notice.message = ''
  try {
    const updated = await uploadAvatar(file)
    // 更新共享 store，顶栏头像即时同步
    auth.setMe(updated)
    setNotice('success', '头像已更新')
  } catch (e: any) {
    setNotice('error', e?.response?.data?.detail ?? '上传失败')
  } finally {
    uploading.value = false
    input.value = ''
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
          <label class="avatar-upload" :class="{ disabled: uploading }">
            {{ uploading ? '上传中...' : '更换头像' }}
            <input type="file" accept="image/jpeg,image/png" hidden @change="handleAvatarChange" />
          </label>
        </div>

        <el-descriptions v-if="me" :column="1" border class="profile-desc">
          <el-descriptions-item label="工号">{{ me.workId }}</el-descriptions-item>
          <el-descriptions-item label="姓名">{{ me.name }}</el-descriptions-item>
          <el-descriptions-item label="账号">{{ me.username ?? '-' }}</el-descriptions-item>
          <el-descriptions-item label="部门">{{ me.departmentName }}</el-descriptions-item>
          <el-descriptions-item label="邮箱">{{ me.email ?? '-' }}</el-descriptions-item>
          <el-descriptions-item label="电话">{{ me.phone ?? '-' }}</el-descriptions-item>
        </el-descriptions>
      </div>
    </el-card>
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

.avatar-upload {
  display: inline-block;
  padding: 6px 16px;
  font-size: 13px;
  color: #409eff;
  border: 1px solid #409eff;
  border-radius: 4px;
  cursor: pointer;
  transition: opacity 0.15s ease;
}

.avatar-upload:hover {
  opacity: 0.8;
}

.avatar-upload.disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.profile-desc {
  flex: 1;
}
</style>

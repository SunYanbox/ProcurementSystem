<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { User as UserIcon, Lock as LockIcon } from '@element-plus/icons-vue'

const auth = useAuthStore()
const router = useRouter()

const formRef = ref<FormInstance>()
const loading = ref(false)
const form = reactive({
  username: '',
  password: '',
})

const rules: FormRules = {
  username: [{ required: true, message: '请输入用户名', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }],
}

async function submit() {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    loading.value = true
    try {
      await auth.login(form.username, form.password)
      const redirect = router.currentRoute.value.query.redirect as string | undefined
      await router.push(redirect ?? '/')
    } catch (e: any) {
      // 后端错误信息通过 ElMessage 展示
      ElMessage.error(e?.response?.data?.detail ?? '登录失败，请检查用户名和密码。')
    } finally {
      loading.value = false
    }
  })
}
</script>

<template>
  <div class="login-page">
    <el-card class="login-card" shadow="always">
      <div class="login-brand">
        <div class="brand-mark">P</div>
        <h1>采购系统</h1>
        <p>请登录以继续</p>
      </div>

      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        label-position="top"
        @keyup.enter="submit"
      >
        <el-form-item label="用户名" prop="username">
          <el-input v-model="form.username" placeholder="请输入用户名" size="large" :prefix-icon="UserIcon" />
        </el-form-item>
        <el-form-item label="密码" prop="password">
          <el-input
            v-model="form.password"
            type="password"
            placeholder="请输入密码"
            size="large"
            show-password
            :prefix-icon="LockIcon"
          />
        </el-form-item>
        <el-button type="primary" size="large" class="login-btn" :loading="loading" @click="submit">
          登录
        </el-button>
      </el-form>
    </el-card>
  </div>
</template>

<style scoped>
.login-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #1e3a8a 0%, #3b82f6 50%, #93c5fd 100%);
  padding: 16px;
}

.login-card {
  width: 380px;
  border-radius: 12px;
  padding: 8px;
}

.login-brand {
  text-align: center;
  margin-bottom: 24px;
}

.brand-mark {
  display: inline-grid;
  place-items: center;
  width: 48px;
  height: 48px;
  border-radius: 12px;
  background: #409eff;
  color: #fff;
  font-size: 24px;
  font-weight: 700;
  margin-bottom: 12px;
}

.login-brand h1 {
  margin: 0 0 4px;
  font-size: 22px;
  color: #1f2937;
}

.login-brand p {
  margin: 0;
  color: #9ca3af;
  font-size: 14px;
}

.login-btn {
  width: 100%;
  margin-top: 8px;
}
</style>

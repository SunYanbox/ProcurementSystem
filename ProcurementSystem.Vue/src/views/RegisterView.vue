<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import type { FormInstance, FormRules } from 'element-plus'
import { User as UserIcon, Lock as LockIcon, Postcard as PostcardIcon } from '@element-plus/icons-vue'

const auth = useAuthStore()
const router = useRouter()

const formRef = ref<FormInstance>()
const loading = ref(false)
// 注册结果内联展示，避免全局弹窗；成功后停留展示用户名，让用户确认记住
const registerError = ref('')
const registeredUsername = ref('')
const form = reactive({
  workId: '',
  username: '',
  password: '',
  passwordAgain: '',
})

const rules: FormRules = {
  workId: [{ required: true, message: '请输入工号', trigger: 'blur' }],
  username: [
    { required: true, message: '请输入用户名', trigger: 'blur' },
    { min: 3, message: '用户名至少 3 个字符', trigger: 'blur' },
  ],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 6, message: '密码至少 6 个字符', trigger: 'blur' },
  ],
  passwordAgain: [
    { required: true, message: '请再次输入密码', trigger: 'blur' },
    {
      validator: (_rule, value: string, callback) => {
        if (value !== form.password) {
          callback(new Error('两次输入的密码不一致'))
        } else {
          callback()
        }
      },
      trigger: 'blur',
    },
  ],
}

async function submit() {
  if (!formRef.value) return
  // 校验失败时 validate 返回的 Promise 会 reject，统一转为 false
  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) return
  loading.value = true
  registerError.value = ''
  registeredUsername.value = ''
  try {
    await auth.register({
      workId: form.workId,
      username: form.username,
      password: form.password,
      passwordAgain: form.passwordAgain,
    })
    // 成功后不自动跳转，停留展示用户名，让用户确认记住账号
    registeredUsername.value = form.username
  } catch (e: any) {
    // 后端错误信息通过内联提示展示
    registerError.value = e?.response?.data?.detail ?? '注册失败'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="register-page">
    <el-card class="register-card" shadow="always">
      <div class="register-brand">
        <div class="brand-mark">P</div>
        <h1>注册账号</h1>
        <p>凭工号绑定登录账号</p>
      </div>

      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        label-position="top"
        @keyup.enter="submit"
      >
        <el-form-item label="工号" prop="workId">
          <el-input v-model="form.workId" placeholder="请输入工号" size="large" :prefix-icon="PostcardIcon" />
        </el-form-item>
        <el-form-item label="用户名" prop="username">
          <el-input v-model="form.username" placeholder="请输入用户名" size="large" :prefix-icon="UserIcon" />
        </el-form-item>
        <el-form-item label="密码" prop="password">
          <el-input
            v-model="form.password"
            type="password"
            placeholder="至少 6 位"
            size="large"
            show-password
            :prefix-icon="LockIcon"
          />
        </el-form-item>
        <el-form-item label="确认密码" prop="passwordAgain">
          <el-input
            v-model="form.passwordAgain"
            type="password"
            placeholder="请再次输入密码"
            size="large"
            show-password
            :prefix-icon="LockIcon"
          />
        </el-form-item>
        <p v-if="registerError" class="register-error">{{ registerError }}</p>
        <el-alert
          v-if="registeredUsername"
          type="success"
          :title="`注册成功，请使用账号 ${registeredUsername} 登录`"
          show-icon
          :closable="false"
        />
        <el-button
          v-if="!registeredUsername"
          type="primary"
          size="large"
          class="register-btn"
          :loading="loading"
          @click="submit"
        >
          注册
        </el-button>
        <el-button v-else type="primary" size="large" class="register-btn" @click="router.push({ name: 'login' })">
          去登录
        </el-button>
        <div v-if="!registeredUsername" class="register-login">
          <span>已有账号？</span>
          <el-link type="primary" @click="router.push({ name: 'login' })">去登录</el-link>
        </div>
      </el-form>
    </el-card>
  </div>
</template>

<style scoped>
.register-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #1e3a8a 0%, #3b82f6 50%, #93c5fd 100%);
  padding: 16px;
}

.register-card {
  width: 400px;
  border-radius: 12px;
  padding: 8px;
}

.register-brand {
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

.register-brand h1 {
  margin: 0 0 4px;
  font-size: 22px;
  color: #1f2937;
}

.register-brand p {
  margin: 0;
  color: #9ca3af;
  font-size: 14px;
}

.register-btn {
  width: 100%;
  margin-top: 8px;
}

.register-error {
  margin: -4px 0 8px;
  color: #f56c6c;
  font-size: 13px;
  text-align: left;
}

.register-login {
  margin-top: 12px;
  text-align: center;
  font-size: 14px;
  color: #9ca3af;
}
</style>

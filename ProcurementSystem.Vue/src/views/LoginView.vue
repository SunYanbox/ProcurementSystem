<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const router = useRouter()

const username = ref('')
const password = ref('')
const error = ref('')

async function submit() {
  error.value = ''
  try {
    await auth.login(username.value, password.value)
    const redirect = router.currentRoute.value.query.redirect as string | undefined
    await router.push(redirect ?? '/')
  } catch (e: any) {
    error.value = e?.response?.data?.detail ?? '登录失败，请检查用户名和密码。'
  }
}
</script>

<template>
  <main style="display: grid; place-items: center; min-height: 100vh">
    <form @submit.prevent="submit" style="display: grid; gap: 12px; min-width: 280px">
      <h1 style="margin: 0; font-size: 24px">登录</h1>
      <input v-model="username" placeholder="用户名" required />
      <input v-model="password" type="password" placeholder="密码" required />
      <p v-if="error" style="color: #dc2626">{{ error }}</p>
      <button type="submit">登录</button>
    </form>
  </main>
</template>

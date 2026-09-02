<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const router = useRouter()

function logout() {
  auth.logout()
  router.push({ name: 'login' })
}
</script>

<template>
  <main style="padding: 24px">
    <h1>已登录</h1>
    <p>欢迎回来。</p>
    <button @click="logout">退出登录</button>
  </main>
</template>

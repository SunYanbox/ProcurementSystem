import axios from 'axios'
import type { AxiosError, InternalAxiosRequestConfig } from 'axios'

const http = axios.create({
  baseURL: '/api',
  timeout: 10000,
})

// 扩展请求配置以标记重试后的请求，避免无限重试死循环
interface RetryableConfig extends InternalAxiosRequestConfig {
  _retry?: boolean
}

// 缓存刷新 promise，多个并发 401 时只触发一次 /auth/refresh
let refreshPromise: Promise<string> | null = null

async function refreshAccessToken(): Promise<string> {
  const refreshToken = localStorage.getItem('refresh_token')
  if (!refreshToken) {
    throw new Error('no refresh token')
  }
  // 使用裸 axios 而非 http 实例，避免拦截器递归调用
  const { data } = await axios.post('/api/auth/refresh', { refreshToken })
  localStorage.setItem('access_token', data.accessToken)
  localStorage.setItem('refresh_token', data.refreshToken)
  if (data.user?.role) {
    localStorage.setItem('user_role', data.user.role)
  }
  return data.accessToken
}

function redirectToLogin() {
  localStorage.removeItem('access_token')
  localStorage.removeItem('refresh_token')
  localStorage.removeItem('user_role')
  window.location.href = '/login'
}

http.interceptors.request.use((config) => {
  const token = localStorage.getItem('access_token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

http.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const original = error.config as RetryableConfig | undefined
    // 登录/刷新端点自身 401 时不再重试，交由上层处理
    const isAuthEndpoint =
      original?.url?.includes('/auth/login') ||
      original?.url?.includes('/auth/refresh')

    if (error.response?.status === 401 && original && !original._retry && !isAuthEndpoint) {
      original._retry = true
      try {
        if (!refreshPromise) {
          refreshPromise = refreshAccessToken().finally(() => {
            refreshPromise = null
          })
        }
        const newToken = await refreshPromise
        original.headers.Authorization = `Bearer ${newToken}`
        return http(original)
      } catch {
        redirectToLogin()
        return Promise.reject(error)
      }
    }
    return Promise.reject(error)
  },
)

export default http

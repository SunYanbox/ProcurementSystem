import { defineStore } from 'pinia'
import http from '../api/http'
import { getMe } from '../api/users'

export interface AuthResponse {
  accessToken: string
  refreshToken: string
}

export const useAuthStore = defineStore('auth', {
  state: () => ({
    accessToken: localStorage.getItem('access_token') ?? '',
    refreshToken: localStorage.getItem('refresh_token') ?? '',
    role: localStorage.getItem('user_role') ?? '',
  }),
  getters: {
    isAuthenticated: (state) => Boolean(state.accessToken),
    isAdmin: (state) => state.role === 'Admin',
  },
  actions: {
    async login(username: string, password: string) {
      const { data } = await http.post<AuthResponse>('/auth/login', {
        username,
        password,
      })
      this.accessToken = data.accessToken
      this.refreshToken = data.refreshToken
      localStorage.setItem('access_token', data.accessToken)
      localStorage.setItem('refresh_token', data.refreshToken)

      // 登录后立即获取用户角色，用于前端菜单与路由权限控制
      const me = await getMe()
      this.role = me.role
      localStorage.setItem('user_role', me.role)
    },
    logout() {
      this.accessToken = ''
      this.refreshToken = ''
      this.role = ''
      localStorage.removeItem('access_token')
      localStorage.removeItem('refresh_token')
      localStorage.removeItem('user_role')
    },
  },
})

import { defineStore } from 'pinia'
import http from '../api/http'
import { getMe } from '../api/users'
import type { RegisterRequest, UserDto } from '../types/api'

export interface AuthResponse {
  accessToken: string
  refreshToken: string
}

export const useAuthStore = defineStore('auth', {
  state: () => ({
    accessToken: localStorage.getItem('access_token') ?? '',
    refreshToken: localStorage.getItem('refresh_token') ?? '',
    role: localStorage.getItem('user_role') ?? '',
    // 共享当前用户信息，顶栏与个人中心都从此处读取，头像上传后即时同步
    me: null as UserDto | null,
  }),
  getters: {
    isAuthenticated: (state) => Boolean(state.accessToken),
    isAdmin: (state) => state.role === 'Admin',
  },
  actions: {
    // 注册成功后不签发令牌，员工仍需用新账号登录
    async register(dto: RegisterRequest): Promise<UserDto> {
      const { data } = await http.post<UserDto>('/auth/register', dto)
      return data
    },
    async loadMe() {
      const me = await getMe()
      this.me = me
      this.role = me.role
      localStorage.setItem('user_role', me.role)
    },
    // 头像上传等操作后同步当前用户信息，无需整页刷新
    setMe(user: UserDto) {
      this.me = user
    },
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
      this.me = me
      this.role = me.role
      localStorage.setItem('user_role', me.role)
    },
    async logout() {
      const refreshToken = localStorage.getItem('refresh_token')
      if (refreshToken) {
        // 主动撤销服务端 refresh token；请求失败也继续清理本地状态
        try {
          await http.post('/auth/logout', { refreshToken })
        } catch {
          // 忽略登出请求失败，避免用户无法退出
        }
      }
      this.accessToken = ''
      this.refreshToken = ''
      this.role = ''
      this.me = null
      localStorage.removeItem('access_token')
      localStorage.removeItem('refresh_token')
      localStorage.removeItem('user_role')
    },
  },
})

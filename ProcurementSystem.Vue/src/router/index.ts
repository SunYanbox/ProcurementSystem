import { createRouter, createWebHistory } from 'vue-router'
import LoginView from '../views/LoginView.vue'
import MainLayout from '../layouts/MainLayout.vue'
import HomeView from '../views/HomeView.vue'
import UserManagementView from '../views/UserManagementView.vue'
import DepartmentsView from '../views/DepartmentsView.vue'
import WarehouseView from '../views/WarehouseView.vue'
import ProfileView from '../views/ProfileView.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: LoginView,
      meta: { public: true },
    },
    {
      path: '/',
      component: MainLayout,
      children: [
        {
          path: '',
          name: 'home',
          component: HomeView,
          meta: { title: '首页' },
        },
        {
          path: 'users',
          name: 'users',
          component: UserManagementView,
          meta: { title: '用户管理', requiresAdmin: true },
        },
        {
          path: 'departments',
          name: 'departments',
          component: DepartmentsView,
          meta: { title: '部门列表', requiresAdmin: true },
        },
        {
          path: 'warehouse',
          name: 'warehouse',
          component: WarehouseView,
          meta: { title: '仓库库存' },
        },
        {
          path: 'profile',
          name: 'profile',
          component: ProfileView,
          meta: { title: '个人中心' },
        },
      ],
    },
  ],
})

router.beforeEach((to) => {
  const hasToken = Boolean(localStorage.getItem('access_token'))
  if (!to.meta.public && !hasToken) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }

  // 管理页面仅对管理员开放，普通员工访问时重定向回首页
  if (to.meta.requiresAdmin && localStorage.getItem('user_role') !== 'Admin') {
    return { name: 'home' }
  }

  return true
})

export default router

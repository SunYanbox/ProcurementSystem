import vue from '@vitejs/plugin-vue'
import { defineConfig } from 'vite'
import AutoImport from 'unplugin-auto-import/vite'
import Components from 'unplugin-vue-components/vite'
import { ElementPlusResolver } from 'unplugin-vue-components/resolvers'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    AutoImport({
      resolvers: [ElementPlusResolver()],
    }),
    Components({
      resolvers: [ElementPlusResolver()],
    }),
  ],
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5074',
        changeOrigin: true,
      },
      // 头像文件由后端 wwwroot 提供，代理转发避免被 Vite SPA fallback 拦截成 index.html
      '/avatars': {
        target: 'http://localhost:5074',
        changeOrigin: true,
      },
    },
  },
})

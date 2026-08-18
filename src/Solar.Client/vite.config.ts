import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    proxy: {
      '/api': {
        target: 'http://localhost:5142',
        changeOrigin: true,
        secure: false
      },
      '/hubs': {
        target: 'http://localhost:5142',
        ws: true,
        changeOrigin: true
      }
    }
  }
})

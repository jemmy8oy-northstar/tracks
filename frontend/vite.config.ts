import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  base: '/tracks/',
  plugins: [
    react({
      babel: {
        plugins: [['babel-plugin-react-compiler']],
      },
    }),
  ],
  server: {
    proxy: {
      // The client calls `<base>api/...` (see emptyApi.ts); the local backend serves at the root.
      '/tracks/api': {
        target: 'http://localhost:5257',
        changeOrigin: true,
        secure: false,
        rewrite: (path) => path.replace(/^\/tracks/, ''),
      },
      '/api': {
        target: 'http://localhost:5257',
        changeOrigin: true,
        secure: false,
      },
      '/openapi': {
        target: 'http://localhost:5257',
        changeOrigin: true,
        secure: false,
      }
    }
  }
})

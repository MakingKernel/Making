import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react-swc'
import path from "path"
import tailwindcss from "@tailwindcss/vite"
// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  server: {
    proxy: {
      '/connect': {
        target: 'http://localhost:5274',
        changeOrigin: true,
        secure: false,
      },
      '/api': {
        target: 'http://localhost:5274',
        changeOrigin: true,
        secure: false,
      },
      '/Account': {
        target: 'http://localhost:5274',
        changeOrigin: true,
        secure: false,
      },
      '/health': {
        target: 'http://localhost:5274',
        changeOrigin: true,
        secure: false,
      },
      '/.well-known': {
        target: 'http://localhost:5274',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})

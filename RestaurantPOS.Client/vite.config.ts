import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  server: {
    allowedHosts: [
      '.trycloudflare.com', // Allows all temporary quick tunnels

    ]
  },
  plugins: [react(), tailwindcss()],
})

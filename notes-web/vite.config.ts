/// <reference types="vitest/config" />
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// The backend address lives only here (launchSettings.json "http" profile).
// Override without editing the file: NOTES_API_URL=http://localhost:1234 npm run dev
const backendUrl = process.env.NOTES_API_URL ?? 'http://localhost:5098'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    // Dev only: /api calls go to the backend, so no CORS setup is needed (design 7).
    proxy: {
      '/api': backendUrl,
    },
  },
  // Targeted regression tests (V48): Vitest + Testing Library in jsdom, `*.test.tsx` next to the source,
  // test functions imported from 'vitest' (no globals).
  test: {
    environment: 'jsdom',
  },
})

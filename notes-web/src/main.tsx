import '@fontsource/courier-prime/400.css'
import '@fontsource/courier-prime/700.css'
import './index.css'

import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { ApiError } from './api'
import App from './App.tsx'
import { AuthProvider } from './auth/AuthProvider'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // Sync only on load, after saving and with the "Frissítés" button (design 7): no refetch on focus.
      refetchOnWindowFocus: false,
      // A 4xx answer will not change by retrying; network errors are retried once.
      retry: (failureCount, error) => !(error instanceof ApiError && error.status >= 400) && failureCount < 1,
    },
  },
})

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AuthProvider>
          <App />
        </AuthProvider>
      </BrowserRouter>
    </QueryClientProvider>
  </StrictMode>,
)

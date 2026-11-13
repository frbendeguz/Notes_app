import { useQueryClient } from '@tanstack/react-query'
import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { api, ApiError, setUnauthorizedHandler, tokenStore } from '../api'
import { AuthContext, type AuthContextValue, type AuthStatus } from './authContext'

function initialStatus(): AuthStatus {
  return tokenStore.get() ? { kind: 'checking' } : { kind: 'anonymous', sessionEnded: false }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const [status, setStatus] = useState<AuthStatus>(initialStatus)
  const [checkRound, setCheckRound] = useState(0)

  const endSession = useCallback(
    (sessionEnded: boolean) => {
      tokenStore.clear()
      queryClient.clear() // the next user must not see the previous user's cached lists
      setStatus({ kind: 'anonymous', sessionEnded })
    },
    [queryClient],
  )

  // Any authenticated request answered with 401 (expired/invalid token) ends the session (design 7).
  useEffect(() => {
    setUnauthorizedHandler(() => endSession(true))
    return () => setUnauthorizedHandler(null)
  }, [endSession])

  // On start (and on retry) a stored token is verified with GET /api/auth/me (task 06).
  useEffect(() => {
    if (!tokenStore.get()) return
    let cancelled = false
    api
      .me()
      .then((me) => {
        if (!cancelled) setStatus({ kind: 'authenticated', userName: me.userName })
      })
      .catch((error: unknown) => {
        if (cancelled) return
        // 401 is already handled by the unauthorized handler; anything else means "cannot check now".
        if (!(error instanceof ApiError && error.status === 401)) setStatus({ kind: 'unreachable' })
      })
    return () => {
      cancelled = true
    }
  }, [checkRound])

  const value = useMemo<AuthContextValue>(() => {
    const signIn = (token: string, userName: string) => {
      tokenStore.set(token)
      queryClient.clear()
      setStatus({ kind: 'authenticated', userName })
    }
    return {
      status,
      login: async (userName, password) => {
        const result = await api.login(userName, password)
        signIn(result.token, result.userName)
      },
      // Registration answers 201 with a token: the user is logged in at once (V11).
      register: async (userName, password) => {
        const result = await api.register(userName, password)
        signIn(result.token, result.userName)
      },
      logout: () => endSession(false),
      retryCheck: () => {
        setStatus({ kind: 'checking' })
        setCheckRound((round) => round + 1)
      },
    }
  }, [status, queryClient, endSession])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

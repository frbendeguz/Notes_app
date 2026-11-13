import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from './useAuth'

function StatusScreen({ children }: { children: ReactNode }) {
  return <main className="mx-auto max-w-md px-4 py-16 text-manila">{children}</main>
}

/** Protected route: only an authenticated user sees the children; otherwise → /login. */
export function RequireAuth({ children }: { children: ReactNode }) {
  const { status, retryCheck, logout } = useAuth()

  if (status.kind === 'checking') {
    return (
      <StatusScreen>
        <p role="status">A munkamenet ellenőrzése…</p>
      </StatusScreen>
    )
  }

  if (status.kind === 'unreachable') {
    return (
      <StatusScreen>
        <p role="alert" className="mb-6">
          A szerver nem érhető el, ezért a belépést nem lehet ellenőrizni.
        </p>
        <div className="flex flex-wrap gap-3">
          <button
            type="button"
            onClick={retryCheck}
            className="border-2 border-manila px-4 py-2 font-bold hover:bg-manila hover:text-ink focus-visible:outline-manila"
          >
            Újrapróbálás
          </button>
          <button
            type="button"
            onClick={logout}
            className="px-4 py-2 underline underline-offset-4 focus-visible:outline-manila"
          >
            Kilépés
          </button>
        </div>
      </StatusScreen>
    )
  }

  if (status.kind === 'anonymous') return <Navigate to="/login" replace />
  return children
}

/** Login and register pages: an authenticated user is sent to the note list. */
export function GuestOnly({ children }: { children: ReactNode }) {
  const { status } = useAuth()
  if (status.kind === 'authenticated') return <Navigate to="/notes" replace />
  if (status.kind === 'anonymous') return children
  // checking / unreachable: the protected route shows the state.
  return <Navigate to="/notes" replace />
}

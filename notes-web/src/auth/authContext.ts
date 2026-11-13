import { createContext } from 'react'

export type AuthStatus =
  | { kind: 'checking' } // a stored token is being verified with GET /api/auth/me
  | { kind: 'unreachable' } // the check failed without an answer (backend down); the token is kept
  | { kind: 'anonymous'; sessionEnded: boolean } // sessionEnded: logged out by a 401, not by the user
  | { kind: 'authenticated'; userName: string }

export interface AuthContextValue {
  status: AuthStatus
  login: (userName: string, password: string) => Promise<void>
  register: (userName: string, password: string) => Promise<void>
  logout: () => void
  /** Repeats the token check after an 'unreachable' result. */
  retryCheck: () => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)

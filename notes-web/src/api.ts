// The only place that calls fetch (frontend rules). Adds the Bearer token, sends/reads JSON,
// turns ProblemDetails into ApiError, and logs the user out on 401 (design 7).
import type {
  AuthResponse,
  FieldErrors,
  Folder,
  MeResponse,
  NoteDetail,
  NoteSummary,
  ShareInfo,
  ShareRole,
} from './types'

const TOKEN_KEY = 'notes.token'

// The JWT lives only in sessionStorage (V13): closing the tab forgets it.
export const tokenStore = {
  get(): string | null {
    return sessionStorage.getItem(TOKEN_KEY)
  },
  set(token: string): void {
    sessionStorage.setItem(TOKEN_KEY, token)
  },
  clear(): void {
    sessionStorage.removeItem(TOKEN_KEY)
  },
}

const GENERIC_ERROR = 'Váratlan hiba történt.'
const NETWORK_ERROR = 'A szerver nem érhető el. Ellenőrizd, hogy fut-e a backend.'

/** A failed API call: the HTTP status, the server's Hungarian `detail` and the field errors (400). */
export class ApiError extends Error {
  readonly status: number
  readonly errors: FieldErrors

  constructor(status: number, detail: string, errors: FieldErrors = {}) {
    super(detail)
    this.name = 'ApiError'
    this.status = status
    this.errors = errors
  }
}

/** The user-facing text of any caught error: the server's `detail`, or a generic message. */
export function errorMessage(error: unknown): string {
  return error instanceof ApiError ? error.message : GENERIC_ERROR
}

/** The server's messages for one field (`errors.<field>`), or none. */
export function fieldErrors(error: unknown, field: string): string[] {
  return error instanceof ApiError ? (error.errors[field] ?? []) : []
}

let unauthorizedHandler: (() => void) | null = null

/** The auth provider registers its logout here; called when an authenticated request gets 401. */
export function setUnauthorizedHandler(handler: (() => void) | null): void {
  unauthorizedHandler = handler
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

function readFieldErrors(value: unknown): FieldErrors {
  if (!isRecord(value)) return {}
  const result: FieldErrors = {}
  for (const [field, messages] of Object.entries(value)) {
    if (Array.isArray(messages)) {
      result[field] = messages.filter((m): m is string => typeof m === 'string')
    }
  }
  return result
}

async function readJson(response: Response): Promise<unknown> {
  const text = await response.text()
  if (text === '') return null
  try {
    return JSON.parse(text) as unknown
  } catch {
    return null
  }
}

async function apiFetch<T>(path: string, init: { method?: string; body?: unknown } = {}): Promise<T> {
  const token = tokenStore.get()
  const headers: Record<string, string> = { Accept: 'application/json' }
  if (init.body !== undefined) headers['Content-Type'] = 'application/json'
  if (token) headers.Authorization = `Bearer ${token}`

  let response: Response
  try {
    response = await fetch(path, {
      method: init.method ?? 'GET',
      headers,
      body: init.body === undefined ? undefined : JSON.stringify(init.body),
    })
  } catch {
    throw new ApiError(0, NETWORK_ERROR)
  }

  const data = await readJson(response)

  if (!response.ok) {
    // 401 with a token = missing/expired/invalid session → log out. A failed login sends no token,
    // so its 401 only shows the message and does not trigger a logout.
    if (response.status === 401 && token) unauthorizedHandler?.()
    throw toApiError(response.status, data)
  }

  return data as T
}

function toApiError(status: number, data: unknown): ApiError {
  // The backend answers every error with ProblemDetails. A 5xx without a body comes from the dev proxy
  // when the backend is not running, so it is reported as "server unreachable" (status 0).
  if (status >= 500 && data === null) return new ApiError(0, NETWORK_ERROR)
  if (!isRecord(data)) return new ApiError(status, GENERIC_ERROR)
  const detail = typeof data.detail === 'string' ? data.detail : GENERIC_ERROR
  return new ApiError(status, detail, readFieldErrors(data.errors))
}

export const api = {
  login: (userName: string, password: string) =>
    apiFetch<AuthResponse>('/api/auth/login', { method: 'POST', body: { userName, password } }),
  register: (userName: string, password: string) =>
    apiFetch<AuthResponse>('/api/auth/register', { method: 'POST', body: { userName, password } }),
  me: () => apiFetch<MeResponse>('/api/auth/me'),

  /** Own notes, optionally filtered: only notes without a folder, or only the notes of one own folder. */
  ownNotes: (filter: OwnNotesFilter) => apiFetch<NoteSummary[]>(ownNotesPath(filter)),
  sharedNotes: () => apiFetch<NoteSummary[]>('/api/notes/shared'),

  note: (id: string) => apiFetch<NoteDetail>(`/api/notes/${encodeURIComponent(id)}`),
  /** `folderId` = the folder the new note goes into (own folder, otherwise 404). */
  createNote: (title: string, content: string, folderId: string | null) =>
    apiFetch<NoteDetail>('/api/notes', { method: 'POST', body: { title, content, folderId } }),
  /** Title and text only: the server ignores a folderId here (moving is `moveNote`). */
  updateNote: (id: string, title: string, content: string) =>
    apiFetch<NoteDetail>(`/api/notes/${encodeURIComponent(id)}`, { method: 'PUT', body: { title, content } }),
  deleteNote: (id: string) => apiFetch<null>(`/api/notes/${encodeURIComponent(id)}`, { method: 'DELETE' }),
  /** `folderId: null` takes the note out of its folder. Does not change `updatedAt` (V23). */
  moveNote: (id: string, folderId: string | null) =>
    apiFetch<NoteDetail>(`/api/notes/${encodeURIComponent(id)}/folder`, { method: 'PUT', body: { folderId } }),

  /** Own folders with note counts, already in Hungarian alphabetical order (V22): do not re-sort. */
  folders: () => apiFetch<Folder[]>('/api/folders'),
  createFolder: (name: string) => apiFetch<Folder>('/api/folders', { method: 'POST', body: { name } }),
  /** The notes of the folder stay and become folderless (D4). */
  deleteFolder: (id: string) => apiFetch<null>(`/api/folders/${encodeURIComponent(id)}`, { method: 'DELETE' }),

  /** The shares of an own note, ordered by user name (V32). Owner only: others get 403/404. */
  shares: (noteId: string) => apiFetch<ShareInfo[]>(`/api/notes/${encodeURIComponent(noteId)}/shares`),
  /** 404 unknown user, 400 self (errors.userName), 409 already shared (V10, V30). */
  shareNote: (noteId: string, userName: string, role: ShareRole) =>
    apiFetch<ShareInfo>(`/api/notes/${encodeURIComponent(noteId)}/shares`, {
      method: 'POST',
      body: { userName, role },
    }),
  /** Revokes a share; the note stays with the owner. A share that is already gone → 404. */
  revokeShare: (noteId: string, userId: string) =>
    apiFetch<null>(`/api/notes/${encodeURIComponent(noteId)}/shares/${encodeURIComponent(userId)}`, {
      method: 'DELETE',
    }),
}

export type OwnNotesFilter = { kind: 'all' } | { kind: 'unfiled' } | { kind: 'folder'; folderId: string }

function ownNotesPath(filter: OwnNotesFilter): string {
  switch (filter.kind) {
    case 'all':
      return '/api/notes'
    case 'unfiled':
      return '/api/notes?unfiled=true'
    case 'folder':
      return `/api/notes?folderId=${encodeURIComponent(filter.folderId)}`
  }
}

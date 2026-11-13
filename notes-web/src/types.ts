// API DTOs (design 5). Dates arrive as ISO strings in UTC ("...Z", D7).

/** The caller's role on a note. Serialized as a string (JsonStringEnumConverter). */
export type NoteRole = 'Owner' | 'Editor' | 'Viewer'

/** Role given in a share: the owner role cannot be shared. */
export type ShareRole = 'Editor' | 'Viewer'

export interface NoteSummary {
  id: string
  title: string
  updatedAt: string
  role: NoteRole
  ownerUserName: string
  /** Owner: the folder or null. Editor/viewer: always null (V24). */
  folderId: string | null
}

export interface NoteDetail extends NoteSummary {
  content: string
  createdAt: string
}

/** Item of GET /api/folders (backend: FolderSummary). */
export interface Folder {
  id: string
  name: string
  noteCount: number
}

export interface ShareInfo {
  userId: string
  userName: string
  role: ShareRole
}

export interface AuthResponse {
  token: string
  userName: string
}

export interface MeResponse {
  id: string
  userName: string
}

/** Field name (camelCase) → user-facing messages, as in ValidationProblemDetails. */
export type FieldErrors = Record<string, string[]>

import { api } from '../api'
import type { NoteSummary } from '../types'

/**
 * The note lists of the sidebar (design 7). "all" = every own note (V36); shared notes have their own view;
 * "folder" = the notes of one own folder (task 07).
 */
export type NoteView =
  | { kind: 'all' }
  | { kind: 'unfiled' }
  | { kind: 'shared' }
  | { kind: 'folder'; folderId: string }

export type FixedViewKind = 'all' | 'unfiled' | 'shared'

interface FixedViewText {
  tabLabel: string
  heading: string
  emptyText: string
}

export const fixedViewTexts: Record<FixedViewKind, FixedViewText> = {
  all: { tabLabel: 'Összes', heading: 'Összes jegyzet', emptyText: 'Még nincs jegyzeted.' },
  unfiled: {
    tabLabel: 'Mappa nélküli',
    heading: 'Mappa nélküli jegyzetek',
    emptyText: 'Nincs mappa nélküli jegyzeted.',
  },
  shared: {
    tabLabel: 'Velem megosztott',
    heading: 'Velem megosztott jegyzetek',
    emptyText: 'Még senki nem osztott meg veled jegyzetet.',
  },
}

export const FOLDER_EMPTY_TEXT = 'Ebben a mappában nincs jegyzet.'

/** Value of `?note=` while a not yet saved new note is open. */
export const NEW_NOTE = 'new'

/**
 * Reads the view from the URL: `?view=unfiled|shared`, `?view=folder&folder=<id>`; anything else is "all".
 * (The selected note is `?note=<id>` or `?note=new`, read separately.)
 */
export function viewFromParams(params: URLSearchParams): NoteView {
  const view = params.get('view')
  if (view === 'unfiled' || view === 'shared') return { kind: view }
  const folderId = params.get('folder')
  if (view === 'folder' && folderId) return { kind: 'folder', folderId }
  return { kind: 'all' }
}

/** The `/notes?...` address of a view, optionally with an open note. */
export function viewPath(view: NoteView, noteId: string | null = null): string {
  const params = new URLSearchParams()
  if (view.kind !== 'all') params.set('view', view.kind)
  if (view.kind === 'folder') params.set('folder', view.folderId)
  if (noteId) params.set('note', noteId)
  const query = params.toString()
  return query ? `/notes?${query}` : '/notes'
}

/**
 * The view to show after a save put the note into `folderId` (V46): a folder view and "Mappa nélküli" follow the
 * note to its new place (the folder, or "Mappa nélküli" without one); "Összes" and "Velem megosztott" stay.
 */
export function viewAfterMove(view: NoteView, folderId: string | null): NoteView {
  if (view.kind !== 'folder' && view.kind !== 'unfiled') return view
  return folderId ? { kind: 'folder', folderId } : { kind: 'unfiled' }
}

export function sameView(a: NoteView, b: NoteView): boolean {
  if (a.kind === 'folder' && b.kind === 'folder') return a.folderId === b.folderId
  return a.kind === b.kind
}

// Query keys: every note list starts with 'notes', so invalidating ['notes'] refreshes them all (task 06).
export const foldersQueryKey = ['folders'] as const

export function notesQueryKey(view: NoteView): readonly unknown[] {
  switch (view.kind) {
    case 'shared':
      return ['notes', 'shared']
    case 'folder':
      return ['notes', 'own', 'folder', view.folderId]
    default:
      return ['notes', 'own', view.kind]
  }
}

export function noteDetailQueryKey(id: string): readonly unknown[] {
  return ['notes', 'detail', id]
}

export function loadNotes(view: NoteView): Promise<NoteSummary[]> {
  return view.kind === 'shared' ? api.sharedNotes() : api.ownNotes(view)
}

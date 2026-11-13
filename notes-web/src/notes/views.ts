import { api } from '../api'
import type { NoteSummary } from '../types'

/** The note lists of the sidebar. "all" = every own note (developer decision, task 06); shared notes have their own view. */
export type NoteView = 'all' | 'unfiled' | 'shared'

export interface NoteViewDefinition {
  view: NoteView
  tabLabel: string
  heading: string
  emptyText: string
  queryKey: readonly unknown[]
  load: () => Promise<NoteSummary[]>
}

export const noteViews: readonly NoteViewDefinition[] = [
  {
    view: 'all',
    tabLabel: 'Összes',
    heading: 'Összes jegyzet',
    emptyText: 'Még nincs jegyzeted.',
    queryKey: ['notes', 'own', 'all'],
    load: () => api.ownNotes(false),
  },
  {
    view: 'unfiled',
    tabLabel: 'Mappa nélküli',
    heading: 'Mappa nélküli jegyzetek',
    emptyText: 'Nincs mappa nélküli jegyzeted.',
    queryKey: ['notes', 'own', 'unfiled'],
    load: () => api.ownNotes(true),
  },
  {
    view: 'shared',
    tabLabel: 'Velem megosztott',
    heading: 'Velem megosztott jegyzetek',
    emptyText: 'Még senki nem osztott meg veled jegyzetet.',
    queryKey: ['notes', 'shared'],
    load: () => api.sharedNotes(),
  },
]

/** Reads the `?view=` URL parameter; anything unknown falls back to "all". */
export function viewFromParam(param: string | null): NoteViewDefinition {
  return noteViews.find((definition) => definition.view === param) ?? noteViews[0]
}

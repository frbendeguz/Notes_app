import { RoleStamp } from '../components/RoleStamp'
import type { NoteSummary } from '../types'
import { GuardedLink } from './GuardedLink'

const dateFormat = new Intl.DateTimeFormat('hu-HU', { dateStyle: 'medium', timeStyle: 'short' })

interface NoteListProps {
  notes: NoteSummary[]
  /** Shared view: role and owner are shown, and the rows do not open (opening comes with the roles in task 08). */
  showSharing: boolean
  selectedId: string | null
  /** The address that opens a note in the current view. */
  noteLink: (id: string) => string
  onNavigate: (to: string) => void
}

/** Title and last update; own notes open in the editor, the open one is marked. */
export function NoteList({ notes, showSharing, selectedId, noteLink, onNavigate }: NoteListProps) {
  return (
    <ul>
      {notes.map((note) => {
        const content = (
          <>
            <span className="min-w-0 break-words text-lg font-bold">{note.title}</span>
            <span className="text-sm text-faded sm:text-right">
              <span className="sr-only">Frissítve: </span>
              <time dateTime={note.updatedAt}>{dateFormat.format(new Date(note.updatedAt))}</time>
            </span>
          </>
        )
        const rowLayout = 'grid gap-x-6 gap-y-1 py-3 sm:grid-cols-[1fr_auto] sm:items-baseline'

        if (showSharing) {
          return (
            <li key={note.id} className={`${rowLayout} border-b-2 border-dotted border-manila-dark`}>
              {content}
              <span className="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm sm:col-span-2">
                <RoleStamp role={note.role} />
                <span className="text-faded">Tulajdonos: {note.ownerUserName}</span>
              </span>
            </li>
          )
        }

        const isSelected = note.id === selectedId
        return (
          <li key={note.id} className="border-b-2 border-dotted border-manila-dark">
            <GuardedLink
              to={noteLink(note.id)}
              onNavigate={onNavigate}
              aria-current={isSelected ? 'true' : undefined}
              className={[
                rowLayout,
                '-mx-2 px-2',
                isSelected ? 'bg-paper shadow-[inset_4px_0_0] shadow-stamp' : 'hover:bg-manila-dark/50',
              ].join(' ')}
            >
              {content}
            </GuardedLink>
          </li>
        )
      })}
    </ul>
  )
}

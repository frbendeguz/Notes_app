import { RoleStamp } from '../components/RoleStamp'
import type { NoteSummary } from '../types'

const dateFormat = new Intl.DateTimeFormat('hu-HU', { dateStyle: 'medium', timeStyle: 'short' })

/** Read-only list (task 06): title and last update; for shared notes also the role and the owner. */
export function NoteList({ notes, showSharing }: { notes: NoteSummary[]; showSharing: boolean }) {
  return (
    <ul>
      {notes.map((note) => (
        <li
          key={note.id}
          className="grid gap-x-6 gap-y-1 border-b-2 border-dotted border-manila-dark py-3 sm:grid-cols-[1fr_auto] sm:items-baseline"
        >
          <span className="min-w-0 break-words text-lg font-bold">{note.title}</span>
          <span className="text-sm text-faded sm:text-right">
            <span className="sr-only">Frissítve: </span>
            <time dateTime={note.updatedAt}>{dateFormat.format(new Date(note.updatedAt))}</time>
          </span>
          {showSharing && (
            <span className="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm sm:col-span-2">
              <RoleStamp role={note.role} />
              <span className="text-faded">Tulajdonos: {note.ownerUserName}</span>
            </span>
          )}
        </li>
      ))}
    </ul>
  )
}

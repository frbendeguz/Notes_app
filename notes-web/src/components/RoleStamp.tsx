import type { NoteRole } from '../types'

const roleLabels: Record<NoteRole, string> = {
  Owner: 'Tulajdonos',
  Editor: 'Szerkesztő',
  Viewer: 'Olvasó',
}

/** The caller's role as a rubber stamp: the one loud element of the design. */
export function RoleStamp({ role }: { role: NoteRole }) {
  return (
    <span className="inline-block -rotate-3 rounded-sm border-2 border-stamp px-1.5 text-sm font-bold uppercase tracking-wider text-stamp">
      {roleLabels[role]}
    </span>
  )
}

import { Link } from 'react-router-dom'
import { noteViews, type NoteView } from './views'

/**
 * The sidebar (design 7) as folder tabs sticking out of the file's left edge; on narrow screens they sit on top.
 * The active tab has the sheet's colour, so it merges into the file. Folders come in task 07.
 */
export function FolderTabs({ active }: { active: NoteView }) {
  return (
    <nav aria-label="Jegyzetlisták" className="shrink-0 md:w-48 md:pt-6">
      <ul className="flex gap-1 overflow-x-auto md:flex-col md:gap-1.5 md:overflow-visible">
        {noteViews.map(({ view, tabLabel }) => {
          const isActive = view === active
          return (
            <li key={view} className="shrink-0">
              <Link
                to={view === 'all' ? '/notes' : `/notes?view=${view}`}
                aria-current={isActive ? 'page' : undefined}
                className={[
                  'block rounded-t-sm px-3 py-2 md:rounded-t-none md:rounded-l-sm md:px-4',
                  isActive
                    ? 'bg-manila font-bold text-ink shadow-[inset_0_3px_0] shadow-stamp md:shadow-[inset_4px_0_0]'
                    : 'bg-manila-dark text-ink hover:bg-manila',
                ].join(' ')}
              >
                {tabLabel}
              </Link>
            </li>
          )
        })}
      </ul>
    </nav>
  )
}

import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api, errorMessage } from '../api'
import { ConfirmDialog } from '../components/ConfirmDialog'
import type { Folder } from '../types'
import { FolderCreateForm } from './FolderCreateForm'
import { GuardedLink } from './GuardedLink'
import { fixedViewTexts, foldersQueryKey, sameView, viewPath, type FixedViewKind, type NoteView } from './views'

interface FolderTabsProps {
  active: NoteView
  /** Folders as the API returns them: already in Hungarian alphabetical order (V22), never re-sorted here. */
  folders: Folder[] | undefined
  foldersError: unknown
  /** The open note (`?note=`), kept when the open folder is deleted. */
  openNote: string | null
  onNavigate: (to: string) => void
}

const tabBase = 'flex shrink-0 items-stretch rounded-t-sm md:rounded-t-none md:rounded-l-sm'
const activeTab = 'bg-manila font-bold shadow-[inset_0_3px_0] shadow-stamp md:shadow-[inset_4px_0_0]'
const inactiveTab = 'bg-manila-dark hover:bg-manila'

/**
 * The sidebar (design 7) as folder tabs sticking out of the file's left edge; on narrow screens they sit on top
 * in one scrollable row. Order: Összes, Mappa nélküli, own folders (with note count), Velem megosztott.
 */
export function FolderTabs({ active, folders, foldersError, openNote, onNavigate }: FolderTabsProps) {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const [toDelete, setToDelete] = useState<Folder | null>(null)

  const remove = useMutation({
    mutationFn: (folder: Folder) => api.deleteFolder(folder.id),
    onSuccess: (_data, folder) => {
      setToDelete(null)
      // The notes of the folder stay and become folderless (spec 8): show them where they went.
      if (active.kind === 'folder' && active.folderId === folder.id) {
        navigate(viewPath({ kind: 'unfiled' }, openNote), { replace: true })
      }
    },
    // Success or 404 (deleted elsewhere): the folder list, the counters and the note lists are reloaded.
    onSettled: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: foldersQueryKey }),
        queryClient.invalidateQueries({ queryKey: ['notes'] }),
      ]),
  })

  function fixedTab(kind: FixedViewKind) {
    const view: NoteView = { kind }
    const isActive = sameView(view, active)
    return (
      <li key={kind} className={`${tabBase} ${isActive ? activeTab : inactiveTab}`}>
        <GuardedLink
          to={viewPath(view)}
          onNavigate={onNavigate}
          aria-current={isActive ? 'page' : undefined}
          className="block flex-1 px-3 py-2 md:px-4"
        >
          {fixedViewTexts[kind].tabLabel}
        </GuardedLink>
      </li>
    )
  }

  return (
    <div className="shrink-0 md:w-56 md:pt-6">
      <nav aria-label="Jegyzetlisták">
        <ul className="flex gap-1 overflow-x-auto md:flex-col md:gap-1.5 md:overflow-visible">
          {fixedTab('all')}
          {fixedTab('unfiled')}
          {folders?.map((folder) => {
            const view: NoteView = { kind: 'folder', folderId: folder.id }
            const isActive = sameView(view, active)
            return (
              <li key={folder.id} className={`${tabBase} ${isActive ? activeTab : inactiveTab} md:ml-3`}>
                <GuardedLink
                  to={viewPath(view)}
                  onNavigate={onNavigate}
                  aria-current={isActive ? 'page' : undefined}
                  className="flex min-w-0 flex-1 items-baseline gap-2 py-2 pl-3 md:pl-4"
                >
                  <span className="max-w-48 truncate md:max-w-none" title={folder.name}>
                    {folder.name}
                  </span>
                  <span className="ml-auto text-sm font-normal text-faded">
                    {folder.noteCount}
                    <span className="sr-only"> jegyzet</span>
                  </span>
                </GuardedLink>
                <button
                  type="button"
                  onClick={() => {
                    remove.reset()
                    setToDelete(folder)
                  }}
                  aria-label={`„${folder.name}” mappa törlése`}
                  title="Mappa törlése"
                  className="px-2 text-lg leading-none font-bold text-faded hover:text-stamp"
                >
                  ×
                </button>
              </li>
            )
          })}
          {fixedTab('shared')}
        </ul>
      </nav>

      {foldersError !== null && (
        <p role="alert" className="mt-2 text-sm text-manila">
          A mappák nem tölthetők be: {errorMessage(foldersError)}
        </p>
      )}

      <div className="mt-3 md:mt-6 md:mr-3">
        <FolderCreateForm />
      </div>

      <ConfirmDialog
        open={toDelete !== null}
        title="Mappa törlése"
        confirmLabel="Mappa törlése"
        onConfirm={() => toDelete && remove.mutate(toDelete)}
        onCancel={() => setToDelete(null)}
        busy={remove.isPending}
        error={remove.isError ? errorMessage(remove.error) : null}
      >
        {toDelete && (
          <p>
            A(z) „{toDelete.name}” mappa törlődik.{' '}
            {toDelete.noteCount === 0
              ? 'A mappa üres.'
              : `A benne lévő ${toDelete.noteCount} jegyzet megmarad, és a „Mappa nélküli” listába kerül.`}
          </p>
        )}
      </ConfirmDialog>
    </div>
  )
}

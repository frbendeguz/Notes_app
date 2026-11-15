import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { api, errorMessage } from '../api'
import { useAuth } from '../auth/useAuth'
import { primaryButton, secondaryButton } from '../components/buttonStyles'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { ErrorAlert } from '../components/ErrorAlert'
import { FolderTabs } from '../notes/FolderTabs'
import { GuardedLink } from '../notes/GuardedLink'
import { NoteEditor } from '../notes/NoteEditor'
import { NoteList } from '../notes/NoteList'
import {
  FOLDER_EMPTY_TEXT,
  fixedViewTexts,
  foldersQueryKey,
  loadNotes,
  NEW_NOTE,
  noteDetailQueryKey,
  notesQueryKey,
  viewFromParams,
  viewPath,
} from '../notes/views'

/** What the unsaved changes dialog does when the user chooses to discard. */
type PendingLeave = { kind: 'navigate'; to: string } | { kind: 'logout' }

export function NotesPage() {
  const { status, logout } = useAuth()
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  // URL state (design 7 + developer decision, task 07): the view and the open note survive a reload.
  const view = viewFromParams(searchParams)
  const isShared = view.kind === 'shared'
  // Shared notes do not open yet (task 08 brings opening with the role rules).
  const openNote = isShared ? null : searchParams.get('note')
  const isNew = openNote === NEW_NOTE

  const folders = useQuery({ queryKey: foldersQueryKey, queryFn: api.folders })
  const notes = useQuery({ queryKey: notesQueryKey(view), queryFn: () => loadNotes(view) })
  const detail = useQuery({
    queryKey: noteDetailQueryKey(openNote ?? ''),
    queryFn: () => api.note(openNote ?? ''),
    enabled: openNote !== null && !isNew,
  })

  // Unsaved changes guard: leaving a dirty editor through the page's own links or "Kilépés" asks first
  // (V42, V45). Deleting a folder does not close the editor, so it does not ask.
  const [dirty, setDirty] = useState(false)
  const [pendingLeave, setPendingLeave] = useState<PendingLeave | null>(null)
  function go(to: string) {
    // The open note's own row (or the address already shown): nothing to leave, so nothing to ask.
    if (to === viewPath(view, openNote)) return
    if (dirty) setPendingLeave({ kind: 'navigate', to })
    else void navigate(to)
  }
  function requestLogout() {
    if (dirty) setPendingLeave({ kind: 'logout' })
    else logout()
  }

  // "Frissítés" (design 7): every note list, the open note and the folders are reloaded from the server.
  const refresh = () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: ['notes'] }),
      queryClient.invalidateQueries({ queryKey: foldersQueryKey }),
    ])

  const userName = status.kind === 'authenticated' ? status.userName : ''
  const currentFolder = view.kind === 'folder' ? folders.data?.find((f) => f.id === view.folderId) : undefined
  const heading =
    view.kind === 'folder'
      ? currentFolder
        ? `„${currentFolder.name}” mappa`
        : 'Mappa'
      : fixedViewTexts[view.kind].heading
  const emptyText = view.kind === 'folder' ? FOLDER_EMPTY_TEXT : fixedViewTexts[view.kind].emptyText
  const editorOpen = openNote !== null

  function renderEditor() {
    if (isNew) {
      return (
        <NoteEditor
          key={NEW_NOTE}
          note={null}
          folders={folders.data}
          defaultFolderId={view.kind === 'folder' ? view.folderId : null}
          onCreated={(created) => navigate(viewPath(view, created.id), { replace: true })}
          onDeleted={() => navigate(viewPath(view))}
          onClose={() => go(viewPath(view))}
          onDirtyChange={setDirty}
        />
      )
    }
    if (detail.isPending) return <p role="status">Betöltés…</p>
    if (detail.isError) {
      return (
        <div className="rounded-sm bg-paper px-5 py-6 sm:px-8">
          <ErrorAlert className="mb-4">{errorMessage(detail.error)}</ErrorAlert>
          <button type="button" onClick={() => navigate(viewPath(view))} className={secondaryButton}>
            Bezárás
          </button>
        </div>
      )
    }
    return (
      <NoteEditor
        key={detail.data.id}
        note={detail.data}
        folders={folders.data}
        defaultFolderId={null}
        onCreated={() => undefined}
        onDeleted={() => navigate(viewPath(view), { replace: true })}
        onClose={() => go(viewPath(view))}
        onDirtyChange={setDirty}
      />
    )
  }

  return (
    <div className="mx-auto max-w-7xl px-4 py-6 sm:py-10">
      <header className="mb-6 flex flex-wrap items-baseline justify-between gap-4 text-manila">
        <p className="text-2xl font-bold">Jegyzetek</p>
        <div className="flex items-baseline gap-4">
          <span>
            <span className="sr-only">Belépve: </span>
            {userName}
          </span>
          <button
            type="button"
            onClick={requestLogout}
            className="rounded-sm border-2 border-manila px-3 py-1 font-bold hover:bg-manila hover:text-ink focus-visible:outline-manila"
          >
            Kilépés
          </button>
        </div>
      </header>

      <div className="flex flex-col md:flex-row">
        <FolderTabs
          active={view}
          folders={folders.data}
          foldersError={folders.isError ? folders.error : null}
          openNote={openNote}
          onNavigate={go}
        />

        <main className="min-h-96 min-w-0 flex-1 rounded-b-sm bg-manila px-5 py-6 sm:px-8 md:rounded-sm md:rounded-tl-none">
          <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
            <div className="min-w-0">
              <h1 className="break-words text-2xl font-bold">{heading}</h1>
              {notes.data && (
                <p className="text-sm text-faded" aria-live="polite">
                  {notes.data.length} jegyzet
                </p>
              )}
            </div>
            <div className="flex flex-wrap gap-3">
              {!isShared && (
                <button
                  type="button"
                  onClick={() => go(viewPath(view, NEW_NOTE))}
                  disabled={isNew}
                  className={primaryButton}
                >
                  Új jegyzet
                </button>
              )}
              <button type="button" onClick={refresh} disabled={notes.isFetching} className={secondaryButton}>
                {notes.isFetching ? 'Frissítés…' : 'Frissítés'}
              </button>
            </div>
          </div>

          <div className={isShared ? 'max-w-3xl' : 'lg:grid lg:grid-cols-[minmax(0,2fr)_minmax(0,3fr)] lg:gap-8'}>
            {/* On narrow screens the open note takes the list's place. */}
            <section aria-label="Jegyzetlista" className={editorOpen ? 'hidden lg:block' : undefined}>
              {notes.isPending && <p role="status">Betöltés…</p>}
              {notes.isError && <ErrorAlert>{errorMessage(notes.error)}</ErrorAlert>}
              {notes.data &&
                (notes.data.length === 0 ? (
                  <p className="py-6 text-faded">{emptyText}</p>
                ) : (
                  <NoteList
                    notes={notes.data}
                    showSharing={isShared}
                    selectedId={openNote}
                    noteLink={(id) => viewPath(view, id)}
                    onNavigate={go}
                  />
                ))}
            </section>

            {!isShared && (
              <section aria-label="Szerkesztő" className={editorOpen ? undefined : 'hidden lg:block'}>
                {editorOpen ? (
                  <>
                    <GuardedLink
                      to={viewPath(view)}
                      onNavigate={go}
                      className="mb-3 inline-block font-bold underline underline-offset-4 hover:text-stamp lg:hidden"
                    >
                      ← Vissza a listához
                    </GuardedLink>
                    {renderEditor()}
                  </>
                ) : (
                  <p className="border-2 border-dashed border-manila-dark px-5 py-10 text-center text-faded">
                    Válassz egy jegyzetet a listából, vagy kezdj újat az „Új jegyzet” gombbal.
                  </p>
                )}
              </section>
            )}
          </div>
        </main>
      </div>

      <ConfirmDialog
        open={pendingLeave !== null}
        title="Nem mentett módosítások"
        confirmLabel="Elvetés és továbblépés"
        cancelLabel="Maradok"
        onConfirm={() => {
          const leave = pendingLeave
          setPendingLeave(null)
          setDirty(false)
          if (leave?.kind === 'navigate') void navigate(leave.to)
          else if (leave?.kind === 'logout') logout()
        }}
        onCancel={() => setPendingLeave(null)}
      >
        <p>A megnyitott jegyzetben nem mentett módosítások vannak. Ha továbblépsz, elvesznek.</p>
      </ConfirmDialog>
    </div>
  )
}

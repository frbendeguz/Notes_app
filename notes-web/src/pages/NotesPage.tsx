import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useSearchParams } from 'react-router-dom'
import { ApiError } from '../api'
import { useAuth } from '../auth/useAuth'
import { secondaryButton } from '../components/buttonStyles'
import { FolderTabs } from '../notes/FolderTabs'
import { NoteList } from '../notes/NoteList'
import { viewFromParam } from '../notes/views'

export function NotesPage() {
  const { status, logout } = useAuth()
  const [searchParams] = useSearchParams()
  const current = viewFromParam(searchParams.get('view'))
  const queryClient = useQueryClient()

  const notes = useQuery({ queryKey: current.queryKey, queryFn: current.load })

  // "Frissítés" (design 7): every note list is reloaded from the server.
  const refresh = () => queryClient.invalidateQueries({ queryKey: ['notes'] })

  const userName = status.kind === 'authenticated' ? status.userName : ''

  return (
    <div className="mx-auto max-w-5xl px-4 py-6 sm:py-10">
      <header className="mb-6 flex flex-wrap items-baseline justify-between gap-4 text-manila">
        <p className="text-2xl font-bold">Jegyzetek</p>
        <div className="flex items-baseline gap-4">
          <span>
            <span className="sr-only">Belépve: </span>
            {userName}
          </span>
          <button
            type="button"
            onClick={logout}
            className="rounded-sm border-2 border-manila px-3 py-1 font-bold hover:bg-manila hover:text-ink focus-visible:outline-manila"
          >
            Kilépés
          </button>
        </div>
      </header>

      <div className="flex flex-col md:flex-row">
        <FolderTabs active={current.view} />

        <main className="min-h-96 flex-1 rounded-b-sm bg-manila px-5 py-6 sm:px-8 md:rounded-sm md:rounded-tl-none">
          <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
            <div>
              <h1 className="text-2xl font-bold">{current.heading}</h1>
              {notes.data && (
                <p className="text-sm text-faded" aria-live="polite">
                  {notes.data.length} jegyzet
                </p>
              )}
            </div>
            <button type="button" onClick={refresh} disabled={notes.isFetching} className={secondaryButton}>
              {notes.isFetching ? 'Frissítés…' : 'Frissítés'}
            </button>
          </div>

          <div className="max-w-3xl">
            {notes.isPending && <p role="status">Betöltés…</p>}
            {notes.isError && (
              <p role="alert" className="border-l-4 border-stamp pl-3 font-bold text-stamp">
                {notes.error instanceof ApiError ? notes.error.message : 'Váratlan hiba történt.'}
              </p>
            )}
            {notes.data &&
              (notes.data.length === 0 ? (
                <p className="py-6 text-faded">{current.emptyText}</p>
              ) : (
                <NoteList notes={notes.data} showSharing={current.view === 'shared'} />
              ))}
          </div>
        </main>
      </div>
    </div>
  )
}

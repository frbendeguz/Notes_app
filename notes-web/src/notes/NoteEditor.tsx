import { useQueryClient } from '@tanstack/react-query'
import { useEffect, useId, useState, type FormEvent } from 'react'
import { api, ApiError, errorMessage } from '../api'
import { dangerButton, primaryButton, secondaryButton } from '../components/buttonStyles'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { ErrorAlert } from '../components/ErrorAlert'
import type { FieldErrors, Folder, NoteDetail } from '../types'
import {
  CONTENT_MAX,
  CONTENT_TOO_LONG,
  TITLE_MAX,
  TITLE_REQUIRED,
  TITLE_TOO_LONG,
  titleLength,
} from './limits'
import { foldersQueryKey, noteDetailQueryKey } from './views'

const dateFormat = new Intl.DateTimeFormat('hu-HU', { dateStyle: 'medium', timeStyle: 'short' })
const numberFormat = new Intl.NumberFormat('hu-HU')

/** What the form shows; `folderId` '' = no folder (the <select> needs a string). */
interface Draft {
  title: string
  content: string
  folderId: string
}

function draftOf(note: NoteDetail): Draft {
  return { title: note.title, content: note.content, folderId: note.folderId ?? '' }
}

function sameDraft(a: Draft, b: Draft): boolean {
  return a.title === b.title && a.content === b.content && a.folderId === b.folderId
}

interface NoteEditorProps {
  /** The saved note, or null for a new, not yet saved one. */
  note: NoteDetail | null
  /** Own folders; undefined while they are loading. */
  folders: Folder[] | undefined
  /** Folder of a new note: the folder view it was started from (POST folderId). */
  defaultFolderId: string | null
  onCreated: (note: NoteDetail) => void
  /** A save moved the note to another folder (PUT /folder succeeded): the page can follow it (V46). */
  onMoved: (note: NoteDetail) => void
  onDeleted: () => void
  onClose: () => void
  onDirtyChange: (dirty: boolean) => void
}

/**
 * The note as a typed report sheet: title, text, folder; Mentés saves all three (developer decision, task 07:
 * the folder change is sent with Mentés, as PUT /folder). The typed text lives in local state, so a failed save
 * never loses it (design 7). Mount it with `key` = note id: the draft is initialised once per note.
 */
export function NoteEditor({
  note,
  folders,
  defaultFolderId,
  onCreated,
  onMoved,
  onDeleted,
  onClose,
  onDirtyChange,
}: NoteEditorProps) {
  const queryClient = useQueryClient()
  const id = useId()

  // `saved` = the server state the draft is compared with. A new note starts empty in the default folder.
  const [saved, setSaved] = useState<Draft>(() =>
    note ? draftOf(note) : { title: '', content: '', folderId: defaultFolderId ?? '' },
  )
  const [draft, setDraft] = useState<Draft>(saved)
  const [errors, setErrors] = useState<FieldErrors>({})
  const [error, setError] = useState<string | null>(null)
  const [justSaved, setJustSaved] = useState(false)
  const [saving, setSaving] = useState(false)
  const [confirmDelete, setConfirmDelete] = useState(false)
  const [deleting, setDeleting] = useState(false)

  const dirty = !sameDraft(draft, saved)

  // "Frissítés" or another device changed the note: an untouched form follows the server; a form with
  // unsaved typing is left alone (last write wins, spec).
  if (note && !dirty && !saving && !sameDraft(draftOf(note), saved)) {
    setSaved(draftOf(note))
    setDraft(draftOf(note))
  }

  useEffect(() => {
    onDirtyChange(dirty)
  }, [dirty, onDirtyChange])
  useEffect(() => () => onDirtyChange(false), [onDirtyChange])

  function edit(change: Partial<Draft>) {
    setDraft((current) => ({ ...current, ...change }))
    setJustSaved(false)
  }

  function clientErrors(value: Draft): FieldErrors {
    const result: FieldErrors = {}
    const length = titleLength(value.title)
    if (length === 0) result.title = [TITLE_REQUIRED]
    else if (length > TITLE_MAX) result.title = [TITLE_TOO_LONG]
    if (value.content.length > CONTENT_MAX) result.content = [CONTENT_TOO_LONG]
    return result
  }

  /** The server's answer becomes the cached detail at once, so the form never compares with stale data. */
  function remember(detail: NoteDetail) {
    queryClient.setQueryData(noteDetailQueryKey(detail.id), detail)
  }

  /** After saving/deleting: lists and folder counters are reloaded (task 07). */
  async function refreshLists() {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['notes'] }),
      queryClient.invalidateQueries({ queryKey: foldersQueryKey }),
    ])
  }

  async function handleSave(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const found = clientErrors(draft)
    setErrors(found)
    setError(Object.keys(found).length > 0 ? 'A megadott adatok érvénytelenek.' : null)
    if (Object.keys(found).length > 0) return

    const toSave = draft
    setSaving(true)
    try {
      if (!note) {
        const created = await api.createNote(toSave.title, toSave.content, toSave.folderId || null)
        remember(created)
        await refreshLists()
        onCreated(created)
        return
      }
      if (toSave.title !== saved.title || toSave.content !== saved.content) {
        const updated = await api.updateNote(note.id, toSave.title, toSave.content)
        remember(updated)
        // Text saved (the title trimmed by the server): only the folder can still differ, if the move fails.
        setSaved((current) => ({ ...current, title: updated.title, content: updated.content }))
        setDraft((current) => ({ ...current, title: updated.title }))
      }
      let moved: NoteDetail | null = null
      if (toSave.folderId !== saved.folderId) {
        moved = await api.moveNote(note.id, toSave.folderId || null)
        remember(moved)
        setSaved(draftOf(moved))
      }
      setErrors({})
      setError(null)
      setJustSaved(true)
      if (moved) onMoved(moved)
    } catch (caught: unknown) {
      // The draft is untouched: the user can fix it and save again.
      setErrors(caught instanceof ApiError ? caught.errors : {})
      setError(errorMessage(caught))
    } finally {
      setSaving(false)
      if (note) await refreshLists()
    }
  }

  async function handleDelete() {
    if (!note) return
    setDeleting(true)
    try {
      await api.deleteNote(note.id)
      queryClient.removeQueries({ queryKey: noteDetailQueryKey(note.id) })
      setConfirmDelete(false)
      onDirtyChange(false)
      onDeleted()
    } catch (caught: unknown) {
      setConfirmDelete(false)
      setError(errorMessage(caught))
      setErrors({})
    } finally {
      setDeleting(false)
      await refreshLists()
    }
  }

  const titleId = `${id}-title`
  const contentId = `${id}-content`
  const folderId = `${id}-folder`
  const titleCount = titleLength(draft.title)
  const titleOver = titleCount > TITLE_MAX
  const contentOver = draft.content.length > CONTENT_MAX
  const titleErrors = errors.title ?? []
  const contentErrors = errors.content ?? []
  const folderErrors = errors.folderId ?? []
  const busy = saving || deleting

  // The draft's folder must have an option, or the <select> would show "(nincs mappa)": while the folders load,
  // and when the folder was deleted meanwhile (saving then gets the server's 404 message).
  const folderList = folders ?? []
  const folderMissing = draft.folderId !== '' && !folderList.some((folder) => folder.id === draft.folderId)

  return (
    <article
      aria-labelledby={`${id}-heading`}
      className="rounded-sm bg-paper px-5 py-6 shadow-[3px_3px_0] shadow-manila-dark sm:px-8"
    >
      <div className="mb-5 flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1 border-b-2 border-ink pb-2">
        <h2 id={`${id}-heading`} className="text-xl font-bold">
          {note ? 'Jegyzet' : 'Új jegyzet'}
        </h2>
        {note && (
          <p className="flex flex-wrap gap-x-4 text-sm text-faded">
            <span>
              Létrehozva: <time dateTime={note.createdAt}>{dateFormat.format(new Date(note.createdAt))}</time>
            </span>
            <span>
              Frissítve: <time dateTime={note.updatedAt}>{dateFormat.format(new Date(note.updatedAt))}</time>
            </span>
          </p>
        )}
      </div>

      {error && <ErrorAlert className="mb-5">{error}</ErrorAlert>}

      <form onSubmit={handleSave} noValidate>
        <div className="mb-5">
          <div className="flex items-baseline justify-between gap-4">
            <label htmlFor={titleId} className="font-bold">
              Cím
            </label>
            <span id={`${titleId}-count`} className={`text-sm ${titleOver ? 'font-bold text-stamp' : 'text-faded'}`}>
              {titleCount}/{TITLE_MAX}
            </span>
          </div>
          <input
            id={titleId}
            value={draft.title}
            onChange={(event) => edit({ title: event.target.value })}
            autoComplete="off"
            aria-invalid={titleErrors.length > 0 || titleOver}
            aria-describedby={[`${titleId}-count`, titleErrors.length > 0 ? `${titleId}-error` : '']
              .filter(Boolean)
              .join(' ')}
            className="mt-1 w-full border-b-2 border-ink bg-transparent px-1 py-1.5 text-2xl font-bold aria-invalid:border-stamp"
          />
          <FieldMessages id={`${titleId}-error`} messages={titleErrors} />
        </div>

        <div className="mb-5">
          <div className="flex items-baseline justify-between gap-4">
            <label htmlFor={contentId} className="font-bold">
              Szöveg
            </label>
            <span
              id={`${contentId}-count`}
              className={`text-sm ${contentOver ? 'font-bold text-stamp' : 'text-faded'}`}
            >
              {numberFormat.format(draft.content.length)}/{numberFormat.format(CONTENT_MAX)}
            </span>
          </div>
          {/* Ruled sheet: the lines are drawn every 2rem and scroll with the text (bg-local). */}
          <textarea
            id={contentId}
            value={draft.content}
            onChange={(event) => edit({ content: event.target.value })}
            rows={14}
            aria-invalid={contentErrors.length > 0 || contentOver}
            aria-describedby={[`${contentId}-count`, contentErrors.length > 0 ? `${contentId}-error` : '']
              .filter(Boolean)
              .join(' ')}
            className="mt-1 block w-full resize-y border-2 border-manila-dark bg-transparent bg-[repeating-linear-gradient(to_bottom,transparent_0,transparent_calc(2rem-1px),var(--color-manila-dark)_calc(2rem-1px),var(--color-manila-dark)_2rem)] bg-local px-2 leading-8 aria-invalid:border-stamp"
          />
          <FieldMessages id={`${contentId}-error`} messages={contentErrors} />
        </div>

        <div className="mb-6">
          <label htmlFor={folderId} className="font-bold">
            Mappa
          </label>
          <select
            id={folderId}
            value={draft.folderId}
            onChange={(event) => edit({ folderId: event.target.value })}
            aria-invalid={folderErrors.length > 0}
            aria-describedby={folderErrors.length > 0 ? `${folderId}-error` : undefined}
            className="mt-1 block w-full max-w-sm border-b-2 border-ink bg-transparent px-1 py-1.5"
          >
            <option value="">(nincs mappa)</option>
            {folderMissing && (
              <option value={draft.folderId}>{folders ? '(törölt mappa)' : 'Betöltés…'}</option>
            )}
            {folderList.map((folder) => (
              <option key={folder.id} value={folder.id}>
                {folder.name}
              </option>
            ))}
          </select>
          <FieldMessages id={`${folderId}-error`} messages={folderErrors} />
        </div>

        <div className="flex flex-wrap items-center gap-3">
          <button type="submit" disabled={busy} className={primaryButton}>
            {saving ? 'Mentés…' : 'Mentés'}
          </button>
          <button type="button" onClick={onClose} disabled={busy} className={secondaryButton}>
            Bezárás
          </button>
          <span role="status" className="text-sm text-faded">
            {justSaved && !dirty ? 'Mentve.' : dirty ? 'Nem mentett módosítás.' : ''}
          </span>
          {note && (
            <button
              type="button"
              onClick={() => setConfirmDelete(true)}
              disabled={busy}
              className={`${dangerButton} ml-auto`}
            >
              Törlés
            </button>
          )}
        </div>
      </form>

      <ConfirmDialog
        open={confirmDelete}
        title="Jegyzet törlése"
        confirmLabel="Jegyzet törlése"
        onConfirm={() => void handleDelete()}
        onCancel={() => setConfirmDelete(false)}
        busy={deleting}
      >
        <p>A(z) „{note?.title}” jegyzet véglegesen törlődik, és a vele megosztott felhasználók sem érik el többé.</p>
      </ConfirmDialog>
    </article>
  )
}

function FieldMessages({ id, messages }: { id: string; messages: string[] }) {
  if (messages.length === 0) return null
  return (
    <ul id={id} className="mt-2 text-sm font-bold text-stamp">
      {messages.map((message) => (
        <li key={message}>{message}</li>
      ))}
    </ul>
  )
}

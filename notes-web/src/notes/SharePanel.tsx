import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useId, useState, type FormEvent } from 'react'
import { api, errorMessage, fieldErrors } from '../api'
import { secondaryButton } from '../components/buttonStyles'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { ErrorAlert } from '../components/ErrorAlert'
import { RoleStamp } from '../components/RoleStamp'
import type { ShareInfo, ShareRole } from '../types'
import { SHARE_USER_NAME_REQUIRED } from './limits'
import { sharesQueryKey } from './views'

const roleOptions: { value: ShareRole; label: string }[] = [
  { value: 'Viewer', label: 'Olvasó' },
  { value: 'Editor', label: 'Szerkesztő' },
]

interface SharePanelProps {
  noteId: string
  noteTitle: string
}

/**
 * "Megosztás" section of an own, saved note (developer decision, task 08: a section on the editor sheet).
 * Share by user name and role, the current shares, revoke after a confirmation. There is no role change (V4):
 * revoke and share again. The server decides; its messages (404 unknown user, 400 self, 409 repeated, V10) are shown.
 */
export function SharePanel({ noteId, noteTitle }: SharePanelProps) {
  const queryClient = useQueryClient()
  const id = useId()
  const [userName, setUserName] = useState('')
  const [role, setRole] = useState<ShareRole>('Viewer')
  const [clientError, setClientError] = useState<string | null>(null)
  const [toRevoke, setToRevoke] = useState<ShareInfo | null>(null)

  const shares = useQuery({ queryKey: sharesQueryKey(noteId), queryFn: () => api.shares(noteId) })
  const reloadShares = () => queryClient.invalidateQueries({ queryKey: sharesQueryKey(noteId) })

  const share = useMutation({
    mutationFn: (request: { userName: string; role: ShareRole }) =>
      api.shareNote(noteId, request.userName, request.role),
    onSuccess: () => {
      setUserName('')
      return reloadShares()
    },
  })

  const revoke = useMutation({
    mutationFn: (target: ShareInfo) => api.revokeShare(noteId, target.userId),
    onSuccess: () => setToRevoke(null),
    // Also after a 404 (revoked meanwhile, e.g. in another tab): the list shows the server's state.
    onSettled: reloadShares,
  })

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const problem = userName.trim() === '' ? SHARE_USER_NAME_REQUIRED : null
    setClientError(problem)
    if (problem) {
      share.reset()
      return
    }
    share.mutate({ userName, role })
  }

  // Field messages first (400: self share → errors.userName, invalid role → errors.role), otherwise the detail
  // (404 "Nincs ilyen nevű felhasználó.", 409 "A jegyzet már meg van osztva ezzel a felhasználóval.").
  const serverMessages = share.isError
    ? [...fieldErrors(share.error, 'userName'), ...fieldErrors(share.error, 'role')]
    : []
  let messages: string[] = []
  if (clientError) messages = [clientError]
  else if (serverMessages.length > 0) messages = serverMessages
  else if (share.isError) messages = [errorMessage(share.error)]

  const userNameId = `${id}-user`
  const roleId = `${id}-role`
  const errorId = `${id}-error`

  return (
    <section aria-labelledby={`${id}-heading`} className="mt-8 border-t-2 border-dashed border-manila-dark pt-5">
      <h3 id={`${id}-heading`} className="mb-3 text-lg font-bold">
        Megosztás
      </h3>

      <form onSubmit={handleSubmit} noValidate className="flex flex-wrap items-end gap-x-4 gap-y-3">
        <div className="min-w-48 flex-1">
          <label htmlFor={userNameId} className="block text-sm font-bold">
            Felhasználónév
          </label>
          <input
            id={userNameId}
            value={userName}
            onChange={(event) => setUserName(event.target.value)}
            autoComplete="off"
            aria-invalid={messages.length > 0}
            aria-describedby={messages.length > 0 ? errorId : undefined}
            className="mt-1 w-full border-b-2 border-ink bg-transparent px-1 py-1 aria-invalid:border-stamp"
          />
        </div>
        <div>
          <label htmlFor={roleId} className="block text-sm font-bold">
            Szerep
          </label>
          <select
            id={roleId}
            value={role}
            onChange={(event) => setRole(event.target.value === 'Editor' ? 'Editor' : 'Viewer')}
            className="mt-1 border-b-2 border-ink bg-transparent px-1 py-1"
          >
            {roleOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </div>
        <button type="submit" disabled={share.isPending} className={secondaryButton}>
          {share.isPending ? 'Megosztás…' : 'Megosztás'}
        </button>
      </form>
      {messages.length > 0 && (
        <ul id={errorId} role="alert" className="mt-2 text-sm font-bold text-stamp">
          {messages.map((message) => (
            <li key={message}>{message}</li>
          ))}
        </ul>
      )}

      <h4 className="mt-5 text-sm font-bold">Jelenlegi megosztások</h4>
      {shares.isPending && <p role="status">Betöltés…</p>}
      {/* A failed reload keeps the last list (the editor already shows why, e.g. the server is down). */}
      {shares.isError && !shares.data && <ErrorAlert className="mt-2">{errorMessage(shares.error)}</ErrorAlert>}
      {shares.data &&
        (shares.data.length === 0 ? (
          <p className="mt-2 text-sm text-faded">Még senkivel nem osztottad meg.</p>
        ) : (
          <ul aria-label="Jelenlegi megosztások" className="mt-1">
            {shares.data.map((item) => (
              <li
                key={item.userId}
                className="flex flex-wrap items-center gap-x-4 gap-y-1 border-b-2 border-dotted border-manila-dark py-2"
              >
                <span className="min-w-0 flex-1 break-words font-bold">{item.userName}</span>
                <RoleStamp role={item.role} />
                <button
                  type="button"
                  onClick={() => {
                    revoke.reset()
                    setToRevoke(item)
                  }}
                  aria-label={`${item.userName} megosztásának visszavonása`}
                  className="text-sm font-bold text-stamp underline underline-offset-4 hover:text-ink"
                >
                  Visszavonás
                </button>
              </li>
            ))}
          </ul>
        ))}

      <ConfirmDialog
        open={toRevoke !== null}
        title="Megosztás visszavonása"
        confirmLabel="Visszavonás"
        onConfirm={() => toRevoke && revoke.mutate(toRevoke)}
        onCancel={() => setToRevoke(null)}
        busy={revoke.isPending}
        error={revoke.isError ? errorMessage(revoke.error) : null}
      >
        <p>
          {toRevoke?.userName} a következő lekéréstől nem éri el a(z) „{noteTitle}” jegyzetet. A jegyzet nálad
          megmarad.
        </p>
      </ConfirmDialog>
    </section>
  )
}

import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useId, useState, type FormEvent } from 'react'
import { api, errorMessage, fieldErrors } from '../api'
import { FOLDER_NAME_MAX, FOLDER_NAME_REQUIRED, FOLDER_NAME_TOO_LONG } from './limits'
import { foldersQueryKey } from './views'

/** New folder on a small index card in the sidebar. The client check only helps; the server decides (409 etc.). */
export function FolderCreateForm() {
  const queryClient = useQueryClient()
  const [name, setName] = useState('')
  const [clientError, setClientError] = useState<string | null>(null)
  const id = useId()
  const errorId = `${id}-error`

  const create = useMutation({
    mutationFn: (folderName: string) => api.createFolder(folderName),
    onSuccess: () => {
      setName('')
      return queryClient.invalidateQueries({ queryKey: foldersQueryKey })
    },
  })

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const trimmed = name.trim()
    const problem =
      trimmed.length === 0 ? FOLDER_NAME_REQUIRED : trimmed.length > FOLDER_NAME_MAX ? FOLDER_NAME_TOO_LONG : null
    setClientError(problem)
    if (problem) {
      create.reset()
      return
    }
    create.mutate(name)
  }

  // Field message from the server (400 errors.name), otherwise its detail (409 "Ilyen nevű mappád már van.").
  const serverMessages = create.isError
    ? fieldErrors(create.error, 'name').length > 0
      ? fieldErrors(create.error, 'name')
      : [errorMessage(create.error)]
    : []
  const messages = clientError ? [clientError] : serverMessages

  return (
    <form onSubmit={handleSubmit} noValidate className="rounded-sm bg-manila p-3">
      <label htmlFor={id} className="block text-sm font-bold">
        Új mappa
      </label>
      <input
        id={id}
        value={name}
        onChange={(event) => setName(event.target.value)}
        autoComplete="off"
        aria-invalid={messages.length > 0}
        aria-describedby={messages.length > 0 ? errorId : undefined}
        className="mt-1 w-full border-b-2 border-ink bg-transparent px-1 py-1 aria-invalid:border-stamp"
      />
      {messages.length > 0 && (
        <ul id={errorId} role="alert" className="mt-2 text-sm font-bold text-stamp">
          {messages.map((message) => (
            <li key={message}>{message}</li>
          ))}
        </ul>
      )}
      <button
        type="submit"
        disabled={create.isPending}
        className="mt-3 rounded-sm bg-ink px-3 py-1 text-sm font-bold text-manila hover:bg-stamp disabled:cursor-wait disabled:opacity-70"
      >
        {create.isPending ? 'Létrehozás…' : 'Létrehozás'}
      </button>
    </form>
  )
}

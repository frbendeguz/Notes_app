import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, fireEvent, render, screen, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { NoteDetail, NoteRole } from '../types'
import { NoteEditor } from './NoteEditor'

// Task 08: role-based editor (design 4, 7). The hiding is only a convenience: the server's 403 is shown too (AC6).

function noteAs(role: NoteRole): NoteDetail {
  return {
    id: 'n1',
    title: 'Közös akta',
    content: 'eredeti',
    updatedAt: '2026-11-18T10:00:00Z',
    createdAt: '2026-11-18T10:00:00Z',
    role,
    ownerUserName: 'anna',
    folderId: null,
  }
}

let putAnswer: { status: number; body: unknown } = { status: 200, body: noteAs('Editor') }

function json(data: unknown, status = 200): Response {
  return new Response(JSON.stringify(data), { status, headers: { 'Content-Type': 'application/json' } })
}

async function fakeFetch(input: RequestInfo | URL, init?: RequestInit): Promise<Response> {
  const url = String(input)
  const method = init?.method ?? 'GET'
  if (method === 'GET' && url === '/api/notes/n1/shares') return json([])
  if (method === 'PUT' && url === '/api/notes/n1') return json(putAnswer.body, putAnswer.status)
  return json({ detail: 'Ismeretlen kérés a tesztben.' }, 404)
}

function renderEditor(role: NoteRole, unavailableMessage: string | null = null) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={queryClient}>
      <NoteEditor
        note={noteAs(role)}
        folders={[]}
        defaultFolderId={null}
        onCreated={vi.fn()}
        onMoved={vi.fn()}
        onDeleted={vi.fn()}
        onClose={vi.fn()}
        onDirtyChange={vi.fn()}
        unavailableMessage={unavailableMessage}
      />
    </QueryClientProvider>,
  )
  return screen.getByRole('article', { name: 'Jegyzet' })
}

function field(label: 'Cím' | 'Szöveg'): HTMLInputElement | HTMLTextAreaElement {
  return screen.getByLabelText(label) as HTMLInputElement | HTMLTextAreaElement
}

beforeEach(() => {
  putAnswer = { status: 200, body: noteAs('Editor') }
  vi.stubGlobal('fetch', vi.fn(fakeFetch))
})

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})

describe('NoteEditor – role-based UI', () => {
  it('viewer: read-only fields, no save, delete, folder or sharing; role and owner are shown', () => {
    const editor = renderEditor('Viewer')

    expect(field('Cím').readOnly).toBe(true)
    expect(field('Szöveg').readOnly).toBe(true)
    expect(within(editor).queryByRole('button', { name: 'Mentés' })).toBeNull()
    expect(within(editor).queryByRole('button', { name: 'Törlés' })).toBeNull()
    expect(within(editor).queryByLabelText('Mappa')).toBeNull()
    expect(within(editor).queryByRole('heading', { name: 'Megosztás' })).toBeNull()
    expect(within(editor).getByText('Olvasó')).toBeTruthy()
    expect(within(editor).getByText('Tulajdonos: anna')).toBeTruthy()
  })

  it('editor: title and text editable with save, but no delete, folder or sharing', () => {
    const editor = renderEditor('Editor')

    expect(field('Cím').readOnly).toBe(false)
    expect(field('Szöveg').readOnly).toBe(false)
    expect(within(editor).getByRole('button', { name: 'Mentés' })).toBeTruthy()
    expect(within(editor).queryByRole('button', { name: 'Törlés' })).toBeNull()
    expect(within(editor).queryByLabelText('Mappa')).toBeNull()
    expect(within(editor).queryByRole('heading', { name: 'Megosztás' })).toBeNull()
    expect(within(editor).getByText('Szerkesztő')).toBeTruthy()
    expect(within(editor).getByText('Tulajdonos: anna')).toBeTruthy()
  })

  it('owner: save, delete, folder and the sharing section', () => {
    const editor = renderEditor('Owner')

    expect(within(editor).getByRole('button', { name: 'Mentés' })).toBeTruthy()
    expect(within(editor).getByRole('button', { name: 'Törlés' })).toBeTruthy()
    expect(within(editor).getByLabelText('Mappa')).toBeTruthy()
    expect(within(editor).getByRole('heading', { name: 'Megosztás' })).toBeTruthy()
    expect(within(editor).queryByText('Tulajdonos: anna')).toBeNull()
  })

  it("shows the server's 403 on save and keeps the typed text", async () => {
    // AC6: the client-side role check is only a convenience.
    putAnswer = { status: 403, body: { detail: 'Nincs jogosultságod ehhez a művelethez.' } }
    const editor = renderEditor('Editor')
    fireEvent.change(field('Szöveg'), { target: { value: 'eredeti + új sor' } })
    fireEvent.click(within(editor).getByRole('button', { name: 'Mentés' }))

    expect((await within(editor).findByRole('alert')).textContent).toBe('Nincs jogosultságod ehhez a művelethez.')
    expect(field('Szöveg').value).toBe('eredeti + új sor')
  })

  it('an unavailable note shows the message and cannot be saved', () => {
    const editor = renderEditor('Editor', 'A jegyzet már nem érhető el.')

    expect(within(editor).getByRole('alert').textContent).toBe('A jegyzet már nem érhető el.')
    expect((within(editor).getByRole('button', { name: 'Mentés' }) as HTMLButtonElement).disabled).toBe(true)
  })

  it('an unavailable own note cannot be deleted and hides the sharing section', () => {
    const editor = renderEditor('Owner', 'A jegyzet már nem érhető el.')

    expect((within(editor).getByRole('button', { name: 'Törlés' }) as HTMLButtonElement).disabled).toBe(true)
    expect(within(editor).queryByRole('heading', { name: 'Megosztás' })).toBeNull()
  })
})

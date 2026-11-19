import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { act, cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { AuthContext, type AuthContextValue } from '../auth/authContext'
import type { NoteDetail } from '../types'
import { NotesPage } from './NotesPage'

// Regression test for manual step 31 of task 07 (V47, V48): a failed save must keep the editor and the typed text.

const NOTE: NoteDetail = {
  id: 'n1',
  title: 'Hálózati próba',
  content: 'eredeti',
  updatedAt: '2026-11-18T10:00:00Z',
  createdAt: '2026-11-18T10:00:00Z',
  role: 'Owner',
  ownerUserName: 'teszt_c1',
  folderId: null,
}

/** 'up' = the backend answers; 'network' = fetch throws; 'empty502' = the dev proxy's 5xx without a body. */
type Backend = 'up' | 'network' | 'empty502'

interface Call {
  method: string
  url: string
  body: unknown
}

let backend: Backend = 'up'
let calls: Call[] = []

function json(data: unknown, status = 200): Response {
  return new Response(JSON.stringify(data), { status, headers: { 'Content-Type': 'application/json' } })
}

async function fakeFetch(input: RequestInfo | URL, init?: RequestInit): Promise<Response> {
  const url = String(input)
  const method = init?.method ?? 'GET'
  const body: unknown = typeof init?.body === 'string' ? JSON.parse(init.body) : undefined
  calls.push({ method, url, body })

  if (backend === 'network') throw new TypeError('Failed to fetch')
  if (backend === 'empty502') return new Response(null, { status: 502 })

  if (method === 'GET' && url === '/api/folders') return json([])
  if (method === 'GET' && url === '/api/notes') return json([NOTE])
  if (method === 'GET' && url === '/api/notes/n1') return json(NOTE)
  // The owner's editor has the sharing section since task 08.
  if (method === 'GET' && url === '/api/notes/n1/shares') return json([])
  if (method === 'PUT' && url === '/api/notes/n1') {
    const sent = body as { title: string; content: string }
    return json({ ...NOTE, title: sent.title, content: sent.content, updatedAt: '2026-11-18T11:00:00Z' })
  }
  return json({ detail: 'Ismeretlen kérés a tesztben.' }, 404)
}

const auth: AuthContextValue = {
  status: { kind: 'authenticated', userName: 'teszt_c1' },
  login: vi.fn(),
  register: vi.fn(),
  logout: vi.fn(),
  retryCheck: vi.fn(),
}

function renderNotesPage() {
  // Same sync rules as main.tsx, but without retries, so a failed request settles at once.
  const queryClient = new QueryClient({
    defaultOptions: { queries: { refetchOnWindowFocus: false, retry: false } },
  })
  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/notes?note=n1']}>
        <AuthContext.Provider value={auth}>
          <NotesPage />
        </AuthContext.Provider>
      </MemoryRouter>
    </QueryClientProvider>,
  )
  return queryClient
}

function contentField(): HTMLTextAreaElement | null {
  const field = screen.queryByLabelText('Szöveg')
  return field instanceof HTMLTextAreaElement ? field : null
}

beforeEach(() => {
  backend = 'up'
  calls = []
  vi.stubGlobal('fetch', vi.fn(fakeFetch))
})

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})

describe('NotesPage – failed save (V47)', () => {
  it.each<[string, Backend]>([
    ['network error', 'network'],
    ['5xx without a body', 'empty502'],
  ])('keeps the editor and the typed text after a %s, and saves it when the backend is back', async (_, failure) => {
    const queryClient = renderNotesPage()

    await waitFor(() => expect(contentField()?.value).toBe('eredeti'))
    const typed = 'eredeti – a leállított backend mellett beírt sor'
    fireEvent.change(contentField()!, { target: { value: typed } })

    // The backend stops; the save fails, and the lists and the open note are reloaded (and fail too).
    backend = failure
    const detailLoadsBefore = calls.filter((c) => c.method === 'GET' && c.url === '/api/notes/n1').length
    fireEvent.click(screen.getByRole('button', { name: 'Mentés' }))
    await waitFor(() => {
      const detailLoads = calls.filter((c) => c.method === 'GET' && c.url === '/api/notes/n1').length
      expect(detailLoads).toBeGreaterThan(detailLoadsBefore)
    })
    await waitFor(() => expect(queryClient.isFetching()).toBe(0))
    await act(async () => {})

    const field = contentField()
    expect(field, 'a szerkesztő „Szöveg” mezője látható marad').not.toBeNull()
    expect(field?.value, 'a beírt szöveg megmarad').toBe(typed)
    const editor = screen.getByRole('article', { name: 'Jegyzet' })
    expect(within(editor).getByRole('alert').textContent).toBe(
      'A szerver nem érhető el. Ellenőrizd, hogy fut-e a backend.',
    )

    // The backend is back: Mentés works again and sends the typed text.
    backend = 'up'
    fireEvent.click(within(editor).getByRole('button', { name: 'Mentés' }))
    await screen.findByText('Mentve.')
    const puts = calls.filter((c) => c.method === 'PUT' && c.url === '/api/notes/n1')
    expect(puts.at(-1)?.body).toEqual({ title: NOTE.title, content: typed })
  })
})

import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { AuthContext, type AuthContextValue } from '../auth/authContext'
import { NOTE_UNAVAILABLE } from '../notes/views'
import type { NoteDetail } from '../types'
import { NotesPage } from './NotesPage'

// Task 08: a shared note opens from "Velem megosztott" with its role; after the share is revoked the next
// reload shows a message and the list no longer has the note (AC5, AC7).

const SHARED: NoteDetail = {
  id: 's1',
  title: 'Anna aktája',
  content: 'megosztott szöveg',
  updatedAt: '2026-11-18T10:00:00Z',
  createdAt: '2026-11-18T10:00:00Z',
  role: 'Editor',
  ownerUserName: 'anna',
  folderId: null,
}

let revoked = false

function json(data: unknown, status = 200): Response {
  return new Response(JSON.stringify(data), { status, headers: { 'Content-Type': 'application/json' } })
}

async function fakeFetch(input: RequestInfo | URL, init?: RequestInit): Promise<Response> {
  const url = String(input)
  const method = init?.method ?? 'GET'
  if (method === 'GET' && url === '/api/folders') return json([])
  if (method === 'GET' && url === '/api/notes/shared') return json(revoked ? [] : [SHARED])
  if (method === 'GET' && url === '/api/notes/s1') {
    return revoked ? json({ detail: 'A jegyzet nem található.' }, 404) : json(SHARED)
  }
  if (method === 'PUT' && url === '/api/notes/s1') {
    return revoked ? json({ detail: 'A jegyzet nem található.' }, 404) : json(SHARED)
  }
  return json({ detail: 'Ismeretlen kérés a tesztben.' }, 404)
}

const auth: AuthContextValue = {
  status: { kind: 'authenticated', userName: 'bela' },
  login: vi.fn(),
  register: vi.fn(),
  logout: vi.fn(),
  retryCheck: vi.fn(),
}

function renderSharedView() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { refetchOnWindowFocus: false, retry: false } } })
  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/notes?view=shared']}>
        <AuthContext.Provider value={auth}>
          <NotesPage />
        </AuthContext.Provider>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

beforeEach(() => {
  revoked = false
  vi.stubGlobal('fetch', vi.fn(fakeFetch))
})

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})

describe('NotesPage – "Velem megosztott"', () => {
  it('opens a shared note with its role, and after a revoke the refresh shows a message and empties the list', async () => {
    renderSharedView()
    const list = screen.getByRole('region', { name: 'Jegyzetlista' })
    fireEvent.click(await within(list).findByRole('link', { name: /Anna aktája/ }))

    const editor = await screen.findByRole('article', { name: 'Jegyzet' })
    expect(within(editor).getByText('Szerkesztő')).toBeTruthy()
    expect(within(editor).getByText('Tulajdonos: anna')).toBeTruthy()
    const content = within(editor).getByLabelText('Szöveg') as HTMLTextAreaElement
    await waitFor(() => expect(content.value).toBe('megosztott szöveg'))
    fireEvent.change(content, { target: { value: 'megosztott szöveg + beírt sor' } })

    // The owner revokes the share; the recipient presses "Frissítés".
    revoked = true
    fireEvent.click(screen.getByRole('button', { name: 'Frissítés' }))

    expect(await within(list).findByText('Még senki nem osztott meg veled jegyzetet.')).toBeTruthy()
    await waitFor(() => expect(within(editor).getByRole('alert').textContent).toBe(NOTE_UNAVAILABLE))
    expect(content.value).toBe('megosztott szöveg + beírt sor')
    expect((within(editor).getByRole('button', { name: 'Mentés' }) as HTMLButtonElement).disabled).toBe(true)
  })

  it('a save that gets 404 after a revoke shows the message, keeps the typed text and empties the list', async () => {
    renderSharedView()
    const list = screen.getByRole('region', { name: 'Jegyzetlista' })
    fireEvent.click(await within(list).findByRole('link', { name: /Anna aktája/ }))
    const editor = await screen.findByRole('article', { name: 'Jegyzet' })
    const content = within(editor).getByLabelText('Szöveg') as HTMLTextAreaElement
    await waitFor(() => expect(content.value).toBe('megosztott szöveg'))
    fireEvent.change(content, { target: { value: 'mentés előtt beírt sor' } })

    // The owner revokes the share; the recipient saves without refreshing first.
    revoked = true
    fireEvent.click(within(editor).getByRole('button', { name: 'Mentés' }))

    await waitFor(() => expect(within(editor).getByRole('alert').textContent).toBe(NOTE_UNAVAILABLE))
    expect(await within(list).findByText('Még senki nem osztott meg veled jegyzetet.')).toBeTruthy()
    expect(content.value).toBe('mentés előtt beírt sor')
    expect((within(editor).getByRole('button', { name: 'Mentés' }) as HTMLButtonElement).disabled).toBe(true)
  })
})

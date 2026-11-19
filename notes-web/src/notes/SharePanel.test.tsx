import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { ShareInfo } from '../types'
import { SharePanel } from './SharePanel'

// Task 08: sharing by user name and role, the server's error messages (spec 5., V10), revoking (spec 7.).

interface Call {
  method: string
  url: string
  body: unknown
}

const SHARES_URL = '/api/notes/n1/shares'
let shares: ShareInfo[] = []
let calls: Call[] = []
/** The answer of the next POST; null = success (the share is added). */
let postAnswer: { status: number; body: unknown } | null = null

function json(data: unknown, status = 200): Response {
  return new Response(JSON.stringify(data), { status, headers: { 'Content-Type': 'application/json' } })
}

async function fakeFetch(input: RequestInfo | URL, init?: RequestInit): Promise<Response> {
  const url = String(input)
  const method = init?.method ?? 'GET'
  const body: unknown = typeof init?.body === 'string' ? JSON.parse(init.body) : undefined
  calls.push({ method, url, body })

  if (method === 'GET' && url === SHARES_URL) return json(shares)
  if (method === 'POST' && url === SHARES_URL) {
    if (postAnswer) return json(postAnswer.body, postAnswer.status)
    const sent = body as { userName: string; role: 'Viewer' | 'Editor' }
    const created: ShareInfo = { userId: `u-${sent.userName}`, userName: sent.userName, role: sent.role }
    shares = [...shares, created]
    return json(created, 201)
  }
  if (method === 'DELETE' && url.startsWith(`${SHARES_URL}/`)) {
    const userId = decodeURIComponent(url.slice(SHARES_URL.length + 1))
    shares = shares.filter((s) => s.userId !== userId)
    return new Response(null, { status: 204 })
  }
  return json({ detail: 'Ismeretlen kérés a tesztben.' }, 404)
}

function renderPanel() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={queryClient}>
      <SharePanel noteId="n1" noteTitle="Titkos akta" />
    </QueryClientProvider>,
  )
}

function userNameField(): HTMLInputElement {
  return screen.getByLabelText('Felhasználónév') as HTMLInputElement
}

function submitShare(userName: string, roleLabel: 'Olvasó' | 'Szerkesztő' = 'Olvasó') {
  fireEvent.change(userNameField(), { target: { value: userName } })
  const role = screen.getByLabelText('Szerep') as HTMLSelectElement
  const option = within(role).getByRole('option', { name: roleLabel }) as HTMLOptionElement
  fireEvent.change(role, { target: { value: option.value } })
  fireEvent.click(screen.getByRole('button', { name: 'Megosztás' }))
}

const posts = () => calls.filter((c) => c.method === 'POST')

beforeEach(() => {
  shares = [{ userId: 'u-anna', userName: 'anna', role: 'Viewer' }]
  calls = []
  postAnswer = null
  vi.stubGlobal('fetch', vi.fn(fakeFetch))
  // jsdom has no showModal/close on <dialog>: the open attribute is enough for these tests.
  HTMLDialogElement.prototype.showModal = function (this: HTMLDialogElement) {
    this.setAttribute('open', '')
  }
  HTMLDialogElement.prototype.close = function (this: HTMLDialogElement) {
    this.removeAttribute('open')
  }
})

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})

describe('SharePanel', () => {
  it('lists the current shares with their role', async () => {
    renderPanel()
    const list = await screen.findByRole('list', { name: 'Jelenlegi megosztások' })
    expect(within(list).getByText('anna')).toBeTruthy()
    expect(within(list).getByText('Olvasó')).toBeTruthy()
  })

  it('shares with the chosen role, clears the name and reloads the list', async () => {
    // AC5
    renderPanel()
    await screen.findByText('anna')
    submitShare('bela', 'Szerkesztő')

    const list = screen.getByRole('list', { name: 'Jelenlegi megosztások' })
    await waitFor(() => expect(within(list).getByText('bela')).toBeTruthy())
    expect(within(list).getByText('Szerkesztő')).toBeTruthy()
    expect(posts()).toHaveLength(1)
    expect(posts()[0].body).toEqual({ userName: 'bela', role: 'Editor' })
    expect(userNameField().value).toBe('')
  })

  it.each<[string, number, unknown, string]>([
    ['unknown user (404)', 404, { detail: 'Nincs ilyen nevű felhasználó.' }, 'Nincs ilyen nevű felhasználó.'],
    [
      'self (400, errors.userName)',
      400,
      {
        detail: 'A megadott adatok érvénytelenek.',
        errors: { userName: ['A jegyzetet saját magaddal nem oszthatod meg.'] },
      },
      'A jegyzetet saját magaddal nem oszthatod meg.',
    ],
    [
      'repeated share (409)',
      409,
      { detail: 'A jegyzet már meg van osztva ezzel a felhasználóval.' },
      'A jegyzet már meg van osztva ezzel a felhasználóval.',
    ],
  ])('shows the server message for %s and keeps the typed name', async (_, status, body, message) => {
    // AC5
    renderPanel()
    await screen.findByText('anna')
    postAnswer = { status, body }
    submitShare('anna')

    expect((await screen.findByRole('alert')).textContent).toBe(message)
    expect(userNameField().value).toBe('anna')
    const list = screen.getByRole('list', { name: 'Jelenlegi megosztások' })
    expect(within(list).getAllByRole('listitem')).toHaveLength(1)
  })

  it('does not send an empty user name', async () => {
    renderPanel()
    await screen.findByText('anna')
    submitShare('   ')

    expect((await screen.findByRole('alert')).textContent).toBe('A felhasználónév megadása kötelező.')
    expect(posts()).toHaveLength(0)
  })

  it('revokes a share after the confirmation, and the list follows the server', async () => {
    // AC7
    renderPanel()
    fireEvent.click(await screen.findByRole('button', { name: 'anna megosztásának visszavonása' }))

    const dialog = screen.getByRole('dialog', { name: 'Megosztás visszavonása' })
    expect(dialog.textContent).toContain('„Titkos akta”')
    fireEvent.click(within(dialog).getByRole('button', { name: 'Visszavonás' }))

    expect(await screen.findByText('Még senkivel nem osztottad meg.')).toBeTruthy()
    expect(calls.filter((c) => c.method === 'DELETE').map((c) => c.url)).toEqual([`${SHARES_URL}/u-anna`])
  })
})

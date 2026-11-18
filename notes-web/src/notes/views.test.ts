import { describe, expect, it } from 'vitest'
import { viewAfterMove, type NoteView } from './views'

// Manual step 25 of task 07 (V46): after a save changes the note's folder, the view follows the note.

describe('viewAfterMove', () => {
  const folderA: NoteView = { kind: 'folder', folderId: 'a' }

  it('switches a folder view to the new folder', () => {
    expect(viewAfterMove(folderA, 'b')).toEqual({ kind: 'folder', folderId: 'b' })
  })

  it('switches a folder view to "Mappa nélküli" when the note leaves its folder', () => {
    expect(viewAfterMove(folderA, null)).toEqual({ kind: 'unfiled' })
  })

  it('switches "Mappa nélküli" to the folder the note was put into', () => {
    expect(viewAfterMove({ kind: 'unfiled' }, 'b')).toEqual({ kind: 'folder', folderId: 'b' })
  })

  it('keeps the folder view when the folder did not change', () => {
    expect(viewAfterMove(folderA, 'a')).toEqual(folderA)
  })

  it('keeps "Mappa nélküli" for a note without a folder', () => {
    expect(viewAfterMove({ kind: 'unfiled' }, null)).toEqual({ kind: 'unfiled' })
  })

  it('does not switch away from "Összes"', () => {
    expect(viewAfterMove({ kind: 'all' }, 'b')).toEqual({ kind: 'all' })
    expect(viewAfterMove({ kind: 'all' }, null)).toEqual({ kind: 'all' })
  })
})

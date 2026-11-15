// Mirror of the backend's Notes.Api/Limits.cs (D8). The client checks are only a help for the user:
// the server validation decides (design 6).

export const TITLE_MAX = 100
export const CONTENT_MAX = 10_000
export const FOLDER_NAME_MAX = 50

// Same texts as the server (Notes.Api/Services/NoteService.cs, FolderService.cs), so the user sees one wording.
export const TITLE_REQUIRED = 'A cím megadása kötelező.'
export const TITLE_TOO_LONG = `A cím legfeljebb ${TITLE_MAX} karakter lehet.`
export const CONTENT_TOO_LONG = `A szöveg legfeljebb ${CONTENT_MAX} karakter lehet.`
export const FOLDER_NAME_REQUIRED = 'A mappa nevének megadása kötelező.'
export const FOLDER_NAME_TOO_LONG = `A mappa neve legfeljebb ${FOLDER_NAME_MAX} karakter lehet.`

/** Character count as the server counts it: UTF-16 code units (V3), title after Trim() (V2). */
export function titleLength(title: string): number {
  return title.trim().length
}

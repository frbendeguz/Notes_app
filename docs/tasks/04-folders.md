# 04. feladat: Mappák

## Cél
Mappák létrehozása és törlése, jegyzet mappába helyezése, szűrés mappa szerint.

## Előfeltétel
03 kész (jegyzet CRUD, `NoteAccessService`).

## Olvasd el először
- `docs/design.md`: 2. Adatmodell (Folder), 5. REST API (folders, notes/folder), 6. Validáció, D2, D4, V7
- `docs/spec.md`: 8. elfogadási feltétel

## Hatókör
### SZEREPEL
- `FolderService` + `FoldersController`: `GET /api/folders` (`[{id, name, noteCount}]`, név szerint rendezve),
  `POST /api/folders` (`{name}`), `DELETE /api/folders/{id}`
- Mappanév: trim után 1–50; egyediség `Text.Normalize` + `NormalizedName` alapján felhasználónként; duplikált → 409; érvénytelen → 400
- Más felhasználó mappája → 404 (azonos válasz a nem létezővel)
- `PUT /api/notes/{id}/folder` (`{folderId | null}`): csak tulajdonos (szerkesztő/olvasó 403, idegen 404); csak saját mappa
  (idegen/nem létező mappa → 404); `null` = mappa nélküli
- `POST /api/notes` kiegészítése opcionális `folderId`-val (ugyanazok a szabályok)
- `GET /api/notes` szűrők: `?folderId=<guid>` és `?unfiled=true` (egyszerre megadva → 400)
- Mappa törlése: a benne lévő jegyzetek megmaradnak, mappa nélküliek lesznek (a tulajdonosnál)
- A `NoteSummary.folderId` csak a tulajdonosnak jelenik meg

### NEM szerepel
- Mappa átnevezése (V7), mappa megosztása, almappák
- Megosztás, frontend

## Kötött döntések
D2 (normalizált egyediség, nem `NOCASE`), D4 (`SetNull`), V7. Egy jegyzet legfeljebb egy mappában lehet (nullable `FolderId`).

## Tesztek (`FoldersTests`)
- Mappanév határok `[Theory]`: 1 ✅, 50 ✅, 51 ❌, üres/szóközös ❌
- Duplikátum kis/nagybetűvel és ékezettel („Árvíztűrő” / „árvíztűrő”) → 409; azonos név másik felhasználónál ✅
- Jegyzet mappába helyezése, listázás `folderId` és `unfiled` szűrővel; mappából kivétel `null`-lal
- Egy jegyzet nem lehet két mappában (áthelyezés után csak az újban látszik)
- Nem üres mappa törlése → a jegyzetek megmaradnak, `folderId == null`
- Idegen mappa használata/törlése → 404; szerkesztő/olvasó mappába helyezési kísérlete → 403
- `folderId` és `unfiled` együtt → 400

## Kész, ha
- [ ] `dotnet build` és `dotnet test` zöld
- [ ] a korábbi tesztek (01–03) továbbra is zöldek
- [ ] `docs/progress.md` 04 kipipálva

## Commit
`Add folders and note-to-folder assignment`

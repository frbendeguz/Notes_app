# 05. feladat: Megosztás

## Cél
Jegyzet megosztása felhasználónév alapján olvasó vagy szerkesztő joggal, megosztás visszavonása,
„Velem megosztott” lista.

## Előfeltétel
04 kész.

## Olvasd el először
- `docs/design.md`: 4. Jogosultsági mátrix, 5. REST API (shares), V1, V4, V10
- `docs/spec.md`: 5., 6., 7. elfogadási feltétel

## Hatókör
### SZEREPEL
- `POST /api/notes/{id}/shares` (`{userName, role}`; `role`: `"Viewer"` | `"Editor"`): csak tulajdonos
  - a címzett keresése `Text.Normalize(userName)` alapján; nem létező felhasználó → 404 (magyar üzenet)
  - megosztás önmagaddal → 400; ismételt megosztás ugyanazzal a felhasználóval → 409, duplikáció nincs
  - érvénytelen szerep → 400
- `GET /api/notes/{id}/shares`: csak tulajdonos (`[{userId, userName, role}]`)
- `DELETE /api/notes/{id}/shares/{userId}`: csak tulajdonos; nem létező megosztás → 404; a jegyzet a tulajdonosnál megmarad
- `GET /api/notes/shared`: a velem megosztott jegyzetek (`NoteSummary`, `role`, `ownerUserName`, `folderId` NÉLKÜL), `UpdatedAt` szerint csökkenő
- Visszavonás után a címzett a következő kérésnél már nem látja és nem éri el a jegyzetet (404)

### NEM szerepel
- Szerepmódosítás (V4: csak visszavonás + újramegosztás), csoportos megosztás
- Frontend

## Kötött döntések
V1, V4, V10. A jogosultság továbbra is adatbázisból, a `NoteAccessService`-en keresztül dől el (nincs gyorsítótár).

## Tesztek (`SharingTests`)
- Megosztás Olvasóként és Szerkesztőként → a címzett a „Velem megosztott” listában látja, és olvashatja (5. feltétel)
- Nem létező felhasználó 404; önmaga 400; ismétlés 409 és a megosztások száma nem nő; érvénytelen szerep 400 (5. feltétel)
- Ugyanaz a címzett egy MÁSIK jegyzetre továbbra is megosztható (a duplikációtilalom jegyzetenkénti, nem globális)
- Szerkesztő módosíthatja a címet/szöveget (200); törlése és megosztása 403; olvasó PUT/DELETE/megosztás 403 (6. feltétel)
- Idegen felhasználó a megosztás-végpontokra és a jegyzetre 404 (6. feltétel)
- Visszavonás után a címzett GET-je 404, a „Velem megosztott” listában nincs; a tulajdonosnál megvan (7. feltétel)
- Szerkesztő a másik felhasználó (tulajdonos) mappájáról nem kap információt; a `folderId` nem szivárog a megosztott nézetbe
- Egyidejű módosítás: az utolsó mentés érvényes (két PUT egymás után)

## Kész, ha
- [ ] `dotnet build` és `dotnet test` zöld (az összes korábbi teszttel)
- [ ] `docs/progress.md` 05 kipipálva
- [ ] a backend feltételei (1–8) mind lefedettek automatizált teszttel (a hiányzót jelezd)

## Commit
`Add note sharing and shared-with-me list`

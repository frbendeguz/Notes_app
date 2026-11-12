# 03. feladat: Jegyzet CRUD és NoteAccessService

## Cél
Jegyzet létrehozása, listázása, lekérése, módosítása és törlése, a teljes jogosultsági mátrix
kikényszerítésével (a megosztások egyelőre közvetlenül az adatbázisba írt tesztadatként).

## Előfeltétel
02 kész: hitelesítés, hibakezelés, `AuthHelper`.

## Olvasd el először
- `docs/design.md`: 4. Jogosultsági mátrix, 5. REST API (notes), 6. Validáció, V1, V2, V3, V8
- `docs/spec.md`: 2., 3., 4., 6. elfogadási feltétel

## Hatókör
### SZEREPEL
- (A 02. reviewere alapján, elsőként) A tesztek felhasználónév-generálásának javítása a `.claude/rules/testing.md`
  szerint: a rövid véletlen nevek megszüntetése, hogy a tesztek ne ütközhessenek. Csak tesztkód változik.
- `NoteAccessService.GetRole(userId, noteId)` → `Owner | Editor | Viewer | None` (tulajdonos a `Note.OwnerId` alapján,
  egyébként a `NoteShare` alapján; mindig adatbázisból) + segédek, amelyek a művelethez szükséges szerepet kikényszerítik
  (idegen/nem létező → `NotFoundException`, kevés jog → `ForbiddenException`)
- `NoteService` + `NotesController`: `GET /api/notes` (saját jegyzetek, `UpdatedAt` szerint csökkenő),
  `GET /api/notes/{id}`, `POST /api/notes` (`{title, content}`), `PUT /api/notes/{id}` (`{title, content}`), `DELETE /api/notes/{id}`
- DTO-k: `NoteSummary`, `NoteDetail` (design 5.), `NoteWriteRequest`
- Validáció a design 6. szerint (cím trim + 1–100, szöveg ≤ 10 000, null szöveg → 400, üres megengedett); a validáció a DB-hozzáférés előtt
- `UpdatedAt` beállítása módosításkor (last write wins, verziózás nincs)

### NEM szerepel
- Mappák, mappába helyezés, a `folderId`/`unfiled` szűrők, a `POST` `folderId` mezője (04. feladat)
- Megosztás-végpontok és a `GET /api/notes/shared` (05. feladat)
- Frontend

## Elvárt kimenetek
`NoteAccessService`, `NoteService`, `NotesController`, `Dtos/Note*`, tesztek.

## Kötött döntések
V1 (idegen = 404, azonos válasz a nem létezővel), V2 (trimmelt cím tárolódik), V3 (`Length`), V8.
Entitást nem adunk vissza. A `role` mező a válaszban a hívó szerepe.

## Tesztek
Egységtesztek (`NoteAccessServiceTests`): szerepszámítás tulajdonosra, szerkesztőre, olvasóra, idegenre, nem létezőre.
Integrációs (`NotesTests`):
- Létrehozás → megjelenik a listában; új bejelentkezéssel (másik `HttpClient`) is ugyanaz (2. feltétel)
- A válasz `createdAt`/`updatedAt` mezője UTC-jelöléssel (`Z`) érkezik, és mentés–visszaolvasás után sem
  tolódik el (a 01-ben bevezetett UTC-konverter ellenőrzése)
- Határértékek `[Theory]`: cím 1/100 ✅, 101 ❌; szöveg 0/10 000 ✅, 10 001 ❌; elutasításnál nem jön létre jegyzet (3. feltétel)
- Üres és szóközös cím 400; sikertelen szerkesztés után a jegyzet változatlan (4. feltétel)
- A lista csak a saját jegyzeteket adja (egy másik felhasználó jegyzete nem látszik) (1. feltétel)
- Jogosultsági mátrix `[Theory]` (művelet × szerep, megosztás közvetlenül az adatbázisba szúrva): olvasó PUT/DELETE → 403 és változatlan jegyzet;
  szerkesztő PUT → 200, DELETE → 403; idegen és nem létező → 404 azonos törzzsel (6. feltétel)
- Törlés után a jegyzet 404

## Kész, ha
- [ ] `dotnet build` és `dotnet test` zöld
- [ ] jogosultsági döntés sehol nincs a controllerben vagy a `NoteService`-ben a `NoteAccessService`-en kívül
- [ ] `docs/progress.md` 03 kipipálva

## Megállj és kérdezz, ha
- a mátrix és a spec között eltérést találsz

## Commit
`Add note CRUD with central access control`

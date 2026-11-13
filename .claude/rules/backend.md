---
paths:
  - "Notes.Api/**"
---

# Backend szabályok (Notes.Api)

- Rétegek: a controller csak DTO-t fogad, service-t hív és HTTP kódot ad vissza. Üzleti logika és
  jogosultsági döntés nincs benne.
- Jogosultság: `NoteAccessService.GetRole(userId, noteId)` → `Owner | Editor | Viewer | None`, minden
  kérésnél adatbázisból. A tokenben csak a felhasználó azonosítója és neve van, szerep soha.
  Idegen és nem létező jegyzet válasza azonos (404). A listák láthatósági szűrése (saját, velem megosztott),
  a mappák tulajdon-ellenőrzése és a válaszban szereplő `role` is a `NoteAccessService`-ből jön (V19); a többi service-ben nincs
  `OwnerId`- vagy `NoteShare`-alapú szűrés.
- Módosító kérés sorrendje: validáció → jogosultság → módosítás (V18).
- Entitást soha ne adj vissza az API-ból, mindig DTO. Enum JSON-ban stringként (`JsonStringEnumConverter`).
- Hibakezelés: a service-ek kivételt dobnak (`ValidationFailedException`, `NotFoundException`,
  `ForbiddenException`, `ConflictException`), a központi `IExceptionHandler` alakítja ProblemDetails-szé.
  A `detail` magyar, felhasználónak szóló üzenet; validációnál `errors` (mezőnév → üzenetek).
- Validáció: a service végzi, az adatbázis módosítása ELŐTT. Korlátok csak a `Limits` osztályból;
  ne írj számliterált (100, 10000, 50) a kódba.
- Kis/nagybetűfüggetlen egyediség: `Text.Normalize` + normalizált oszlop. SQLite `NOCASE` tilos.
- Időbélyegek: `DateTime.UtcNow`.
- C#: nullable bekapcsolva, `async`/`await` végig, `CancellationToken` a controller-akciókban.
- Konfiguráció: titok (`Jwt:Key`) csak user-secrets vagy környezeti változó; az `appsettings.json`-ba nem.

## Migrációk
- A séma csak migrációval változik; a migrációt a `dotnet ef migrations add` generálja.
- Meglévő migrációs fájlt és a `ModelSnapshot`-ot kézzel nem szerkesztjük (hook blokkolja).
- Új migráció után nézd át a generált fájlt, és foglald össze, milyen sémaváltozást tartalmaz.

# 01. feladat: Alapok és adatréteg

## Cél
Buildelő solution, SQLite adatbázis `InitialCreate` migrációval, és sématesztek, amelyek az
adatmodell kényszereit bizonyítják.

## Előfeltétel
Nincs (ez az első feladat). A repóban csak a `docs/`, a `.claude/`, a `CLAUDE.md` és a `.gitignore` van.
A gépen telepítve: .NET SDK, Node.js. A `dotnet-ef` eszköz hiányát jelezd (a telepítése jóváhagyást kér).

## Olvasd el először
- `docs/design.md`: 1. Architektúra, 2. Adatmodell, 9. Döntések (D1–D8)
- `docs/spec.md`: 8. elfogadási feltétel (mappatörlés), 5. (megosztás duplikáció)

## Hatókör
### Ebben a feladatban SZEREPEL
- `Notes.sln`, `Notes.Api` (webapi, controllers), `Notes.Tests` (xunit) projektek, referencia a tesztből az API-ra
- Csomagok: `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Design` (API);
  `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.EntityFrameworkCore.Sqlite` (tesztek). A .NET és az EF verziója egyezzen.
- A sablon mintakódjának törlése (`WeatherForecast*`)
- Mappák: `Data/`, `Data/Entities/`, `Services/`, `Dtos/`, `Errors/`
- `Limits` és `Text.Normalize` (design 2. szakasz)
- Entitások: `User`, `Folder`, `Note`, `NoteShare`, enum `ShareRole`
- `AppDbContext` fluent konfigurációval: egyedi indexek, `OnDelete` szabályok, `Role` int-ként tárolva
- `Program.cs`: DbContext (SQLite, connection string az `appsettings.json`-ból), `AddControllers`, `MapControllers`,
  `public partial class Program { }`
- `InitialCreate` migráció és az adatbázis létrehozása
- `Notes.Tests/TestApiFactory` (in-memory SQLite, egyetlen életben tartott kapcsolat, `EnsureCreated`)
- Sématesztek (lásd lent)
- A meglévő `.gitignore` ellenőrzése (kiegészítése csak akkor, ha a sablon új generált fájlt hoz létre)

### Ebben a feladatban NEM szerepel (ne csináld meg)
- Controller, DTO, validáció, hitelesítés/JWT, hibakezelő, frontend
- Bármilyen üzleti logika vagy service

## Elvárt kimenetek
- Fájlstruktúra a design 1. szakasza szerint (a még nem szükséges mappák üresek maradhatnak, `.gitkeep`-pel)
- Migráció: `Notes.Api/Migrations/…InitialCreate*`

## Kötött döntések
D1–D8 a `docs/design.md`-ből. Különösen: `Guid` kulcsok, normalizált oszlopok az egyediséghez
(NEM `NOCASE`), `SetNull` a `Note.FolderId`-n, kaszkád törlés a megosztásokra, UTC időbélyegek.

## Tesztek (xUnit, `Notes.Tests/SchemaTests.cs`)
- Mappa törlése után a jegyzet megmarad és `FolderId == null`
- Ugyanaz a (jegyzet, felhasználó) pár kétszer → `DbUpdateException`
- Két azonos normalizált mappanév egy tulajdonosnál → kivétel (ékezetes és kis/nagybetűs változattal, pl. „Árvíztűrő” / „árvíztűrő”)
- Azonos mappanév két különböző tulajdonosnál → megengedett
- Azonos normalizált felhasználónév kétszer → kivétel
- Jegyzet törlése után a megosztásai is törlődnek
- Felhasználó törlése után a jegyzetei és mappái törlődnek

## Kész, ha
- [ ] `dotnet build` hibamentes az egész solutionre
- [ ] `dotnet test` zöld
- [ ] a migrációban látható: 3 egyedi index (`Users.NormalizedUserName`, `Folders(OwnerId, NormalizedName)`,
      `NoteShares(NoteId, UserId)`) és a `Notes.FolderId` külső kulcsa `SetNull`
- [ ] `notes.db`, `bin/`, `obj/` nincs verziókezelve
- [ ] `docs/progress.md` 01 kipipálva

## Megállj és kérdezz, ha
- a csomagverziók ütköznek a target frameworkkel
- a `dotnet-ef` nincs telepítve, vagy a verziója eltér az EF Core csomagokétól
- a design és a spec ellentmondónak tűnik

## Commit
`Add solution skeleton, data model and initial migration`

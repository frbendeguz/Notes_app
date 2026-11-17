# Szinkronizált jegyzetkezelő

Webalkalmazás szöveges jegyzetekhez: mappák, megosztás felhasználónév alapján, jegyzetenkénti
szerepkörök (Tulajdonos / Szerkesztő / Olvasó). Egyetemi házi feladat.

- Specifikáció (CSAK OLVASHATÓ): `docs/spec.md`
- Mérvadó terv: `docs/design.md` – feladatonként csak a feladatfájlban megjelölt szakaszokat olvasd el
- Feladatok: `docs/tasks/NN-*.md` – állapot és mérföldkövek: `docs/progress.md`
- MI-dokumentáció: `docs/ai/` (napló, döntési esetek) – tesztjelentés és futások: `docs/testing/`
- Változtatási igény: `docs/change-request/` (a `/change-request` paranccsal indul)
- Részletes konvenciók: `.claude/rules/` (a backend-, teszt- és frontend-szabályok a megfelelő
  mappák fájljainak megnyitásakor töltődnek be)

## Stack
- Backend: ASP.NET Core Web API (C#), EF Core, SQLite, REST – `Notes.Api/`
- Tesztek: xUnit – `Notes.Tests/`
- Frontend: React + TypeScript (Vite) – `notes-web/`

## Parancsok
- Build: `dotnet build`
- Teszt: `dotnet test`
- Új migráció: `dotnet ef migrations add <Név> --project Notes.Api`
- Adatbázis frissítése: `dotnet ef database update --project Notes.Api`
- Backend futtatása: `dotnet run --project Notes.Api`
- Frontend (a `notes-web/` mappában): `npm run dev`, `npm run build`, `npm run lint`, `npm test`

## Alapszabályok
- Controllers (vékonyak) → Services → `AppDbContext`. Jogosultság KIZÁRÓLAG a `NoteAccessService`-ben.
- Hibakódok: 400 validáció, 401 hitelesítés, 403 jogosultság, 404 nem létező VAGY idegen, 409 duplikáció.
- Titkot (JWT kulcs, jelszó) ne írj kódba vagy a repóba, és ne kérj tőlem titkos értéket a chatben:
  írd le a parancsot, amivel én állítom be.

## Nyelv
- Velem magyarul beszélj. Felhasználónak szóló szövegek (UI, API hibaüzenetek): magyar.
- Azonosítók, fájlnevek, commit üzenetek: angol.

## Munkamódszer (FONTOS)
- Egy munkamenet = egy feladat. A feladatot a `/task NN` paranccsal indítom; másik feladathoz ne nyúlj.
- A munkamenet plan módban indul: kód előtt terv, a jóváhagyásomig nincs fájlmódosítás.
- A feladat „NEM szerepel” listáját tartsd be. Hatókörön kívüli ötletet javaslatként írj le, ne valósítsd meg.
- Ha a spec vagy a design hiányos/ellentmondásos: KÉRDEZZ, ne dönts helyettem.
- Új NuGet/npm csomagot csak a feladatban felsoroltat vagy az engedélyemmel adj hozzá.
- Meglévő migrációt soha ne írj át; változásnál új migráció kell.
- A `docs/spec.md` és a `docs/design.md` védett (jogosultság és hook is blokkolja). Ha módosítás kell,
  javasold a beszámolóban.
- A feladat végén: „Kész, ha” pontok lefuttatása, MI-napló bejegyzés, `task-reviewer` subagent, őszinte
  beszámoló. Ami nem teljesül, azt ne jelöld késznek. Commitot csak a kérésemre készíts.
- A dokumentáció (napló, tesztjelentés) beadandó anyag: csak tényt írj bele, a döntéseimet ne találd ki.
- Ha ugyanazt a hibát kétszer javítom ki nálad, javasold, hogy vegyük fel ebbe a fájlba vagy a szabályokba.

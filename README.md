# Szinkronizált jegyzetkezelő

Webalkalmazás szöveges jegyzetekhez. A felhasználó regisztrál és belép, jegyzeteket hoz létre, szerkeszt és töröl,
mappákba rendezi őket, és felhasználónév alapján megosztja másokkal olvasó vagy szerkesztő joggal. A jegyzetek a
szerveren tárolódnak, így bármely eszközről, belépés után elérhetők. Jegyzetenként három szerep van: Tulajdonos,
Szerkesztő és Olvasó. Egyetemi házi feladat, MI-támogatott fejlesztéssel (lásd [MI-használat](docs/ai/mi-hasznalat.md)).

**Verzió:** v1.0 – első működő verzió (2. mérföldkő).

## Stack

| Réteg | Technológia |
|---|---|
| Backend | ASP.NET Core Web API (C#, .NET 10), EF Core 10, SQLite, REST, JWT Bearer |
| Tesztek | xUnit integrációs és egységtesztek (`WebApplicationFactory`, in-memory SQLite); a frontendhez Vitest + Testing Library |
| Frontend | React 19 + TypeScript (Vite 8), TanStack Query, React Router, Tailwind CSS v4 |

## Mappaszerkezet

```
Notes.sln
├─ Notes.Api/          backend: Controllers → Services → Data (AppDbContext), Dtos, Errors, Migrations
├─ Notes.Tests/        xUnit tesztek (TestApiFactory, AuthHelper)
├─ notes-web/          frontend (Vite + React + TypeScript), src/: api.ts, pages/, notes/, components/
├─ docs/               specifikáció, terv, feladatok, API-leírás, tesztjelentés, MI-dokumentáció
├─ .claude/            Claude Code beállítások: szabályok, hookok, skillek, subagent
├─ CLAUDE.md           az MI-agent projektutasításai
└─ dotnet-tools.json   helyi .NET eszközök (dotnet-ef)
```

## Előfeltételek

- **.NET 10 SDK.** A projektek célkerete `net10.0`. Fejlesztéskor használt SDK: 10.0.401.
- **Node.js 20.19+ vagy 22.12+.** Ez a Vite 8 követelménye (`notes-web/package-lock.json`). Fejlesztéskor használt
  verzió: 24.13.0.
- Git.

## Indítás tiszta klónból

Minden parancs a repó gyökeréből indul, ha nincs más megadva.

**1. Klónozás**
```
git clone https://github.com/frbendeguz/Notes_app.git
cd Notes_app
```

**2. Helyi .NET eszközök visszaállítása** (a `dotnet-ef` 10.0.15 a `dotnet-tools.json`-ból, globális telepítés nem kell)
```
dotnet tool restore
```

**3. JWT-kulcs beállítása** (egyszer kell). A kulcs titok, ezért nincs a repóban; user-secrets-ben tárolódik
(a felhasználói profilban, a repón kívül). Legalább 32 bájt hosszú legyen, különben a backend nem indul el.
```
dotnet user-secrets set "Jwt:Key" "<legalább 32 bájtos véletlen titok>" --project Notes.Api
```
Véletlen kulcs generálása és beállítása egy lépésben, PowerShellben:
```powershell
$b = New-Object byte[] 48; [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b)
dotnet user-secrets set "Jwt:Key" ([Convert]::ToBase64String($b)) --project Notes.Api
```
Bash-ben: `dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)" --project Notes.Api`.
User-secrets helyett a `Jwt__Key` környezeti változó is használható.

**4. Adatbázis létrehozása** (a migrációk lefuttatása, az eredmény a `Notes.Api/notes.db` SQLite-fájl)
```
dotnet ef database update --project Notes.Api
```

**5. Backend indítása** (1. terminál)
```
dotnet run --project Notes.Api --launch-profile http
```
→ `http://localhost:5098` (Development környezet; az OpenAPI-leírás a `http://localhost:5098/openapi/v1.json` címen).

**6. Frontend telepítése és indítása** (2. terminál)
```
cd notes-web
npm ci
npm run dev
```
→ Nyisd meg a `http://localhost:5173` címet a böngészőben.

A Vite fejlesztői szerver az `/api` kéréseket a backendhez továbbítja (proxy, így CORS-beállítás nem kell). A backend
címe a `notes-web/vite.config.ts`-ben van; ha a backend más porton fut, a `NOTES_API_URL` környezeti változóval
felülírható (pl. PowerShellben `$env:NOTES_API_URL="http://localhost:1234"; npm run dev`).

### Portok

| Szolgáltatás | Cím |
|---|---|
| Backend (`http` profil) | `http://localhost:5098` |
| Backend (`https` profil, nem kötelező) | `https://localhost:7126` és `http://localhost:5098` |
| Frontend (Vite dev szerver) | `http://localhost:5173` |

## Tesztek futtatása

**Backend (xUnit)**, a repó gyökerében (JWT-kulcs és adatbázis nem kell, a tesztek saját kulcsot és in-memory SQLite-ot
használnak):
```
dotnet build
dotnet test
```
Egy tesztosztály vagy egy teszt szűrése:
```
dotnet test --filter "FullyQualifiedName~SharingTests"
dotnet test --filter "FullyQualifiedName~NotesTests.CreateNote_BoundaryLengths_Returns201AndStored"
```
Részletes kimenet: `dotnet test --logger "console;verbosity=normal"`.

**Frontend**, a `notes-web/` mappában (előtte `npm ci`):
```
npm test                                   # Vitest, egyszeri futás
npm test -- src/pages/NotesPage.test.tsx   # egy tesztfájl
npm run build                              # típusellenőrzés (tsc -b) + éles build
npm run lint                               # statikus ellenőrzés (oxlint)
```

**Mentett futások és kézi tesztek**
- Mentett tesztkimenetek: [`docs/testing/runs/`](docs/testing/runs/). A teljes futás a
  [`09-teljes-futas.txt`](docs/testing/runs/09-teljes-futas.txt) fájlban van (`dotnet test` 220/220), mellette a
  `09-npm-*.txt` fájlok.
- Kézi tesztlépések eredménnyel: [`docs/testing/manual/`](docs/testing/manual/).
- Elfogadási tesztjelentés (AC1–AC8, kötelező esetek): [`docs/testing/acceptance.md`](docs/testing/acceptance.md).
- Hibafeltárás (valódi hiba: a hibás változaton elbukó, a javítotton átmenő teszt):
  [`docs/testing/hibafeltaras.md`](docs/testing/hibafeltaras.md). A hibás állapot visszaállítása a mai kódon:
  ```
  git apply docs/testing/10-valodi-hiba-visszaallitas.patch
  cd notes-web
  npm test -- src/pages/NotesPage.test.tsx    # 2 eset elbukik
  cd ..
  git checkout -- notes-web/src/pages/NotesPage.tsx
  ```

## Dokumentáció

| Dokumentum | Tartalom |
|---|---|
| [`docs/spec.md`](docs/spec.md) | Specifikáció, elfogadási feltételek (AC1–AC8) |
| [`docs/design.md`](docs/design.md) | Tervezési dokumentum: architektúra, adatmodell, jogosultság, API, döntések (D1–D8, V1–V52) |
| [`docs/api.md`](docs/api.md) | API-leírás a megvalósított kód alapján |
| [`docs/testing/acceptance.md`](docs/testing/acceptance.md) | Elfogadási tesztjelentés |
| [`docs/testing/hibafeltaras.md`](docs/testing/hibafeltaras.md) | Hibafeltárás bemutatása |
| [`docs/ai/mi-hasznalat.md`](docs/ai/mi-hasznalat.md) | MI-használat: eszköz, munkamódszer, támogató elemek, korlátok |
| [`docs/ai/ai-naplo.md`](docs/ai/ai-naplo.md) | MI-napló feladatonként |
| [`docs/ai/dontesi-esetek.md`](docs/ai/dontesi-esetek.md) | Döntési esetek |
| [`docs/progress.md`](docs/progress.md) | Haladás, mérföldkövek, nyitott kérdések |
| [`docs/tasks/`](docs/tasks/) | Az MI-agentnek kiadott feladatleírások |

## Ismert korlátok

- **Nincs valós idejű szinkron.** A kliens a szerveren tárolt adatot betöltéskor, mentés után vagy a „Frissítés”
  gombbal kéri le; egy másik eszközön történt változás csak ezután látszik (spec).
- **Egyidejű szerkesztés: az utolsó mentés érvényes** (last write wins). Nincs verziókezelés vagy ütközésjelzés, egy
  másik felhasználó közbeni mentése felülíródhat (spec, vállalt korlát).
- **Csak mappák vannak**, címkék és kategóriák nincsenek. A mappák a tulajdonoséi; a címzett a megosztott jegyzeteket
  a „Velem megosztott” listában látja. Mappát átnevezni nem lehet (V7).
- **Megosztás csak felhasználónév alapján, egyenként**; csoportos megosztás nincs. A szerep nem módosítható: vissza
  kell vonni, és újra megosztani (V4).
- **A mappák magyar ábécérendje** (`hu-HU` kultúra, V22) a gép nyelvi támogatásától függ. Windows 10/11-en ez beépített.
  Linuxon ICU kell (`libicu`); ha az alkalmazás invariáns globalizációs módban fut, a sorrend eltérhet a magyartól.
- **Munkamenet:** a token `sessionStorage`-ban van (V13). A lap bezárása után, illetve új lapon újra be kell lépni. A
  token a szerveren a lejáratáig (60 perc) érvényes marad, visszavonás és refresh token nincs; lejáratkor a kliens
  kijelentkeztet.
- **Nem célok** (design 10.): fiók törlése, jelszó-visszaállítás, belépési kísérletek korlátozása, HTTPS-konfiguráció
  az alapon túl (a HTTPS-átirányítás csak Development környezeten kívül aktív, V14), teljes frontend-tesztlefedettség
  (csak célzott regressziós tesztek vannak, V48).

# 06. feladat: Frontend váz, belépés, lista

## Cél
Futó React + TS kliens, amelyben lehet regisztrálni, belépni, kilépni, és látszik a saját és a
megosztott jegyzetek listája (egyelőre csak olvasható lista).

## Előfeltétel
05 kész: a backend minden végpontja működik és tesztelt.

## Olvasd el először
- `docs/design.md`: 3. Hitelesítés (kliens), 5. REST API, 7. Frontend, V9, V13
- `docs/spec.md`: 1. és 2. elfogadási feltétel

## Hatókör
### SZEREPEL
- Vite + React + TypeScript projekt a `notes-web/` mappában; csomagok: `react-router-dom`, `@tanstack/react-query`,
  `tailwindcss`, `@tailwindcss/vite` (V33)
- Vite dev proxy: `/api` → a backend (a port legyen egy helyen konfigurálva)
- `src/api.ts`: fetch-wrapper (Bearer token, JSON, ProblemDetails `detail` kinyerése, 401 → kijelentkeztetés)
- Típusok az API DTO-ihoz (`NoteSummary`, `NoteDetail`, `Folder`, `ShareInfo`)
- Hitelesítési állapot (context): token `sessionStorage`-ban, `GET /api/auth/me` indításkor a token ellenőrzésére
- Oldalak: `/login`, `/register` (hibaüzenet a szerver `detail`-jéből; a regisztrációnál a jelszószabályok
  előre kiírva, és a szerver `errors.password` üzenetei a mező alatt), védett `/notes` útvonal, kilépés
- `/notes`: oldalsáv váz (Összes, Mappa nélküli, „Velem megosztott”) és a jegyzetlista (cím, frissítés ideje; megosztottnál szerep és tulajdonos);
  „Frissítés” gomb
- Tailwind beállítása: a `@tailwindcss/vite` plugin a `vite.config.ts`-ben, `@import "tailwindcss";` a `src/index.css`-ben,
  a design tokenek (színek, betűtípus) egy `@theme` blokkban. A Vite sablon mintastílusainak (`App.css`, mintalogók) törlése.
- Vizuális irány: a `frontend-design` skill alapján a tervben röviden írd le a választott irányt (színek, tipográfia,
  elrendezés), és ezt hagyjam jóvá, mielőtt építesz rá. Ez az irány a 07–08. feladatban is érvényes marad.
- Magyar feliratok
- ESLint beállítás (a Vite sablon szerint)

### NEM szerepel
- Jegyzet szerkesztése/létrehozása/törlése, mappák kezelése, megosztás UI (07–08. feladat)
- Frontend automatizált tesztek

## Kötött döntések
V13 (token `sessionStorage`), V33 (Tailwind + `frontend-design`), design 7. (React Router, TanStack Query). A szerepek szerinti UI-tiltás a 08-ban jön.

## Kész, ha
- [ ] `npm run build` és `npm run lint` hibamentes
- [ ] a kézi ellenőrzés lépései megírva a `docs/testing/manual/06.md` fájlba: számozott, reprodukálható lépések
      elvárt eredménnyel (a böngészős próbát a fejlesztő végzi, az agent nem tud böngészőt kezelni). Lefedi:
      regisztráció → belépés → üres lista; hibás jelszó → hibaüzenet, nincs belépés;
      kilépés; frissítés (F5) után belépve marad; a lap bezárása és újranyitása után, illetve új lapon újra
      be kell lépni; a jelszószabályokat megsértő regisztráció hibaüzenettel; lejárt/hibás token → a `/login`-ra kerül
- [ ] `dotnet test` továbbra is zöld
- [ ] `docs/progress.md` 06 kipipálva; a fejlesztő a commit előtt végrehajtja a kézi lépéseket, és az eredményt
      a `docs/testing/manual/06.md`-be írja (hiba esetén új munkamenetben javítjuk)

## Megállj és kérdezz, ha
- a backend válasza eltér a design DTO-itól

## Commit
`Add frontend skeleton with auth and note list`

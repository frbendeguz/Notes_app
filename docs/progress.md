# Haladás

Egyszerre egy feladat, mindegyik új munkamenetben: `/clear`, majd `/task NN` (pl. `/task 01`).
Feladat végén: „Kész, ha” pontok zöldek, `task-reviewer` lefutott, MI-napló bejegyzés kész, pipa –
a commitot a fejlesztő hagyja jóvá.

## 1. mérföldkő – Specifikáció (6. hét)
- [x] Specifikáció beadva (`docs/spec.md`)
- [x] 00 – Tervezés és agent-munkakörnyezet (`ai/00-tervezes-osszefoglalo.md`)

## 2. mérföldkő – Első működő verzió (10. hét)
- [x] 01 – Alapok és adatréteg (`tasks/01-foundation.md`)
- [x] 02 – Regisztráció, belépés, JWT, hibakezelés (`tasks/02-auth.md`)
- [x] 03 – Jegyzet CRUD és `NoteAccessService` (`tasks/03-notes-crud.md`)
- [x] 04 – Mappák (`tasks/04-folders.md`)
- [x] 05 – Megosztás (`tasks/05-sharing.md`)
- [x] 06 – Frontend: váz, belépés, lista (`tasks/06-frontend-auth-and-list.md`)
- [x] 07 – Frontend: szerkesztő és mappák (`tasks/07-frontend-editor-folders.md`)
- [ ] 07b – A 07. kézi próbájában talált két hiba javítása, frontend-teszttel (`tasks/07b-editor-bugfixes.md`)
- [ ] 08 – Frontend: megosztás (`tasks/08-frontend-sharing.md`)
- [ ] 09 – Elfogadási feltételek ellenőrzése és tesztjelentés (`tasks/09-acceptance-walkthrough.md`)
- [ ] 10 – Hibafeltárás bemutatása (`tasks/10-bug-demonstration.md`)
- [ ] 11 – README, API-leírás, MI-használat (`tasks/11-readme-and-docs.md`)
- [ ] (fejlesztő) Tiszta klónból kipróbálva; `v1.0` címke létrehozva és pusholva; a repó publikus

## Változtatási igény (a 2. mérföldkő után)
- [ ] (fejlesztő) Az igény szövege a `docs/change-request/igeny.md`-ben
- [ ] `/change-request` – hatásvizsgálat, spec v2.0 és design javaslat, új feladatfájlok
- [ ] (fejlesztő) A spec és a design javaslat átvezetve a `docs/spec.md`-be és a `docs/design.md`-be
- (a `/change-request` ide veszi fel a 12-től számozott feladatokat)

## 3. mérföldkő – Végleges verzió (13. hét)
- [ ] A változtatási igény feladatai készen, regressziós és adatmegőrzési tesztekkel
- [ ] `docs/testing/acceptance.md` frissítve a v2.0 feltételekkel
- [ ] `docs/change-request/valtozasok-osszefoglalasa.md`
- [ ] (fejlesztő) Három döntési eset megírva (`docs/ai/dontesi-esetek.md`)
- [ ] README és `docs/ai/mi-hasznalat.md` frissítve
- [ ] (fejlesztő) Felkészülés a védésre: a feladatok beszámolóinak „Felkészülés a védésre” pontjai

## Megjegyzések / nyitott kérdések
(az agent és a fejlesztő ide írja, ami a feladatok közben felmerül)
- 01: a design 1. szakaszában a `Migrations/` a `Data/` alatt van, a migráció a 01. feladat szerint a
  `Notes.Api/Migrations/` alá került. Javaslat: a design fastruktúrájának pontosítása (a fejlesztő dönt).
- 01: a `dotnet-ef` lokális tool (`dotnet-tools.json` a repó gyökerében); tiszta klónban `dotnet tool restore`
  kell előtte – a README-be (11. feladat) felveendő.
- 01 → 02: a `Program.cs`-ben a `UseAuthentication()` a `UseAuthorization()` elé kerüljön.
- 01 → 05: teszt arra, hogy ugyanaz a felhasználó egy másik jegyzetre még megosztható.
- 02: belépés hiányzó/üres mezőkkel → 401 ugyanazzal az üzenettel (fejlesztői döntés), `ClockSkew = 0`, a `/me` a
  tokenből válaszol. Javaslat: a design 3./5. szakaszába felvenni (a fejlesztő dönt).
- 02: új hibatípus: `UnauthorizedException` (belépési 401). A design 5. szakaszának kivétellistája négyet sorol fel;
  pontosítás javasolt.
- 02: a tesztek a `TestApiFactory.TestJwtKey` nyilvános tesztkulcsot használják; tiszta klónban a backend futtatásához
  a fejlesztői kulcsot `dotnet user-secrets set "Jwt:Key" ...` paranccsal kell beállítani – a README-be (11. feladat).
- 02: a `Register_UserNameBoundaryLength_Returns201` 3 karakteres neve csak 256 változat; ha később más teszt is
  regisztrál 3 karakteres nevet a közös fixture-ben, ütközhet.
- 02: kézi kipróbálás (`dotnet run` + kérés a végpontokra) az agent részéről nem történt (jogosultság elutasította).
- 02 → 03: a rövid tesztnevek ütközési kockázata javítva (`AuthTests.NameOfLength`: 13 karakter alatt fix, kötőjeles
  név, amelyet csak a hossz-határteszt használ).
- 03: a `NoteSummary`/`NoteDetail` még nem tartalmaz `folderId`-t (fejlesztői döntés) – a 04. feladatban kell felvenni
  (csak a tulajdonosnak töltve, design 5.).
- 03: PUT-nál a validáció a jogosultság-ellenőrzés előtt fut (fejlesztői döntés): az olvasó vagy idegen érvénytelen
  törzsű kérése 400-at kap (nem 403/404-et). Javaslat: a design 4./6. szakaszába felvenni (a fejlesztő dönt).
- 03: a státuszkódok (POST 201 + `Location`, PUT 200, DELETE 204) a design 5.-ben nincsenek rögzítve; felvételük javasolt.
- 03: a nem GUID formájú `{id}` a route-megkötés miatt 404-et ad, de általános ProblemDetails törzzsel (nem a
  „A jegyzet nem található." üzenettel).
- 03 → 05: a „Velem megosztott” listánál a hívó szerepét a `NoteAccessService` adja (a 03-as listában a `NoteRole.Owner`
  az `OwnedNotes` jelentéséből következik; reviewer-észrevétel).
- 04: fejlesztői döntések: mappa-tulajdon a `NoteAccessService`-ben (`OwnedFolders`, `EnsureOwnFolderAsync`); `?folderId=`
  idegen/nem létező mappára 404; `unfiled=false` = nincs szűrő; mappába
  helyezés nem módosítja az `UpdatedAt`-et; mappalista magyar ábécérendben (`hu-HU`, kis/nagybetűtől független, memóriában); a `folderId` mindig a JSON-ban, nem tulajdonosnak `null`; `PUT /notes/{id}/folder`:
  előbb jegyzet-jogosultság, utána mappa; `POST /api/folders` `Location`: `/api/folders/{id}` (nincs rá GET). Javaslat: a
  design 5. szakaszába felvenni (a fejlesztő dönt).
- 04 → 11: az API-leírásba: a `PUT /api/notes/{id}` a törzs `folderId` mezőjét figyelmen kívül hagyja (áthelyezés:
  `PUT /api/notes/{id}/folder`); a két szűrő együtt (`folderId` + `unfiled=true`) 400, `errors.unfiled`.
- 04: az „Árvíztűrő” és az „Arvizturo” két különböző mappanév (a `Text.Normalize` csak kis/nagybetűt egyesít, ékezetet nem);
  a teszt ezt rögzíti – a fejlesztő megerősítheti.
- 05: fejlesztői döntések: a szerep `"Viewer"`/`"Editor"`, kis/nagybetűtől függetlenül (a DTO-ban string); hiányzó/üres
  `userName` → 400, formailag érvénytelen név → 404; POST sorrend: validáció → tulajdonos → címzett (404) → önmaga (400) →
  ismétlés (409); POST 201 + `ShareInfo` + `Location: /api/notes/{id}/shares/{userId}` (nincs rá GET); megosztások listája
  felhasználónév szerint; a megosztott nézetben `folderId: null`. Javaslat: a design 5. szakaszába felvenni (a fejlesztő dönt).
- 05: fejlesztői döntés: a megosztás-sorok lekérdezései (lista, ismétlés-ellenőrzés, visszavonás) is a `NoteAccessService`-ben
  vannak (`SharesOf`, `ShareOf`); a `ShareService` csak az új sort veszi fel. Javaslat: a design 4. szakaszába felvenni.
- 05: a számként küldött szerep (`"role": 1`) a modellkötésnél bukik el: 400, de `errors["$.role"]` az általános „A kérés
  formátuma érvénytelen.” üzenettel. A párhuzamos ismételt megosztás (egyedi index, 2067) → 409, teszt nélkül.
- 05 → 11: az API-leírásba: megosztás-végpontok, hibakódok, a `Location` fejléc GET nélkül, a `/api/notes/shared` rendezése
  (`UpdatedAt`, majd `CreatedAt` csökkenő).
- 06: fejlesztői döntések: az „Összes” csak a saját jegyzeteket mutatja; vizuális irány: régi aktadosszié (szekrényacél,
  manila, bélyegzővörös, mappafülek), betűtípus `@fontsource/courier-prime`; linter: oxlint (a jelenlegi Vite sablon nem
  ad ESLintet). Javaslat: a 06. feladatfájl „ESLint beállítás” pontjának, valamint a CLAUDE.md és a design pontosítása
  (linter: oxlint), és az irány felvétele a design 7. szakaszába (a fejlesztő dönt).
- 06: a kézi lépéseket (`docs/testing/manual/06.md`, 25 lépés) a fejlesztő hajtja végre a commit előtt; az agent
  böngészős és proxyn keresztüli próbát nem végzett (a `dotnet run` + `curl` füstpróbát a jogosultság elutasította).
- 06 → 11: a README-be: frontend indítása (`cd notes-web`, `npm install`, `npm run dev`, `http://localhost:5173`); a backend
  címe a `notes-web/vite.config.ts`-ben, a `NOTES_API_URL` környezeti változóval felülírható.
- 06 → 07: a hibaüzenet-megjelenítés (`ApiError.message`, `ApiError.errors`) és a `buttonStyles` újrahasználható; a lista
  query-kulcsai `['notes', …]`, mentés után az invalidálás ezzel a prefixszel megy.
- 07: fejlesztői döntések: a nyitott jegyzet az URL-ben (`?note=<id>` / `?note=new`, mappa nézet `?view=folder&folder=<id>`);
  új jegyzet POST-ja az első Mentéskor; a mappaváltás a Mentéshez kötött; nincs `maxLength`, határ felett a kliens nem ment;
  a „Velem megosztott” sorai nem kattinthatók (08); nem mentett módosításnál megerősítés lapon belüli navigációkor és
  a Kilépés gombnál (V45, a mappatörlésnél nincs);
  `lg`-n lista + szerkesztő egymás mellett; a nyitott mappa törlése után „Mappa nélküli”. Javaslat: a design 7. szakaszába
  felvenni (URL-paraméterek, mappaváltás a Mentéssel) – a fejlesztő dönt.
- 07: új token `--color-paper` (szerkesztőlap), új `dangerButton` stílus.
- 07: nem kérdez rá a nem mentett módosításra: a böngésző Vissza gombja, F5/lapbezárás (`beforeunload`), a mappatörlés
  (nem zárja be a szerkesztőt).
- 07: a kézi lépéseket (`docs/testing/manual/07.md`, 36 lépés) a fejlesztő hajtja végre a commit előtt; az agent böngészős
  próbát nem végzett.
- 07 → 08: a megosztott jegyzet megnyitása (`?view=shared&note=` jelenleg figyelmen kívül marad) és a szerep szerinti tiltás a
  `NoteEditor`-ban; az editor szerkesztő szerepnél a Törlést és a Mappa mezőt rejtse.
- 06 napló: a fejlesztői „Ellenőrzés (fejlesztő)” sorban csonka mondat maradt („…megjelent. is a „szerver nem érhető el”…”).

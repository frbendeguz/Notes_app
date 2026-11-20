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
- [x] 07b – A 07. kézi próbájában talált két hiba javítása, frontend-teszttel (`tasks/07b-editor-bugfixes.md`)
- [x] 08 – Frontend: megosztás (`tasks/08-frontend-sharing.md`)
- [x] 09 – Elfogadási feltételek ellenőrzése és tesztjelentés (`tasks/09-acceptance-walkthrough.md`)
- [x] 10 – Hibafeltárás bemutatása (`tasks/10-bug-demonstration.md`)
- [x] 11 – README, API-leírás, MI-használat (`tasks/11-readme-and-docs.md`)
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
  címe a `notes-web/vite.config.ts`-ben, a `NOTES_API_URL` környezeti változóval felülírható. (A 11-ben
  megvalósítva, a fejlesztő döntése szerint `npm ci`-vel.)
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
  `NoteEditor`-ban; az editor szerkesztő szerepnél a Törlést és a Mappa mezőt rejtse. (A 08-ban megvalósítva.)
- 07b: fejlesztői döntések: a `@testing-library/dom` (a `@testing-library/react` peer-függősége) kifejezetten
  fejlesztői függőség; a V46 az új jegyzetre is vonatkozik (mappa nézetben más mappába / mappa nélkül létrehozott
  jegyzet után a nézet a jegyzet helyére vált); a nézetváltás `replace` navigációval megy. Javaslat: a design 7.
  „Szerkesztő” blokkjában a V46 kiegészítése az új jegyzettel (a fejlesztő dönt).
- 07b: a `docs/testing/manual/07.md` 25. és 31. lépése újrafuttatásra vár (fejlesztő); az agent böngészős próbát nem végzett.
  Javaslat: új kézi lépés a „mappa nézetben új jegyzet más mappába” esetre (V46 új jegyzetnél).
- 07b: az `npm audit` 6 magas súlyosságú jelzést ad (`picomatch`, a vite/vitest láncon); az `npm audit fix --force` a
  vite-ot 6.x-re léptetné vissza, ezért nem futott – a fejlesztő dönt.
- 07b → 10: a valódi hiba bizonyítékai: `docs/testing/runs/07b-failed-save-keeps-draft-elbukik.txt` / `…-atmegy.txt`,
  teszt: `notes-web/src/pages/NotesPage.test.tsx`; hibás változat: `2a1bee2` (frontendkód: `4a51135`).
- 06 napló: a fejlesztői „Ellenőrzés (fejlesztő)” sorban csonka mondat maradt („…megjelent. is a „szerver nem érhető el”…”).
- 07b → 08: az `npm audit` a commitolt állapotban 0 sebezhetőséget jelez, vite 8.3.8, picomatch 4.0.7
  (`docs/testing/runs/08-npm-audit.txt`, `08-npm-ls-picomatch.txt`); a fenti 07b-s audit-megjegyzés így lezárult.
- 08: fejlesztői döntések: a megosztás-kezelő a szerkesztő lapján lévő „Megosztás” szakasz (csak tulajdonosnak, csak
  mentett jegyzetnél); a visszavonás megerősítést kér; ha a megnyitott jegyzet elérhetetlenné válik (újratöltéskor
  404/403), a szerkesztő a beírt szöveggel nyitva marad, felül üzenet, a Mentés és a Törlés tiltott, a lista frissül.
  Javaslat: a design 7. szakaszába felvenni (a fejlesztő dönt).
- 08: az olvasónál a mezők `readOnly`-k (kijelölhető, nem szerkeszthető), nincs számláló; a megosztott jegyzet URL-je
  `?view=shared&note=<id>`, ott `note=new` nem nyílik meg.
- 08: a kézi lépéseket (`docs/testing/manual/08.md`, 27 lépés, két böngészőprofil + két PowerShell-lépés) a fejlesztő
  hajtja végre a commit előtt; az agent böngészős próbát nem végzett.
- 08: ha a címzett szerepe visszavonás + újramegosztás után megváltozik (V4), a nyitott szerkesztő a következő
  újratöltéskor követi az új szerepet; erre nincs automatizált teszt.
- 09: fejlesztői döntések: regressziós esetnek az új `SharingTests.DeleteFolder_SharedNoteInFolder_RecipientKeepsAccess`
  (nem a 07b frontend-tesztje, az a 10. feladat bizonyítéka); a felületi részeknél az `acceptance.md` a már lefuttatott
  `manual/06–08.md` lépésekre hivatkozik (nem új kézi lépések); az `npm` kimenetek külön `runs/09-npm-*.txt` fájlokban.
- 09: két új teszt (`NotesTests.SameAccount_TwoIndependentClients_SeeEachOthersChanges` – AC2, és a fenti regressziós teszt);
  mindkettő elsőre átment, termékhibát nem találtak. `dotnet test` 220/220, `npm test` 23/23.
- 09: a design V1–V13 ellenőrzése – eltérés nincs, mind a leírt módon valósult meg:
  V1 (`NotesTests.NoteOperation_ForeignVsMissingNote_IdenticalNotFoundBody`), V2 (`CreateNote_TitleWithSurroundingSpaces_…`),
  V3 (`NoteService.Validate`, `FolderService`, `AuthService`: `string.Length`; a kliens `limits.ts` is `.length`), V4 (nincs
  szerepmódosító végpont, a `SharesController` csak GET/POST/DELETE; ismétlés 409), V5 és V6 (`AuthTests` határ- és
  karakterosztály-tesztjei), V7 (a `FoldersController` csak GET/POST/DELETE), V8 (`CreateNote_BoundaryLengths_…(1, 0)`),
  V9 (`appsettings.json` `Jwt:ExpiresMinutes = 60`, `Login_Token_ExpiresAfterConfiguredMinutesAndHasNoRole`; nincs refresh
  végpont), V10 (`SharingTests` 404/400/409), V11 (`Register_ValidData_Returns201WithTokenThatWorksOnMe`), V12 (`GET
  /api/auth/me`), V13 (`notes-web/src/api.ts`: csak `sessionStorage`, `localStorage` sehol; `manual/06.md/13–14`).
- 09: az AC2 „másik eszköz” felületi változatára (ugyanaz a fiók két böngészőben) nincs kézi lépés; az API-szintű teszt fedi.
  Javaslat: a változtatási igény kézi próbájába egy ilyen lépés felvétele (a fejlesztő dönt).
  lezárva: AC2-K1–K2 kézi lépés az acceptance.md-ben
- 09: a `docs/testing/manual/07.md`-nek nincs „Eredmény” összegző szakasza (a 06 és a 08 fájlnak van); az eredmények
  soronként rögzítettek. Javaslat: a fejlesztő pótolja (dátum, böngésző).
- 09: az `npm run lint` (oxlint) átirányított kimenetnél nem ír összegzősort, csak a kilépési kód (0) tanúsítja a hibamentességet.
- 10: fejlesztői döntések:
  - A) út: a 07b valódi hibája (V47);
  - a hibát a mai kódban is újra előidéztük (a hibás feltétel ideiglenes visszaállítása): `runs/10-failed-save-keeps-draft-elbukik.txt`
    / `…-atmegy.txt`;
  - reprodukálás: `git apply docs/testing/10-valodi-hiba-visszaallitas.patch`.

  Összefoglaló: `docs/testing/hibafeltaras.md`, hivatkozás az `acceptance.md` „Kötelező esetek” táblázatából.
- 10: a 09-es naplóbejegyzés „Ellenőrzés (fejlesztő)” sorában helyőrzők maradtak (`[x/220]`, `[x/23]`, `[eredmény]`) – a fejlesztő pótolja.
- 10 → 11: a README tesztelési részébe: a `hibafeltaras.md` és a reprodukálás parancsa.
- 11: fejlesztői döntések: a Node-verzió a Vite 8 követelményéből (20.19+ / 22.12+, fejlesztve 24.13.0), a frontend
  `npm ci`-vel indul; döntési eset jelöltek fázisonként egy (V13, V22, V47).
- 11: a kód eltérései a `docs/design.md` 1. és 5. szakaszától (a design nem módosult, a fejlesztő dönt):
  - design 1.: a fastruktúrából hiányzik a `SharesController` és a `ShareService` (a 05-ben készült), és a `Migrations/`
    helye is eltér (lásd a 01-es megjegyzést);
  - design 5.: nincs benne a Developmentben elérhető `GET /openapi/v1.json` (a `webapi` sablonból, napló 01);
  - design 5.: nincs benne, hogy a nem GUID `?folderId=` 400-at ad (`errors.folderId`, „A kérés formátuma érvénytelen.”);
  - design 5. hibatábla: a 405 (létező útvonal, nem támogatott metódus) és a sikertelen belépés hiányzó mezőre (V15, a
    3. szakaszban szerepel) nincs a táblában.
  Egyébként a végpontok, a DTO-k, a státuszkódok és a hibakódok egyeznek.
- 11: a `docs/claude-code-utmutato.md` pontatlanságai (a fejlesztő döntése szerint csak jelezve): a fájltáblázat egy
  nem létező `CLAUDE.local.md.example`-t említ; a `frontend-design` telepítésénél a marketplace neve
  `claude-plugins-official`, a `.claude/settings.json`-ban viszont `frontend-design@anthropic-plugin-directory` van.
- 11: a tiszta klónt az agent a HEAD-ből ellenőrizte (build, 220/220 teszt, `dotnet ef database update`, `npm ci`,
  `npm run build`); a backend és a frontend elindítása és a böngészős próba a fejlesztőre vár (lépések a README-ben).

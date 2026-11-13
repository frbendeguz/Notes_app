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
- [ ] 04 – Mappák (`tasks/04-folders.md`)
- [ ] 05 – Megosztás (`tasks/05-sharing.md`)
- [ ] 06 – Frontend: váz, belépés, lista (`tasks/06-frontend-auth-and-list.md`)
- [ ] 07 – Frontend: szerkesztő és mappák (`tasks/07-frontend-editor-folders.md`)
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

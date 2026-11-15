# MI-napló

Feladatonként egy bejegyzés. Az agent a `/task` munkafolyamat végén tölti ki a vázat; a
**Fejlesztői döntés** és az **Ellenőrzés (fejlesztő)** sort a fejlesztő írja vagy pontosítja.
Ez a napló a nyersanyaga a README MI-fejezetének és a három döntési esetnek
(`docs/ai/dontesi-esetek.md`).

Fázisok: követelményelemzés/tervezés · megvalósítás · tesztelés · változtatási igény · dokumentáció

---

## 00 – Tervezés és agent-munkakörnyezet
- **Fázis:** követelményelemzés/tervezés
- **Eszköz:** Claude (claude.ai), Claude Opus 5.5
- **Részletek:** `docs/ai/00-tervezes-osszefoglalo.md`

<!-- Új bejegyzés sablonja (az agent másolja a fájl végére):

## NN – <feladat címe> (ÉÉÉÉ-HH-NN)
- **Fázis:** megvalósítás | tesztelés | változtatási igény | dokumentáció
- **Eszköz:** Claude Code (<felület: VS Code bővítmény / CLI>), <modell>
- **Kontextus:** `docs/tasks/NN-*.md`, a design hivatkozott szakaszai, CLAUDE.md, `.claude/rules/…`
- **Terv és módosításai:** mit javasolt az agent; mit változtatott rajta a fejlesztő a jóváhagyás előtt
- **Eltérés a tervtől megvalósítás közben:** – / mi és miért
- **Blokkolt műveletek (hook/jogosultság):** – / mi volt, és mi lett helyette
- **Elbukó tesztek valódi hiba miatt:** – / teszt neve, a hiba oka, a mentett kimenet útja
- **Reviewer megállapításai:** és mi történt velük
- **Ellenőrzés:** futtatott parancsok és eredményük (pl. dotnet test: 42/42)
- **Fejlesztői döntés:** (a fejlesztő tölti ki) elfogadás / módosítás / elvetés és indok
- **Ellenőrzés (fejlesztő):** (a fejlesztő tölti ki) mit nézett át, mit futtatott le saját maga
- **Döntési eset jelölt?** igen/nem – ha igen, egy mondatban miért
-->

## 01 – Alapok és adatréteg (2026-11-12)
- **Fázis:** megvalósítás
- **Eszköz:** Claude Code (VS Code bővítmény), Claude Opus 5.5
- **Kontextus:** `docs/tasks/01-foundation.md`, design 1., 2., 9. (D1–D8), spec AC5 és AC8, CLAUDE.md,
  `.claude/rules/backend.md`, `.claude/rules/testing.md`
- **Terv és módosításai:** az agent három kérdést tett fel a jóváhagyás előtt. (1) A `dotnet-ef` nem volt
  telepítve: a fejlesztő a lokális tool manifestet választotta. (2) A `webapi` sablon hozza a
  `Microsoft.AspNetCore.OpenApi` csomagot (nincs a feladat listáján): a fejlesztő döntése, hogy maradjon.
  (3) D7: SQLite-ból visszaolvasva a `DateTime` `Kind=Unspecified`: a fejlesztő jóváhagyta az
  `AppDbContext`-be tett UTC értékkonvertert. A tervet ezekkel a válaszokkal változtatás nélkül jóváhagyta.
- **Eltérés a tervtől megvalósítás közben:**
  - A tool manifest a repó gyökerébe került (`dotnet-tools.json`), nem a tervben írt `.config/` mappába:
    a .NET 10 SDK `dotnet new tool-manifest` ide generálja.
  - A sablon `Notes.Api.http` fájlját is törölte (a `WeatherForecast` végpontot hívta).
  - Az UTC-konverter első változata írásnál `ToUniversalTime()`-ot hívott, ami az `Unspecified` értéket
    helyi időnek venné; a tesztek futtatása előtt javítva (csak `Local` értéket konvertál, a többit UTC-nek
    jelöli).
  - Verziók: EF Core, Mvc.Testing és `dotnet-ef` mind 10.0.15 (legfrissebb 10.0.x); az OpenApi a sablonból
    10.0.12; xUnit 2.9.3 a sablonból.
- **Blokkolt műveletek (hook/jogosultság):** –
- **Elbukó tesztek valódi hiba miatt:** – (minden teszt elsőre zöld volt). Ellenőrzésként az agent
  ideiglenesen `SetNull` → `Cascade`-re cserélte a `Note.FolderId` törlési szabályát: a
  `DeleteFolder_FolderHasNote_NoteRemainsWithNullFolderId` elbukott („Assert.NotNull() Failure”), tehát a
  teszt az adatbázis viselkedését méri; utána visszaállítva. Ez nem termékhiba volt, ezért nincs mentett futás.
- **Reviewer megállapításai:** blokkoló: a `progress.md`-ben a 01 nem volt kipipálva (a review után
  pótolva). Nem blokkoló: (1) a design 1. szakasza a `Migrations/`-t a `Data/` alá rajzolja, a feladat
  `Notes.Api/Migrations/`-t ír elő; a megvalósítás a feladatot követi, a design pontosítása javasolt.
  (2) A tool manifest a gyökérben van (lásd fent). (3) A `Program.cs`-ben a sablonból maradt
  `UseHttpsRedirection()`/`UseAuthorization()`: a 02. feladatnál ellenőrizni a middleware-sorrendet.
  (4) A sablon `coverlet.collector` (6.0.4), `Microsoft.NET.Test.Sdk` (17.14.1) és `xunit.runner.visualstudio`
  (3.1.4) csomagot is hozott; ezek a tesztsablon részei, nem kerültek eltávolításra. (5) Nincs teszt arra,
  hogy ugyanaz a felhasználó egy másik jegyzetre még megosztható; a 05. feladathoz javasolt.
- **Ellenőrzés:** `dotnet build`: 0 hiba, 0 figyelmeztetés; `dotnet test`: 8/8 (7 teszt, egy `[Theory]` két
  adatsorral); `dotnet ef migrations has-pending-model-changes`: nincs eltérés; a migrációban 3 egyedi index
  és `FK_Notes_Folders_FolderId` `SetNull`; `git status --ignored`: `notes.db`, `bin/`, `obj/` ignorált.
- **Fejlesztői döntés:**  
  - *dotnet-ef helyi eszközként (tool manifest):* elfogadtam. A verzió (10.0.15) a repóban rögzített és egyezik az EF Core csomagokéval; nincs szükség globális telepítésre, tiszta klón után a `dotnet tool restore` elég. A README-be a 11. feladatban bekerül.
  - *A tesztsablon csomagjai:* megtartottam őket. A `Microsoft.NET.Test.Sdk` és az `xunit.runner.visualstudio` nélkül a `dotnet test` nem találná és nem futtatná a teszteket; a `coverlet.collector` (lefedettségmérés) nem kötelező, ha a projekt végéig nem használjuk, törölhető.
  - *UTC-értékkonverter az `AppDbContext`-ben (a feladat hatókörén túli kiegészítés):* elfogadtam. Az SQLitenem tárolja a `DateTimeKind`-ot, ezért beolvasáskor `Unspecified` lenne, és a JSON-ból hiányozna az UTC-jelölés. A felelősség az adatelérési rétegé, egy helyen van, és nem érinti a sémát: a migráció nem változott (`has-pending-model-changes` szerint a modell egyezik). Ellenőrzése a 03. feladat tesztjébe került.
  - *V14 (HTTPS-átirányítás csak Development környezeten kívül):* a reviewer észrevétele alapján javasolt megoldást elfogadtam, mert a Vite proxy HTTP-n éri el a backendet; a 02. feladatban valósul meg.
- **Ellenőrzés (fejlesztő):** A teszteket magam is lefuttatam manuálisan, hogy megbizonyosodjak arról, hogy tényleg nem futnak hibára, és nem csak az agent hallucinált. A tesztek mind lefutottak (8/8 sikeres).
- **Döntési eset jelölt?** nem

## 02 – Regisztráció, belépés, JWT, hibakezelés (2026-11-12)
- **Fázis:** megvalósítás
- **Eszköz:** Claude Code (VS Code bővítmény), Claude Opus 5.5
- **Kontextus:** `docs/tasks/02-auth.md`, a design 1., 3., 5., 6., 8. szakasza és V5, V6, V9, V11, V12, V14; a spec 1.
  elfogadási feltétele; CLAUDE.md, `.claude/rules/backend.md`, `.claude/rules/testing.md`
- **Terv és módosításai:** az agent három nyitott kérdést tett fel. A fejlesztő döntései: (1) belépés hiányzó/üres
  felhasználónévvel vagy jelszóval → 401 ugyanazzal az üzenettel, mint a rossz jelszónál; belépéskor nincs
  formátumellenőrzés; (2) `ClockSkew = 0`, a token pontosan 60 percig érvényes; (3) a `/me` a token claim-jeiből válaszol,
  adatbázis-lekérdezés nélkül. A tervet ezekkel jóváhagyta.
  JWT-kulcs: a `dotnet user-secrets init` futtatását az agent megkísérelte, a fejlesztő elutasította és maga futtatta; a
  kulcsot a fejlesztő generálta és állította be. Az agent által javasolt `RandomNumberGenerator.GetBytes(48)` Windows
  PowerShell 5.1-ben nem létezik (hibát adott); helyette a `RandomNumberGenerator.Create().GetBytes($b)` változat futott.
- **Eltérés a tervtől megvalósítás közben:**
  - Új `Errors/UnauthorizedException` (a tervben négy kivétel szerepelt): a belépési 401-et a service dobja, és a központi
    kezelő alakítja ProblemDetails-szé, így a controllerben nincs döntés.
  - A regisztráció az előzetes `AnyAsync` ellenőrzés mellett a párhuzamos, azonos nevű regisztráció egyedi
    index-ütközését (`SqliteException` 19) is 409-re fordítja.
  - Az `OnForbidden` esemény is 403-as ProblemDetails-t ír (jelenleg nincs ilyen eset; előkészítés).
  - A tervezettnél több teszt: határhossz-elfogadás (3/32 karakteres név, 8/128 karakteres jelszó), trimmelt tárolás,
    belépés kis/nagybetű- és szóközfüggetlensége, hiányzó mezők belépéskor, manipulált aláírás, a token lejárata és a
    szerep hiánya, induláskori kulcshiba.
  - A JwtBearer csomag `10.0.*`-gal került be, utána a feloldott `10.0.15`-re rögzítve.
- **Blokkolt műveletek (hook/jogosultság):**
  - A `guard-bash` hook blokkolt egy olvasó parancsot, amely a védett design-fájlt olvasta, és ugyanabban a sorban
    `2>/dev/null` átirányítás volt; szétbontva, átirányítás nélkül lefutott. Ugyanez a hook blokkolta a napló heredoc-os
    (`>>`) hozzáfűzését, mert a szövegben szerepelt a védett fájl neve; a bejegyzés Edit eszközzel került be.
  - A `dotnet user-secrets init` hívást a fejlesztő elutasította (ő futtatta).
  - A backend helyi indítását és `curl`-os kipróbálását (`dotnet run` + `curl`) a jogosultsági beállítás elutasította;
    az agent kézi kipróbálást nem végzett.
- **Elbukó tesztek valódi hiba miatt:** –. Egy teszt elsőre elbukott (`Register_UserNameAndPasswordInvalid_BothFieldsInErrors`,
  `ObjectDisposedException`), de a tesztsegéd hibája okozta (ugyanazt a választ kétszer olvasta `ReadFromJsonAsync`-kel), nem
  termékhiba; a segéd azóta szövegként olvassa a választ. Mentett futás ezért nincs.
- **Reviewer megállapításai:** ítélet: „nem zárható le”, egyetlen blokkoló, adminisztratív okkal: a `progress.md`-ben a
  02 még nem volt kipipálva, és ez a napló-sor még nem volt kitöltve (mindkettő pótolva). Nem blokkolók és sorsuk:
  (1) a `SqliteErrorCode 19` minden constraint-sértést 409-re fordított → javítva `SqliteExtendedErrorCode 2067`-re
  (csak UNIQUE); (2) a hibás JSON / üres törzs ágat (`InvalidModelStateResponseFactory`) nem fedte teszt → új
  `Register_MalformedBody_Returns400ProblemDetailsAndUserNotCreated` [Theory] (2 adatsor); (3) két teszt nem ellenőrizte
  a mellékhatás hiányát → kiegészítve (nincs `token` a válaszban), és a nevük ennek megfelelően módosult; (4) a 3 karakteres
  egyedi név csak 256 változat, a közös fixture-ben később instabil lehet → nem javítva, a `progress.md`-be felvéve;
  (5) a `dotnet add package` BOM-ot tett a `Notes.Api.csproj` elejére → eltávolítva; (6) a `JwtOptions.KeyMinBytes = 32`
  a `Limits`-en kívül van → nem javítva: konfigurációs és nem tartalmi korlát, a fejlesztő dönthet róla; (7) kézi
  kipróbálás nem történt → a fejlesztőre marad.
- **Ellenőrzés:** `dotnet build`: 0 hiba, 0 figyelmeztetés; `dotnet test`: 55/55 (8 korábbi + 47 az `AuthTests`-ben, a
  reviewer utáni javításokkal);
  `git grep -i "jwt:key"`: csak kommentek és a `TestApiFactory` nyilvános tesztkulcsa (`TestJwtKey`); a fejlesztői kulcs nincs
  a repóban, az `appsettings.json`-ban nincs `Key`.
- **Fejlesztői döntés:**
  - *V15 – belépésnél a hiányzó/üres mező is 401:* a belépés minden sikertelen esete ugyanazt a választ adja, így a válaszból nem derül ki, mi volt a hiba oka, és a kliensnek egy esetet kell kezelnie.
  - *V16 – `ClockSkew = 0`:* a token pontosan a 60 perces lejáratkor érvénytelen (az alapértelmezés 5 perc ráhagyást adna), és ez tesztben egyértelműen ellenőrizhető.
  - *V17 – a `/me` a tokenből válaszol:* nincs fölösleges adatbázis-lekérdezés; mivel fióktörlés és átnevezés nincs, a token adata nem avulhat el.
  - *Ötödik kivételtípus (`UnauthorizedException`) és a 403-as ProblemDetails:* elfogadtam, mert így a controllerben nincs hibakezelési döntés, és minden hibaválasz egységes formátumú.
  - *`JwtOptions.KeyMinBytes` a `Limits`-en kívül:* elfogadtam; konfigurációs biztonsági korlát, nem tartalmi.
  - *Rövid tesztnevek ütközési kockázata:* a 03. feladat elején javíttatom, és bekerült a tesztelési szabályok közé.
- **Ellenőrzés (fejlesztő):** a `dotnet test` saját futtatása 55/55 sikeres tesztet adott. A `.claude/logs/instructions.log` alapján a `.claude/rules/backend.md` a `Notes.Api` fájljainak beolvasásakor betöltődött (`path_glob_match`), tehát az útvonalhoz kötött szabályok működnek. Kézzel, futó backenden (PowerShell, `Invoke-RestMethod`) kipróbáltam a regisztrációt (201, token), a `/api/auth/me` hívást a kapott tokennel (200, a felhasználó adatai) és a rossz jelszavas belépést (401, ProblemDetails magyar hibaüzenettel).
- **Döntési eset jelölt?** (a fejlesztő dönt) – lehetséges jelölt: belépés hiányzó mezőire 401 a 400 helyett.

## 03 – Jegyzet CRUD és NoteAccessService (2026-11-13)
- **Fázis:** megvalósítás
- **Eszköz:** Claude Code (VS Code bővítmény), Claude Opus 5.5
- **Kontextus:** `docs/tasks/03-notes-crud.md`, a design 2., 4., 5., 6. szakasza és V1, V2, V3, V8; a spec 2., 3., 4., 6.
  elfogadási feltétele; CLAUDE.md, `.claude/rules/backend.md`, `.claude/rules/testing.md`.
- **Terv és módosításai:** az agent terve: tesztnév-javítás elsőként; `NoteRole`, `NoteOperation`, `NoteAccessService`
  (`GetRoleAsync`, `EnsureCanAsync`, a mátrix adatként), `NoteService`, `NotesController`, `NoteDtos`,
  `JsonStringEnumConverter` a `Program.cs`-ben; a saját jegyzetek listájának `OwnerId`-szűrése a `NoteService`-ben.
  Három nyitott kérdés: (1) `folderId` a válasz-DTO-ban már most; (2) státuszkódok: POST 201 + `Location`, PUT 200,
  DELETE 204; (3) PUT-nál a validáció a jogosultság-ellenőrzés előtt fut (olvasó/idegen érvénytelen törzzsel 400-at kap).
  A fejlesztő módosításai: a listaszűrés is a `NoteAccessService`-be kerüljön (minden jogosultságkezelés ott) → 
  `OwnedNotes(userId)`; (1) a `folderId` a 04. feladatig kimarad; (2) a státuszkódok elfogadva; (3) a validáció marad elöl.
- **Eltérés a tervtől megvalósítás közben:** –. Apróság: a rövid tesztnevekhez tervezett fix név a 3 karakteres
  esetben `"b-x"` (a 2 karakteres, elutasított esetben `"b-"`); 13 karaktertől a név továbbra is GUID-alapú.
  A tervezettnél néhány teszttel több készült: `GetRole_ShareRevoked_ReturnsNoneOnNextCall`,
  `OwnedNotes_OwnerWithSharedAndForeignNotes_ReturnsOnlyOwned`, `GetNote_ShareRemovedFromDatabase_Returns404OnNextRequest`,
  `NotesEndpoints_NoToken_Return401`, valamint a törlési teszt a megosztások kaszkádos törlését is ellenőrzi.
- **Blokkolt műveletek (hook/jogosultság):** –
- **Elbukó tesztek valódi hiba miatt:** – (az új tesztek elsőre átmentek; mentett futás nincs).
- **Reviewer megállapításai:** ítélet: „nem zárható le”, egyetlen blokkoló: a naplóban a teszteloszlás hibás volt
  („21 + 44” a valós 22 + 43 helyett) → javítva. Nem blokkolók és sorsuk: (1) a lista szerepe beégetett `NoteRole.Owner`
  a `NoteService`-ben → nem javítva: az `OwnedNotes` (a `NoteAccessService`-ben) definíció szerint a tulajdonos jegyzeteit
  adja; a 05. „Velem megosztott” listájánál a szerepet a `NoteAccessService`-ből kell venni (a `progress.md`-be felvéve);
  (2) az `UpdatedAt > created.UpdatedAt` elméletben azonos óraütésnél elbukhat → nem javítva, a kockázat elméleti;
  (3) a sorrendteszt nem ellenőrizte a PUT státuszát → kiegészítve; (4) a „validáció a jogosultság előtt” döntést
  nem rögzítette teszt → új `UpdateNote_ViewerWithInvalidBody_Returns400AndNoteUnchanged`; (5) üres fejlesztői
  sorok → a fejlesztő tölti ki; (6) nincs `docs/testing/runs/` → rendben, nem volt valódi hibából elbukó teszt.
- **Ellenőrzés:** `dotnet build --no-incremental`: 0 hiba, 0 figyelmeztetés; `dotnet test`: 121/121 (55 korábbi +
  22 `NoteAccessServiceTests` + 44 `NotesTests`, a reviewer utáni kiegészítéssel; a reviewer előtt 120/120, ebből
  43 `NotesTests`); `grep` a `NotesController`-ben és a `NoteService`-ben: nincs
  `NoteShare`-, szerep- vagy `OwnerId`-alapú hozzáférési döntés (az `OwnerId = userId` a létrehozáskor a tulajdonost
  állítja be). Kézi kipróbálás futó backenden nem történt.
- **Fejlesztői döntés:** 
  - *Jogosultsági és láthatósági szűrés:* a terv a saját jegyzetek listájának `OwnerId` szerinti szűrését a
    `NoteService`-ben hagyta volna. Ezt módosítottam: a design szerint minden jogosultsági döntés a `NoteAccessService`-ben van, ezért a lista szűrése is oda került (`OwnedNotes`). Így a 05. feladat „Velem megosztott” listája is ugyanerre a helyre épül, és nem alakul ki párhuzamos jogosultsági logika (V19).
  - *`folderId` a válasz-DTO-kban:* kimarad a 04. feladatig, mert a mappák ott készülnek el; most csak mindig `null` értéket adna vissza.
  - *Státuszkódok:* elfogadtam a javaslatot (`POST` → 201 + `Location` + `NoteDetail`, `PUT` → 200 + `NoteDetail`, `DELETE` → 204), mert ez a szokásos REST-konvenció, és a kliens a létrehozott jegyzetet újabb lekérés nélkül megkapja.
  - *Validáció a jogosultság-ellenőrzés előtt:* elfogadtam (V18). Érvénytelen törzsre az olvasó, az idegen és a nem létező jegyzet is 400-at kap; ez nem árul el semmit a jegyzet létezéséről, mert minden esetben azonos, és a spec 6. feltételével sem ütközik, mert ott a kérés érvényes. A döntést a `UpdateNote_ViewerWithInvalidBody_Returns400AndNoteUnchanged` teszt rögzíti.
  - *Az agent saját döntései* (általános 404 nem GUID azonosítóra, a hibaüzenetek szövege): elfogadtam.
- **Ellenőrzés (fejlesztő):** a `dotnet test` saját futtatása 121/121 sikeres tesztet adott.
- **Döntési eset jelölt?** (a fejlesztő dönt) – lehetséges jelölt: a validáció a jogosultság-ellenőrzés előtt fut
  (az olvasó érvénytelen PUT-ja 400-at kap 403 helyett).

## 04 – Mappák (2026-11-13)
- **Fázis:** megvalósítás
- **Eszköz:** Claude Code (VS Code bővítmény), Claude Opus 5.5
- **Kontextus:** `docs/tasks/04-folders.md`, a design 2., 4., 5., 6. szakasza, D2, D4, V7 (és V1, V18, V19); a spec 8.
  elfogadási feltétele; CLAUDE.md, `.claude/rules/backend.md`, `.claude/rules/testing.md`.
- **Terv és módosításai:** az agent terve: `FolderService` + `FoldersController`, `FolderDtos`; `NoteOperation.MoveToFolder`;
  a `NoteAccessService` bővítése; `NoteService` szűrők, `folderId` a létrehozásnál, `MoveToFolderAsync`; `folderId` a
  `NoteSummary`/`NoteDetail`-ben; nincs új csomag és migráció. Hét nyitott kérdés, a fejlesztő döntései:
  (1) a mappa-tulajdon ellenőrzése a `NoteAccessService`-ben (`OwnedFolders`, `EnsureOwnFolderAsync`) – a javaslat szerint;
  (2) `?folderId=` idegen/nem létező mappára 404 – a javaslat szerint; (3) `unfiled=false` = nincs szűrő – a javaslat
  szerint; (4) mappalista rendezése: egyszerű `OrderBy(Name)` az adatbázisban – az agent `hu-HU` kultúrás memóriabeli
  rendezést javasolt, a fejlesztő az egyszerűbbet választotta (a reviewer után módosítva, lásd lent); (5) mappába helyezés nem módosítja az `UpdatedAt`-et –
  a javaslat szerint; (6) a `folderId` mező mindig ott van a JSON-ban, nem tulajdonosnak `null` – a javaslat szerint;
  (7) a `PUT /notes/{id}/folder`-nél előbb a jegyzet jogosultsága, utána a mappa – a javaslat szerint.
- **Eltérés a tervtől megvalósítás közben:** megvalósítás közben felmerült kérdés: a `POST /api/folders` `Location`
  fejlécének nincs létező GET végpontja; a fejlesztő döntése: `/api/folders/{id}` (az agent javaslata). További
  megvalósítási részletek: a `NoteWriteRequest` kapott egy opcionális `FolderId` mezőt (alapértéke `null`), amelyet a
  `PUT /api/notes/{id}` figyelmen kívül hagy; a mappanév-ütközést a `FolderService` az ellenőrzés után az egyedi index
  megsértésekor (`SqliteExtendedErrorCode` 2067) is 409-re fordítja (versenyhelyzet), erre nincs teszt; a mappa törlése
  egy `ExecuteDeleteAsync` hívás az `OwnedFolders` szűrésen, 0 törölt sor → 404; a jegyzetek mappa nélkülivé tétele az
  adatbázis `ON DELETE SET NULL` szabályán múlik. A két szűrő együtt adott 400-as hibája az `errors.unfiled` mezőn jelenik meg.
  A tervhez képest néhány teszttel több készült (pl. trim + max hossz, ékezet nélküli név nem duplikátum, `unfiled=false`,
  nem GUID `folderId` → 400, token nélkül 401, `UpdatedAt` változatlan).
- **Blokkolt műveletek (hook/jogosultság):** –
- **Elbukó tesztek valódi hiba miatt:** – (az első futáskor fordítási hiba volt a tesztben – típus nélküli collection
  expression `.Order()` hívással –, javítás után minden teszt elsőre átment; mentett futás nincs).
- **Reviewer megállapításai:** ítélet: „lezárható”, blokkoló hiba nincs (nyitott pont csak a `progress.md` pipája és a
  napló kitöltése). Nem blokkolók és sorsuk: (1) a mappa ellenőrzése és a mentés között párhuzamosan törölt mappa
  FK-hibát (500) okozna a `CreateAsync`/`MoveToFolderAsync`-ban → javítva: `SaveWithFolderAsync` a 787-es SQLite
  FK-hibát 404-re fordítja (teszt nincs rá, a versenyhelyzet tesztből nehezen idézhető elő); (2) a `PUT /api/notes/{id}`
  csendben figyelmen kívül hagyja a `folderId`-t → nem javítva, a 11. feladat API-leírásába felveendő (`progress.md`);
  (3) az `OrderBy(Name)` SQLite BINARY rendezése miatt a kisbetűs és ékezetes kezdetű nevek a „Z” után jönnek →
  a fejlesztő a beszámoló után módosította a döntését: a lista magyar ábécérend szerint, kis/nagybetűtől függetlenül
  rendezett (`StringComparer.Create(hu-HU, ignoreCase)`, memóriában a lekérdezés után) → megvalósítva, új teszt:
  `ListFolders_MixedCaseAndAccentedNames_HungarianAlphabeticalOrder` (pl. „Ábel”, „alma” az A-knál, „cs” a „c” után); (4) a nem GUID `folderId` teszt csak
  a státuszt nézte → kiegészítve (ProblemDetails `errors.folderId` és üzenet); (5) a teszt rögzíti, hogy az „Árvíztűrő”
  és az „Arvizturo” különböző név → nem módosítva, ez a `Text.Normalize` (D2) következménye, a fejlesztő megerősítheti;
  (6) a 2067 → 409 ágra nincs teszt → nem javítva, a naplóban jelezve.
- **Ellenőrzés:** `dotnet build`: 0 hiba, 0 figyelmeztetés; `dotnet test`: 161/161 (121 korábbi + 5 új `MoveToFolder`
  sor a `NoteAccessServiceTests`-ben + 35 `FoldersTests`), a reviewer utáni javítások után is 161/161; a magyar
  rendezés után 162/162 (36 `FoldersTests`).
  Kézi kipróbálás futó backenden nem történt.
- **Fejlesztői döntés:**
  - *Mappa-tulajdon ellenőrzése:* a `NoteAccessService`-be került (`OwnedFolders`, `EnsureOwnFolderAsync`), nem a `FolderService`-be, hogy minden jogosultsági döntés egy helyen maradjon (V19).
  - *Idegen vagy nem létező mappa szűrőként:* 404, nem üres lista, egységesen a „más felhasználó mappája → 404” szabállyal (V20). Az üres lista nem különböztetné meg a hibás azonosítót egy valóban üres mappától.
  - *`unfiled=false`:* ugyanaz, mint a szűrő hiánya; 400 csak a `folderId` + `unfiled=true` ellentmondó párosra (V21).
  - *Mappalista rendezése:* először az egyszerű, adatbázisbeli `OrderBy(Name)`-et választottam. A reviewer jelezte, hogy így a kisbetűs és az ékezetes kezdőbetűs nevek a „Z” után kerülnek, ami magyar felhasználónak hibásnak tűnik. Ezért módosítottam: memóriában, `hu-HU` kultúrával, kis- és nagybetűtől függetlenül rendez. A teljesítmény nem gond, mert egy felhasználónak kevés mappája van. Vállalt következmény: a sorrend a gép magyar nyelvi támogatásától függ (V22). Bizonyíték: `ListFolders_MixedCaseAndAccentedNames_HungarianAlphabeticalOrder`, amely a régi rendezéssel elbukna.
  - *`UpdatedAt` mappába helyezéskor:* nem frissül, mert a jegyzet tartalma nem változik, és így a lista sorrendje sem ugrik (V23).
  - *`folderId` a nem tulajdonosnak:* a mező mindig jelen van, de `null`; így a kliens egyféle válaszformát kezel, és a tulajdonos mappaszerkezetéről semmi nem szivárog ki (V24).
  - *Ellenőrzési sorrend az áthelyezésnél:* előbb a jegyzet jogosultsága, utána a mappa; így a szerkesztő idegen mappára is 403-at kap, és nem tudja meg, létezik-e a mappa (V25).
  - *`Location` fejléc:* `/api/folders/{id}`, bár `GET` végpont nincs rá; az azonosító így is egyértelműen jelöli a létrehozott mappát, az eltérés az API-leírásban dokumentált (V26).
  - *Ékezet a mappanév egyediségében:* az „Árvíztűrő” és az „Arvizturo” két külön név maradt (V27). A spec csak kis- és nagybetű-függetlenséget ír elő, a magyarban az ékezet jelentést hordoz (pl. „Kor” és „Kör”), és az ékezetek összevonása a normalizálás megváltoztatásával adatmigrációt is igényelne.
- **Ellenőrzés (fejlesztő):** a `dotnet test` saját futtatása 162/162 sikeres tesztet adott.
- **Döntési eset jelölt?** igen – a mappalista rendezése: a saját első döntésemet a reviewer észrevétele alapján módosítottam, és az új viselkedést egy teszt bizonyítja, amely a régi változaton elbukna.

## 05 – Megosztás (2026-11-13)
- **Fázis:** megvalósítás
- **Eszköz:** Claude Code (VS Code bővítmény), Claude Opus 5.5
- **Kontextus:** `docs/tasks/05-sharing.md`, a design 2., 4., 5., 6. szakasza, V1, V4, V10, V18, V19, V24; a spec 5–7.
  elfogadási feltétele; CLAUDE.md, `.claude/rules/backend.md`, `.claude/rules/testing.md`.
- **Terv és módosításai:** az agent terve: `NoteOperation.Share` a mátrixban; `NoteAccessService.SharedNotes` (a „Velem
  megosztott” lekérdezés a szereppel); `NoteService.ListSharedAsync` + `GET /api/notes/shared`; új `ShareService`,
  `SharesController`, `ShareDtos`; nincs új csomag és migráció. Hét nyitott kérdés, a fejlesztő döntései:
  (1) szerep: string mező, `"Viewer"`/`"Editor"` – az agent kis/nagybetűre érzékeny egyezést javasolt, a fejlesztő
  döntése: kis/nagybetűtől független (a `"viewer"` is elfogadott); (2) hiányzó/üres `userName` → 400, formailag érvénytelen
  név → 404 – a javaslat szerint; (3) sorrend a POST-nál: validáció → tulajdonos-ellenőrzés → címzett (404) → önmaga (400)
  → ismétlés (409) – a javaslat szerint; (4) POST: 201 + `ShareInfo` + `Location: /api/notes/{id}/shares/{userId}` (GET
  nincs rá) – a javaslat szerint; (5) a megosztások listája felhasználónév szerint, `hu-HU` nélkül – a javaslat szerint;
  (6) a megosztott nézetben a `folderId` mező jelen van, `null` értékkel (V24) – a javaslat szerint; (7) a `NoteShare`-sorok
  kezelése (egy jegyzet megosztásainak listázása, ismétlés-ellenőrzés, törlés) a `ShareService`-ben a sikeres
  `EnsureCanAsync(Share)` után, a láthatósági lekérdezés a `NoteAccessService`-ben – erre a kérdésre a jóváhagyáskor nem
  érkezett külön válasz, az agent először a terv javaslatát követte; a beszámoló után a fejlesztő döntése: a lekérdezések
  költözzenek a `NoteAccessService`-be (lásd az eltéréseknél).
- **Eltérés a tervtől megvalósítás közben:** a beszámoló után a fejlesztő a 7. kérdésben a szigorú változatot választotta:
  a `ShareService` `NoteShare`-lekérdezései (megosztások listája, ismétlés-ellenőrzés, visszavonás) a `NoteAccessService`
  új `SharesOf(noteId)` és `ShareOf(noteId, recipientId)` metódusain keresztül futnak; a `ShareService`-ben csak az új sor
  felvétele (`db.NoteShares.Add`) maradt. Új egységteszt: `SharesOfAndShareOf_NoteWithTwoRecipients_ReturnOnlyThatNotesShares`.
  A szerep-ellenőrzés beágyazott feltételes kifejezése (Sonar S3358) a `ParseRole` segédmetódusba került. A tervhez képest néhány teszttel több készült: a nem tulajdonos ismeretlen
  felhasználónévvel is 403/404-et kap (nem a címzett 404-ét), olvasó érvénytelen szereppel 400 (V18), számként küldött szerep
  400, a második címzett a visszavonás után is hozzáfér, visszavonás + újramegosztás más szereppel (V4), token nélkül 401 a
  megosztás-végpontokon, a szerkesztő mentése nem veszi ki a jegyzetet a tulajdonos mappájából, valamint egy
  `SharedNotes` egységteszt a `NoteAccessServiceTests`-ben. A párhuzamos ismételt megosztást a `ShareService` az egyedi
  index megsértésekor (SQLite 2067) is 409-re fordítja; erre nincs teszt. A számként küldött szerep (`"role": 1`) a
  modellkötésnél bukik el, ezért az általános „A kérés formátuma érvénytelen.” 400-at adja, nem az `errors.role` üzenetet.
- **Blokkolt műveletek (hook/jogosultság):** – (egy Bash heredoc-os fájlírás szintaktikai hiba miatt nem futott le, a
  fájlok utána a Write/Edit eszközzel készültek; ez nem blokkolás volt).
- **Elbukó tesztek valódi hiba miatt:** – (minden teszt elsőre átment; egy xUnit2013 analizátor-figyelmeztetés javítva).
- **Reviewer megállapításai:** ítélet: „lezárható”, blokkoló hiba és hatókörön kívüli változás nincs. Nem blokkolók és
  sorsuk: (1) a `progress.md` pipája hiányzott → a lezáráskor bejelölve; (2) a napló reviewer-sora helykitöltő volt → kitöltve,
  a fejlesztői sorok üresen maradtak; (3) a `.claude/rules/backend.md` szerint a többi service-ben nincs `NoteShare`-alapú
  szűrés, a `ShareService` viszont három helyen (megosztások listája, ismétlés-ellenőrzés, visszavonás) `NoteShares`-lekérdezést
  végez, mindig a sikeres `EnsureCanAsync(Share)` után → a fejlesztő döntése alapján javítva: a lekérdezések a
  `NoteAccessService`-be költöztek (`SharesOf`, `ShareOf`); (4) tesztminőség: a `ShareNote_ViewerWithInvalidRole_Returns400` és a
  `ShareNote_NotOwnerWithUnknownUser_GetsAccessErrorNotRecipientError` nem ellenőrizte a mellékhatás hiányát, a
  `ShareNote_NumericRole_Returns400AndNoShareCreated` csak a státuszt nézte → javítva (megosztásszám-ellenőrzés; ProblemDetails
  `errors["$.role"]` „A kérés formátuma érvénytelen.” üzenettel); (5) a 2067 → 409 ágra nincs teszt → nem javítva, a
  versenyhelyzet tesztből nehezen idézhető elő; (6) a „Velem megosztott” lista másodlagos rendezése (`CreatedAt` csökkenő)
  a designban nincs rögzítve → nem módosítva, a saját lista (03) ugyanígy rendez; a design pontosítása javasolt.
- **Ellenőrzés:** `dotnet build`: 0 hiba, 0 figyelmeztetés; `dotnet test`: 217/217 (162 korábbi + 5 új `Share` sor és
  1 `SharedNotes` teszt a `NoteAccessServiceTests`-ben + 49 `SharingTests`); a reviewer utáni tesztjavítások után is 217/217; a lekérdezések átköltöztetése után 218/218 (+1 `NoteAccessServiceTests`),
  a build 0 figyelmeztetéssel. Az 1–8. elfogadási feltétel mindegyikéhez van
  `// ACn` jelölésű automatizált teszt. Kézi kipróbálás futó backenden nem történt.
- **Fejlesztői döntés:**
  - *Szerep formátuma:* az MI pontos, kis- és nagybetűre érzékeny egyezést javasolt; módosítottam, a `"viewer"` is elfogadott (V28). A szerep jelentése nem függ az írásmódtól, és így a kliens hibalehetősége is kisebb.
  - *Formailag érvénytelen címzett-név:* 404, mert ilyen felhasználó nem létezhet; külön formai ellenőrzés csak egy újabb hibaágat jelentene azonos eredménnyel (V29).
  - *Ellenőrzési sorrend a megosztásnál:* elfogadtam a javaslatot (validáció → tulajdonos → címzett → önmaga → ismétlés). Így a szerkesztő és az olvasó mindig 403-at kap, és a válaszokból nem derül ki, létezik-e egy felhasználónév (V30).
  - *`Location` fejléc `GET` végpont nélkül:* elfogadtam, a mappáknál hozott V26-tal egységesen (V31).
  - *Megosztások rendezése:* az adatbázisban, a normalizált felhasználónév szerint; `hu-HU` rendezés nem kell, mert a felhasználónév csak ASCII karaktert tartalmazhat (V32).
  - *`folderId` a megosztott nézetben:* a mező jelen van, mindig `null` értékkel, hogy a kliens egyféle választ kezeljen (V24).
  - *Megosztás-sorok lekérdezése:* a javasolt enyhébb változat helyett a szigorúbbat választottam: minden `NoteShare`-lekérdezés a `NoteAccessService`-be került (`SharesOf`, `ShareOf`), a `ShareService` csak ír (V19). Indok: a jogosultsági logika egy helyen maradjon, és a szabály ne igényeljen kivételt.
- **Ellenőrzés (fejlesztő):** a `dotnet test` saját futtatása 218/218 sikeres tesztet adott.
- **Döntési eset jelölt?** (a fejlesztő dönt) – lehetséges jelölt: az ellenőrzési sorrend a megosztásnál (a nem tulajdonos
  nem tudhatja meg, létezik-e egy felhasználónév).

## 06 – Frontend: váz, belépés, lista (2026-11-13)
- **Fázis:** megvalósítás
- **Eszköz:** Claude Code (VS Code bővítmény), Claude Opus 5.5
- **Kontextus:** `docs/tasks/06-frontend-auth-and-list.md`, design 3., 5., 7. szakasz, V9, V13, V33; spec 1–2. elfogadási
  feltétel; CLAUDE.md, `.claude/rules/frontend.md`; `frontend-design` skill
- **Terv és módosításai:** az agent a terv mellé két kérdést (betűtípus, az „Összes” jelentése) és egy vizuális irányt
  („vonalas füzet”: hideg fehér papír, töltőtollkék, piros margóvonal, Literata serif) adott. A fejlesztő döntései:
  (1) az „Összes” csak a saját jegyzeteket mutatja (`GET /api/notes`), a megosztottak a „Velem megosztott” nézetben;
  (2) a vizuális irányt elvetette, helyette régi aktadossziét kért Consolas-szerű betűvel. Az agent erre új irányt adott
  (szekrényacél háttér, manila lap, bélyegzővörös szerep-bélyegzők, mappafülek oldalsávként, egyetlen monospace család),
  valódi hivatali jelvény nélkül; (3) a betűtípus a `@fontsource/courier-prime` npm csomag (a fejlesztő „első (javasolt)
  opció a courier-prime” megfogalmazással választotta; az agent a név szerint megnevezett csomagot követte, a javasolt
  1. opció a rendszerbetű volt). A tervet így jóváhagyta.
- **Eltérés a tervtől megvalósítás közben:** (1) a Vite sablon jelenlegi változata (create-vite 9.3.4, Vite 8, TypeScript 7,
  React 19) ESLint helyett oxlintet ad (`"lint": "oxlint"`, `.oxlintrc.json`). Az agent megállt és kérdezett; a fejlesztő
  döntése: az oxlint marad (új csomag nélkül). A feladatfájl „ESLint beállítás” pontja így nem szó szerint teljesül.
  (2) A sablon `tsconfig.app.json`-ja nem tartalmazott `"strict": true` sort; a frontend-szabály miatt az agent felvette.
  (3) A `useAuth` hook, a context és a provider három fájlba került (`authContext.ts`, `useAuth.ts`, `AuthProvider.tsx`)
  a `react/only-export-components` lint-szabály miatt. (4) A tervben szereplő `Sidebar.tsx` neve `notes/FolderTabs.tsx`
  lett, a nézetek definíciója a `notes/views.ts`-be, a gombstílusok a `components/buttonStyles.ts`-be kerültek.
  (5) A tervhez képest többlet: „unreachable” állapot (ha a `/me` ellenőrzés nem kap választ, a token megmarad,
  „Újrapróbálás” gomb), a 401 miatti kijelentkezés után a belépőoldal üzenete („A munkamenet lejárt vagy érvénytelen.”),
  `refetchOnWindowFocus: false` (design 7: szinkron csak betöltéskor, mentés után és gombbal).
- **Blokkolt műveletek (hook/jogosultság):** (1) két Bash-parancsot, amelyek szövegében szerepelt a védett design-fájl
  neve (egy olvasó grep és ennek a naplóbejegyzésnek a heredoc-os hozzáfűzése), a védett fájlok hookja blokkolt; helyette
  Grep/Read, illetve Edit eszköz. (2) A kézi füstpróba (backend `dotnet run` a háttérben + Vite dev szerver + `curl` a
  proxyn át) a jogosultsági beállítás miatt nem futott le; a háttérben elindított backendet az agent leállította. Böngészős
  és proxyn keresztüli kipróbálás az agent részéről nem történt.
- **Elbukó tesztek valódi hiba miatt:** – (frontend automatizált teszt nincs, NEM szerepel a feladatban).
- **Reviewer megállapításai:** ítélet: „lezárható”, blokkoló hiba és hatókörön kívüli változás nincs (a reviewer saját
  futása: build sikeres, lint 0 jelzés, `dotnet test` 218/218). Nem blokkolók és sorsuk: (1) a Vite sablon angol
  `notes-web/README.md`-je megmaradt → törölve; (2) leállt backend mellett a Vite proxy (a reviewer szerint, nem futtatva)
  üres törzsű 500-at ad, így a „szerver nem érhető el” üzenet nem jelenne meg → javítva: az `api.ts` a törzs nélküli 5xx-et
  hálózati hibaként kezeli (`toApiError`), és a kézi lépések kiegészültek a 24–25. lépéssel (leállított backend); a proxy
  viselkedését az agent sem futtatta le; (3) a 22. lépés (375 px) elvárt eredménye nem említette a fülsor saját vízszintes
  görgetését → pontosítva; (4) a napló fejlesztői sorai üresek → szándékosan, a fejlesztő tölti ki; (5) a
  `docs/testing/runs/` nem kell → egyetértés, nem készült. A javítás közben egy Sonar-jelzés (S3776, kognitív komplexitás
  16/15 az `apiFetch`-ben) miatt a hibaválasz-feldolgozás a `toApiError` függvénybe került.
- **Ellenőrzés:** `npm run build`: sikeres (tsc + vite build); `npm run lint` (oxlint): 0 figyelmeztetés, 0 hiba – az
  agent egy ideiglenes, szándékosan hibás fájllal ellenőrizte, hogy az oxlint valóban vizsgálja a `src`-t (3 jelzés,
  exit 1), majd törölte; `dotnet test`: 218/218. A reviewer utáni javítások után a build és a lint újra hibamentes (a
  frontend-változás a backend-teszteket nem érinti, a `dotnet test` nem futott újra). A kézi lépések a
  `docs/testing/manual/06.md`-ben (25 lépés), végrehajtásuk a fejlesztőre vár.
- **Fejlesztői döntés:**
  - *Stílus (V33):* az eredeti terv sima CSS volt UI-könyvtár nélkül. A 06. feladat előtt Tailwind CSS v4-re váltottam, és a vizuális tervezéshez a `frontend-design` plugint vontam be (projekt hatókörrel). Indok: a Tailwindet már ismerem, így a generált felületet át tudom látni és javítani; a skill pedig egységes vizuális irányt ad a három frontend-feladatnak. Komponenskönyvtárat szándékosan nem használunk, hogy a felület kódja a projektben maradjon és a védésen elmagyarázható legyen.
  - *Vizuális irány (V35):* az MI „vonalas füzet” irányt javasolt; ehelyett saját irányt adtam: régi FBI-aktákat idéző „aktadosszié” stílus, írógépes betűvel. A felkínált betűk közül a `@fontsource/courier-prime` csomagot választottam: valódi írógépes hatású, tartalmazza az „ő” és „ű” betűt, és helyben szolgálja ki a betűt, így nincs külső CDN-kérés, és minden gépen ugyanúgy néz ki (a Consolas-alapú rendszerbetű gépenként eltérne).
  - *„Összes” nézet (V36):* csak a saját jegyzeteket mutatja, a megosztottak a „Velem megosztott” nézetben vannak. Így egy lekérés elég, és a saját és a mások jegyzetei nem keverednek (a mappák is csak a sajátjaim).
  - *Linter (V34):* a jelenlegi Vite sablon oxlintet ad ESLint helyett; ezt megtartottam, mert a sablon része, és a `npm run lint` ugyanúgy ellenőrzi a kódot (az agent egy szándékosan hibás fájllal igazolta, hogy a `src`-t vizsgálja).
- **Ellenőrzés (fejlesztő):** a `docs/testing/manual/06.md` lépéseit futó backenden és `npm run dev` mellett végrehajtottam; az eredményeket a fájlba írtam. A 18. lépésben a megadott PowerShell-parancs hibát adott, ennek oka nem termékhiba volt, hanem a Windows PowerShell 5.1 kódolása (lásd lent). Javított parancsokkal a megosztás működött, és a címzettnél a felületen is megjelent.
- **Döntési eset jelölt?** (a fejlesztő dönt)
- **Korlát (kézi ellenőrzés):** az MI által írt kézi tesztlépés hibás volt Windows PowerShell 5.1 alatt, mert a szöveges kéréstörzset Latin-1 kódolással küldi, így az ékezetes JSON 400-at kapott. Ez csak a kézi végrehajtáskor derült ki, mert az agent nem futtathatta a parancsokat. A szerver viselkedése helyes volt (érvénytelen kódolású kérés → 400). Tanulság: az MI által írt, le nem futtatott lépéseket ténylegesen végre kell hajtani; a szabályt a 07–09. feladat kézi lépéseibe felvettem.

## 07 – Frontend: szerkesztő és mappák (2026-11-15)
- **Fázis:** megvalósítás
- **Eszköz:** Claude Code (VS Code bővítmény), Claude Opus 5.5
- **Kontextus:** `docs/tasks/07-frontend-editor-folders.md`, design 5., 6., 7., 9. szakasz (V7, V22–V25, V33–V36);
  spec 2., 3., 4., 8. elfogadási feltétel; CLAUDE.md, `.claude/rules/frontend.md`; `frontend-design` skill; a backend
  üzenetszövegei (`NoteService.cs`, `FolderService.cs`, `ValidationFailedException.cs`) és a 06-os kézi lépések formája
- **Terv és módosításai:** az agent a terv mellé nyolc kérdést tett fel javaslattal. A fejlesztő döntései: (1) a
  megnyitott jegyzet is az URL-ben van (`?note=<id>`, új jegyzetnél `?note=new`; mappa nézet `?view=folder&folder=<id>`);
  (2) az „Új jegyzet” üres szerkesztőt nyit, a `POST` az első Mentéskor megy el, mappa nézetben az aktuális mappával;
  (3) a javaslattól eltérően a mappa megváltoztatása NEM azonnal megy el, hanem a Mentés gombhoz kötött (a Mentés
  szükség szerint `PUT /api/notes/{id}`, majd `PUT /api/notes/{id}/folder` kérést küld); (4) nincs `maxLength`, határ
  felett a kliens a szerverével azonos üzenettel nem ment, a címszámláló a levágott hosszt mutatja; a 101/10 001-es kézi
  lépés így a kliens tiltását ellenőrzi, a szerveri elutasítást a backend tesztek fedik; (5) a „Velem megosztott” lista a
  07-ben nem kattintható (08); (6) nem mentett módosításnál megerősítő ablak a lapon belüli navigációkor, `beforeunload`
  nélkül; (7) széles képernyőn lista és szerkesztő egymás mellett (`max-w-7xl`), keskenyen a szerkesztő a lista helyén;
  (8) a nyitott mappa törlése után a „Mappa nélküli” nézet nyílik meg. A tervet így jóváhagyta.
- **Eltérés a tervtől megvalósítás közben:** (1) új fájl a tervhez képest: `notes/GuardedLink.tsx` (valódi link, de a
  bal kattintás a nem mentett módosítás ellenőrzésén megy át) – a 6. döntés megvalósításához kellett. (2) Új szín-token:
  `--color-paper` (a szerkesztő „gépelt lapja”); a vonalazott szövegmező a meglévő `manila-dark` tokenből rajzolja a
  vonalakat. (3) Új gombstílus: `dangerButton` (Törlés). (4) Az `api.ownNotes` paramétere a korábbi `unfiled` logikai
  érték helyett szűrő-objektum lett (`all` / `unfiled` / `folder`). (5) A `views.ts` statikus nézetlistája helyett
  `NoteView` unió (mappa nézettel) és URL-építő függvények. (6) A szerkesztő a mappák betöltése alatt, illetve közben
  törölt mappánál külön opciót mutat („Betöltés…” / „(törölt mappa)”), hogy a legördülő ne mutasson hamisan
  „(nincs mappa)”-t. (7) A böngésző Vissza gombja nem kérdez rá a nem mentett módosításra (a `BrowserRouter` mellett
  ehhez nincs blokkoló API); csak a lap saját linkjei és gombjai.
- **Blokkolt műveletek (hook/jogosultság):** egy olvasó Bash-parancsot (backend-fájlok listázása), amelynek szövegében a
  védett design-fájl neve is szerepelt, a hook blokkolt; helyette a Read eszköz.
- **Elbukó tesztek valódi hiba miatt:** – (frontend automatizált teszt nincs, NEM szerepel a feladatban).
- **Reviewer megállapításai:** ítélet: „nem zárható le” két kisebb hiba miatt (a reviewer saját futása: build sikeres,
  lint 0 jelzés, `dotnet test` 218/218; hatókörön kívüli változás, új csomag, migráció nincs). Blokkolók és sorsuk:
  (1) a `progress.md`-ben a 07 nincs kipipálva → a lezárás utolsó lépéseként kipipálva; (2) a `manual/07.md`
  előkészítése elavult lépésszámokra hivatkozott (27 → 33, 28 → 34) → javítva. Nem blokkolók: (3) nem mentett
  módosítás mellett a már nyitott jegyzet sorára kattintva az „Elvetés” ugyanarra az URL-re navigált, a szerkesztő nem
  töltődött újra, az oldal `dirty` jelzője viszont törlődött, így a további navigáció már nem kérdezett rá → javítva: a
  `go` nem csinál semmit, ha a cél az aktuális cím; (4) a Kilépés és a mappatörlés (×) nem kérdez rá a nem mentett
  módosításra → nem változott: a 6. döntés a lapon belüli navigációról szól, a mappatörlés nem zárja be a szerkesztőt,
  a Kilépés kérdése a fejlesztő döntése lehet; (5) mentés közben a mezők szerkeszthetők, a közben gépelt szöveg „nem
  mentett” marad, adatvesztés nincs → nem változott; (6) a 06-os bejegyzés fejlesztői „Ellenőrzés” sorában csonka mondat
  maradt → nem javítva, mert a fejlesztő szövege; (7) frontend automatizált teszt és `runs/` kimenet nem kell → egyetértés.
  A javítások után a build és a lint újra hibamentes.
- **Ellenőrzés:** `npm run build`: sikeres; `npm run lint` (oxlint): 0 figyelmeztetés, 0 hiba (az első futás 2
  `preserve-manual-memoization` figyelmeztetést adott a `useCallback` miatt, javítva); `dotnet test`: 218/218. A kézi
  lépések a `docs/testing/manual/07.md`-ben (35 lépés), végrehajtásuk a fejlesztőre vár; böngészős próbát az agent nem
  végzett.
- **Fejlesztői döntés:**
- **Ellenőrzés (fejlesztő):**
- **Döntési eset jelölt?** (a fejlesztő dönt) – lehetséges jelölt: a mappaváltás a Mentéshez kötése (az agent azonnali
  küldést javasolt).
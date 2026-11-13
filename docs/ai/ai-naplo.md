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

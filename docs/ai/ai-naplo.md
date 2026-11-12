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

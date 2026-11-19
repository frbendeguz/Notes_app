# Elfogadási tesztjelentés – v1.0

A `docs/spec.md` 8 elfogadási feltételének (AC1–AC8) ellenőrzése. Készült a 09. feladatban (2026-11-19), a `32cf29e`
commit és a 09. feladat két új tesztje alapján.

- **Automatizált tesztek:** xUnit integrációs tesztek (`Notes.Tests`, `WebApplicationFactory<Program>` + in-memory SQLite) és
  Vitest frontend-tesztek (`notes-web`). A tesztnevek `Osztály.Metódus` alakúak; `[Theory]` esetén az `InlineData` sorát is
  megadjuk.
- **Kézi ellenőrzés:** a tisztán felületi elemeket (hibaüzenet megjelenése, „Velem megosztott” lista) a
  `docs/testing/manual/06.md`, `07.md` és `08.md` lépései ellenőrzik. Ezeket a fejlesztő hajtotta végre, és az eredményt
  soronként rögzítette (06: 2026-11-13; 07: a 07b után újrafuttatott lépésekkel; 08: 2026-11-19). A hivatkozás alakja: `06.md/9` = a
  `06.md` 9. lépése. A lépések előkészítése (JWT-kulcs, adatbázis, backend és frontend indítása) a fájlok elején van.
  Az AC2 „másik eszköz” felületi változatának két lépése (AC2-K1, AC2-K2) magában ebben a fájlban van.
- **Teljes futás:** `docs/testing/runs/09-teljes-futas.txt` (`dotnet test`: 220/220), `09-npm-test.txt` (23/23),
  `09-npm-build.txt`, `09-npm-lint.txt` – összefoglaló a fájl végén.

## Kötelező esetek

| Kategória | Teszt | Mit bizonyít | Eredmény |
|---|---|---|---|
| Normál eset | `NotesTests.CreateNote_Valid_AppearsInListAlsoAfterNewLogin` | Érvényes jegyzet mentése 201, megjelenik a listában, új belépés után egy másik kliensen ugyanaz a lista és ugyanaz a jegyzet (AC2). | ✅ átment |
| Határeset 1 – cím 100 / 101 | `NotesTests.CreateNote_BoundaryLengths_Returns201AndStored(titleLength: 100, contentLength: 0)` és `NotesTests.CreateNote_TooLong_Returns400AndNoNoteCreated(titleLength: 101, …, "title")` | A pontosan 100 karakteres cím elfogadva és tárolva; a 101 karakteres 400, „A cím legfeljebb 100 karakter lehet.”, a felhasználónak 0 jegyzete van (AC3). | ✅ átment |
| Határeset 2 – szöveg 10 000 / 10 001 | `NotesTests.CreateNote_BoundaryLengths_Returns201AndStored(1, 10000)`, `(100, 10000)` és `NotesTests.CreateNote_TooLong_Returns400AndNoNoteCreated(1, 10001, "content")` | A pontosan 10 000 karakteres szöveg elfogadva (100 karakteres címmel együtt is); a 10 001 karakteres 400, a jegyzet nem jön létre (AC3). | ✅ átment |
| Hibás bemenet | `NotesTests.UpdateNote_InvalidInput_Returns400AndNoteUnchanged("   ", "új", "title")` (és `""`, `null` sora) | Meglévő jegyzet mentése szóközös/üres címmel 400, `errors.title` hibaüzenettel; a tárolt jegyzet (cím, szöveg, `UpdatedAt`) változatlan (AC4). | ✅ átment |
| Jogosulatlan művelet | `SharingTests.Viewer_UpdateAndDelete_Return403AndNoteUnchanged` | Az olvasó PUT és DELETE kérése 403 „Nincs jogosultságod ehhez a művelethez.”; a jegyzet címe, szövege és `UpdatedAt`-je változatlan (AC6). | ✅ átment |
| Regresszió | `SharingTests.DeleteFolder_SharedNoteInFolder_RecipientKeepsAccess` (új, 09) | Lásd lent. | ✅ átment |
| Hibafeltárás – valódi hiba (10) | `NotesPage.test.tsx` – „NotesPage – failed save (V47)” (2 eset) | Sikertelen mentésnél (leállított backend) a szerkesztő és a beírt szöveg megmarad (V47). A hibás változaton elbukik, a javítotton átmegy. Részletek, futások és reprodukálás: [`hibafeltaras.md`](hibafeltaras.md). | ✅ átment (a hibás változaton ❌) |

**A regressziós eset mit véd:** a korábbi feladatok, a 04. (mappatörlés, D4 `SetNull`) és a 05. (megosztás) együttes
működését a későbbi változtatásokkal, például a változtatási igénnyel szemben.
A tulajdonos mappában lévő, szerkesztővel megosztott jegyzetének mappáját törli. A teszt elvárja, hogy:
- a címzett továbbra is lássa a „Velem megosztott” listában, ugyanazzal a szereppel, és tudja szerkeszteni;
- a tulajdonosnál a megosztások listája változatlan maradjon;
- a jegyzet a tulajdonosnál mappa nélküli legyen, a címzett módosításával.

Ha egy későbbi változás (például a változtatási igény) a mappatörlést kaszkádolóra írná át, vagy a mappa jegyzeteivel együtt
a megosztásaikat is törölné, ez a teszt elbukik. A teszt elsőre átment, nem talált hibát.

A változtatási igénynél ez a táblázat bővül (regressziós és adatmegőrzési tesztek).

## AC1 – Csak a saját és a megosztott jegyzetek; hibás jelszó elutasítva

> Helyes adatokkal belépve a felhasználó csak a saját és a vele megosztott jegyzeteket látja. Hibás jelszóval a belépés
> elutasítva, hibaüzenet jelenik meg, munkamenet nem jön létre.

**Ellenőrzési mód – automatizált:**

| Teszt | Kategória |
|---|---|
| `AuthTests.Login_ValidCredentials_Returns200WithToken` | normál |
| `NotesTests.ListNotes_OtherUsersAndSharedNotes_ReturnsOnlyOwnNotes` – a `/api/notes` csak a saját jegyzeteket adja | normál |
| `SharingTests.ListShared_OwnForeignAndSharedNotes_ReturnsOnlySharedOrderedByUpdatedAtDesc` – a `/api/notes/shared` csak a vele megosztottakat, a sajátok listája külön | normál |
| `SharingTests.Stranger_NoteAndSharedList_Returns404AndNotListed` – idegen jegyzet nem látszik és nem érhető el | jogosulatlan művelet |
| `AuthTests.Login_WrongPassword_Returns401WithoutToken` – 401, nincs `token` a válaszban | hibás bemenet |
| `AuthTests.Login_UnknownUser_Returns401WithSameMessageAsWrongPassword` | hibás bemenet |
| `AuthTests.Login_MissingField_Returns401WithSameMessageAndNoToken` (5 sor, V15) | hibás bemenet |

**Ellenőrzési mód – kézi (felület):**
- `06.md/9`: hibás jelszó → „Hibás felhasználónév vagy jelszó.”, az URL `/login` marad, a Session storage-ban nincs
  `notes.token` (munkamenet nem jött létre).
- `06.md/10`, `06.md/11`: ismeretlen név és üres mezők esetén ugyanez az üzenet.
- `06.md/19–20` és `07.md/33`: a megosztott jegyzet csak a „Velem megosztott” fülön látszik, az „Összes” fülön nem (V36).

**Eredmény:** teljesül. Automatizált: `09-teljes-futas.txt`, mind átment. Kézi: a 06. és a 07. kézi próba fenti lépései az
elvárt eredményt adták.

## AC2 – Mentett jegyzet a listában, újbóli belépés és másik eszköz után is

> 1–100 karakteres címmel és legfeljebb 10 000 karakteres szöveggel mentett jegyzet megjelenik a listában, és újbóli belépés
> után, illetve másik eszközről is ugyanúgy látszik.

**Ellenőrzési mód – automatizált:**

| Teszt | Kategória |
|---|---|
| `NotesTests.CreateNote_Valid_AppearsInListAlsoAfterNewLogin` – mentés, lista, majd új `HttpClient` új belépéssel: azonos lista és részletek | normál |
| `NotesTests.SameAccount_TwoIndependentClients_SeeEachOthersChanges` (új, 09) – ugyanaz a fiók két független klienssel, egyszerre belépve: az A kliensen létrehozott jegyzet a B-n látszik; a B-n végzett módosítás az A következő lekérésénél látszik; a két lista azonos, és a regisztrációkor kapott token is érvényes marad | normál |
| `NotesTests.CreateNote_BoundaryLengths_Returns201AndStored` (1/0, 100/0, 1/10 000, 100/10 000) – a határokon mentett jegyzet tárolva | határeset |
| `NotesTests.CreateNote_Timestamps_SerializedAsUtcAndUnchangedAfterReadBack` – visszaolvasva ugyanazok az időbélyegek (D7) | normál |

**Ellenőrzési mód – kézi (felület):**
- `07.md/8–9`: mentés után a jegyzet a listában, F5 után ugyanaz nyílik meg.
- `07.md/36`: kilépés és újbóli belépés után a jegyzet a mentett tartalommal látszik.
- `07.md/16`, `07.md/18`: 100 karakteres cím és 10 000 karakteres szöveg mentve, a listában megjelenik.

**Kézi ellenőrzés – másik eszköz (ugyanaz a fiók két böngészőben):** a lépéseket a fejlesztő hajtotta végre
2026-11-19-én, futó backenden (`http://localhost:5098`) és `npm run dev` mellett (`http://localhost:5173`). A két
böngésző külön munkamenetet (külön tokent) kap, így két eszközt szimulál.

| # | Lépés | Elvárt eredmény | Eredmény |
|---|---|---|---|
| AC2-K1 | Ugyanazzal a fiókkal (`teszt_t8` / `Jelszo123`) belépés Chrome-ban és Firefoxban. **Chrome:** „Új jegyzet”, cím `Két eszköz próba`, szöveg `Chrome-ból`, „Mentés”. **Firefox:** „Frissítés”. | A jegyzet megjelenik a Firefox listájában; megnyitva a cím és a szöveg ugyanaz. | Az elvárt eredménynek megfelelő. |
| AC2-K2 | **Firefox:** a jegyzet szövegének végére ` – Firefoxból`, „Mentés”. **Chrome:** „Frissítés”, majd nyisd meg a jegyzetet. | Chrome-ban a szöveg: `Chrome-ból – Firefoxból`; a „Frissítve” időpont a Firefoxban mentett időpont. | Az elvárt eredménynek megfelelő. |

**Eredmény:** teljesül. Automatizált: `09-teljes-futas.txt`, mind átment. Kézi: a 07. kézi próba lépései és az AC2-K1, AC2-K2 lépés (két böngésző, ugyanaz a fiók) az elvárt eredményt adták.

## AC3 – Határesetek: cím 100/101, szöveg 10 000/10 001

> Pontosan 100 karakteres cím és pontosan 10 000 karakteres szöveg elfogadva; 101 karakteres cím vagy 10 001 karakteres szöveg
> esetén a mentés elutasítva, a jegyzet nem jön létre.

**Ellenőrzési mód – automatizált:**

| Teszt | Kategória |
|---|---|
| `NotesTests.CreateNote_BoundaryLengths_Returns201AndStored` – `(1, 0)`, `(100, 0)`, `(1, 10000)`, `(100, 10000)`; a tárolt hossz is ellenőrizve | határeset |
| `NotesTests.CreateNote_TooLong_Returns400AndNoNoteCreated` – `(101, 0, "title")`, `(1, 10001, "content")`; 400, mezőszintű hibaüzenet, a felhasználónak 0 jegyzete van | határeset |
| `NotesTests.UpdateNote_InvalidInput_Returns400AndNoteUnchanged` – 101 karakteres cím és 10 001 karakteres szöveg szerkesztéskor: 400, a jegyzet változatlan | határeset |
| `NotesTests.CreateNote_TitleWithSurroundingSpaces_StoredTrimmedContentKept` – a cím levágva tárolódik (V2), a hossz a levágott címre vonatkozik | határeset |

A határok egy helyen vannak (`Notes.Api/Limits.cs`, D8: `TitleMax = 100`, `ContentMax = 10_000`), a számolás UTF-16 egységben
(`string.Length`, V3; `NoteService.Validate`).

**Ellenőrzési mód – kézi (felület):** `07.md/16–20`: a számlálók `100/100` és `10 000/10 000` mellett „Mentve.”;
101 / 10 001 karakternél a kliens a hibát mutatja, és a jegyzet nem változik, illetve nem jön létre (V40).

**Eredmény:** teljesül. Automatizált: `09-teljes-futas.txt`. Kézi: a 07. kézi próba fenti lépései az elvárt eredményt adták.

## AC4 – Üres vagy szóközös cím: 400, a korábbi tartalom változatlan

> Üres vagy csak szóközökből álló cím esetén a mentés elutasítva (400-as hiba és hibaüzenet), meglévő jegyzet
> szerkesztésekor a korábbi tartalom változatlan marad.

**Ellenőrzési mód – automatizált:**

| Teszt | Kategória |
|---|---|
| `NotesTests.CreateNote_MissingOrBlankField_Returns400AndNoNoteCreated` – `""`, `"   "`, `null` cím és `null` szöveg: 400, `errors.title` / `errors.content` hibaüzenettel, jegyzet nem jön létre | hibás bemenet |
| `NotesTests.UpdateNote_InvalidInput_Returns400AndNoteUnchanged` – ugyanez szerkesztéskor (és túl hosszú mezők): a tárolt cím, szöveg és `UpdatedAt` változatlan | hibás bemenet |
| `NotesTests.UpdateNote_ViewerWithInvalidBody_Returns400AndNoteUnchanged` – az olvasó érvénytelen törzse is 400 (V18), a jegyzet változatlan | hibás bemenet |

**Ellenőrzési mód – kézi (felület):** `07.md/12–13`: üres, illetve öt szóközös cím mentésekor felül „A megadott adatok
érvénytelenek.”, a cím alatt „A cím megadása kötelező.”, a szöveg a mezőben marad. `07.md/15`: elvetés és újranyitás után a
jegyzet a korábbi tartalommal látszik.

**Eredmény:** teljesül. Automatizált: `09-teljes-futas.txt`. Kézi: a 07. kézi próba fenti lépései az elvárt eredményt adták.

## AC5 – Megosztás; nem létező felhasználó, önmaga, ismétlés

> A tulajdonos megoszt egy jegyzetet egy létező felhasználóval olvasó vagy szerkesztő joggal, a címzett a „Velem megosztott”
> listában látja. Nem létező felhasználóval, önmagával vagy ismételt megosztás esetén hibaüzenet jelenik meg, duplikáció nem
> keletkezik.

**Ellenőrzési mód – automatizált:**

| Teszt | Kategória |
|---|---|
| `SharingTests.ShareNote_ExistingUser_Returns201AndRecipientSeesItInSharedList` – `Viewer` és `Editor`: 201, a címzett `/api/notes/shared` listájában a jegyzet a szereppel és a tulajdonos nevével | normál |
| `SharingTests.ShareNote_UserNameInOtherCaseWithSpacesAndLowerCaseRole_Returns201` – a címzett kis/nagybetűtől függetlenül, a szerep is (V28) | határeset |
| `SharingTests.ShareNote_UnknownUser_Returns404AndNoShareCreated` – 404 „Nincs ilyen nevű felhasználó.”, megosztás nem jön létre (V10, V29) | hibás bemenet |
| `SharingTests.ShareNote_WithSelfInOtherCase_Returns400AndNoShareCreated` – önmagával 400 (V10) | hibás bemenet |
| `SharingTests.ShareNote_Repeated_Returns409AndShareCountUnchanged` – ismétlés (más szereppel is) 409, a megosztások száma 1 (V4, V10) | hibás bemenet |
| `SchemaTests.AddShare_SameNoteAndUserTwice_ThrowsDbUpdateExceptionAndNoDuplicate` – az egyedi index adatbázis-szinten is megakadályozza a duplikációt | hibás bemenet |
| `SharingTests.ShareNote_InvalidRole_Returns400AndNoShareCreated`, `ShareNote_MissingUserName_Returns400AndNoShareCreated` | hibás bemenet |
| `SharePanel.test.tsx` – „shares with the chosen role, clears the name and reloads the list”, „does not send an empty user name” | normál / hibás bemenet |

**Ellenőrzési mód – kézi (felület):**
- `08.md/4–8`: a felületen megjelenő hibaüzenetek: üres név → „A felhasználónév megadása kötelező.”, nem létező → „Nincs ilyen
  nevű felhasználó.”, önmaga → „A jegyzetet saját magaddal nem oszthatod meg.”, ismétlés → „A jegyzet már meg van osztva ezzel
  a felhasználóval.”. Közben a „Jelenlegi megosztások” lista egy sort tartalmaz.
- `08.md/9`: a címzettnél a „Velem megosztott” listában a jegyzet „OLVASÓ” bélyegzővel és a tulajdonos nevével.
- `08.md/17–18`: ugyanez „SZERKESZTŐ” szereppel.

**Eredmény:** teljesül. Automatizált: `09-teljes-futas.txt`, `09-npm-test.txt`. Kézi: a 08. kézi próba fenti lépései az elvárt
eredményt adták.

## AC6 – Jogosultság: olvasó, szerkesztő, idegen

> Az olvasó szerkesztési vagy törlési kísérlete 403-as hibát kap és a jegyzet változatlan; a szerkesztő módosíthat, de törlési
> és megosztási kísérlete 403; a jegyzethez nem tartozó idegen felhasználó 403/404-et kap.

**Ellenőrzési mód – automatizált:**

| Teszt | Kategória |
|---|---|
| `SharingTests.Viewer_UpdateAndDelete_Return403AndNoteUnchanged` | jogosulatlan művelet |
| `SharingTests.Editor_UpdatesNote_Returns200ButDeleteIs403AndNoteKept` | normál / jogosulatlan művelet |
| `SharingTests.ShareEndpoints_NotOwner_RejectedAndSharesUnchanged` – szerkesztő/olvasó 403, idegen és nem létező jegyzet 404 a megosztás-végpontokon; a megosztások változatlanok | jogosulatlan művelet |
| `SharingTests.ShareNote_NotOwnerWithUnknownUser_GetsAccessErrorNotRecipientError` | jogosulatlan művelet |
| `NotesTests.NoteOperation_CallerRole_FollowsMatrix` – szerep × művelet (olvasás, szerkesztés, törlés) a design 4. mátrixa szerint | jogosulatlan művelet |
| `NotesTests.NoteOperation_ForeignVsMissingNote_IdenticalNotFoundBody` – idegen = nem létező, azonos 404 törzs (V1) | jogosulatlan művelet |
| `FoldersTests.MoveNote_NotOwner_RejectedAndFolderUnchanged` – áthelyezés csak a tulajdonosnak | jogosulatlan művelet |
| `NoteAccessServiceTests.EnsureCan_CallerAndOperation_FollowsMatrix`, `GetRole_Caller_ReturnsRoleFromDatabase` – egységteszt a `NoteAccessService`-re | jogosulatlan művelet |
| `NotesTests.GetNote_ShareRemovedFromDatabase_Returns404OnNextRequest` – a szerep minden kérésnél az adatbázisból jön | regresszió |
| `NoteEditor.test.tsx` – „NoteEditor – role-based UI” (olvasó: csak olvasható mezők; szerkesztő: nincs törlés, mappa, megosztás), „shows the server's 403 on save and keeps the typed text” | normál |

**Ellenőrzési mód – kézi:** `08.md/10–12` (olvasó: a felület csak olvasható; PowerShellből a PUT, DELETE, POST /shares 403,
a jegyzet változatlan), `08.md/18–21` (szerkesztő: módosít és ment; DELETE, POST /shares, PUT /folder 403).

**Eredmény:** teljesül. Automatizált: `09-teljes-futas.txt`, `09-npm-test.txt`. Kézi: a 08. kézi próba fenti lépései az elvárt
eredményt adták (a 12. lépés segédfüggvényének PowerShell 5.1-es hibáját a fejlesztő javította, lásd `08.md` Eredmény).

## AC7 – Visszavonás után a címzett nem éri el, a tulajdonosnál megmarad

> Megosztás visszavonása után a címzett a következő lekérésnél már nem látja és nem éri el a jegyzetet; a jegyzet a
> tulajdonosnál megmarad.

**Ellenőrzési mód – automatizált:**

| Teszt | Kategória |
|---|---|
| `SharingTests.RevokeShare_Owner_Returns204RecipientLosesAccessOwnerKeepsNote` – `Viewer` és `Editor`: 204; a címzett GET és PUT kérése 404, a „Velem megosztott” lista üres; a tulajdonosnál a jegyzet és a tartalma megvan | normál |
| `SharingTests.RevokeShare_OneOfTwoRecipients_OtherKeepsAccess` | határeset |
| `SharingTests.RevokeShare_NotShared_Returns404` | hibás bemenet |
| `SharingTests.RevokeShare_ThenShareAgainWithOtherRole_RecipientGetsNewRole` (V4) | normál |
| `NoteAccessServiceTests.GetRole_ShareRevoked_ReturnsNoneOnNextCall` | normál |
| `NotesPage.shared.test.tsx` – „opens a shared note with its role, and after a revoke the refresh shows a message and empties the list”, „a save that gets 404 after a revoke shows the message, keeps the typed text and empties the list” | normál |

**Ellenőrzési mód – kézi:** `08.md/13–16` (visszavonás megerősítéssel; a tulajdonosnál a jegyzet megmarad; a címzett
„Frissítés” után üres listát lát, F5 után „A jegyzet nem található.”), `08.md/22` (visszavonás utáni mentés).

**Eredmény:** teljesül. Automatizált: `09-teljes-futas.txt`, `09-npm-test.txt`. Kézi: a 08. kézi próba fenti lépései az elvárt
eredményt adták.

## AC8 – Mappák

> Egy jegyzet legfeljebb egy mappában lehet; a mappanév 1–50 karakter, felhasználónként egyedi (kis- és nagybetűtől
> függetlenül); duplikált név elutasítva. Nem üres mappa törlésekor a benne lévő jegyzetek megmaradnak, mappa nélküli állapotba
> kerülnek.

**Ellenőrzési mód – automatizált:**

| Teszt | Kategória |
|---|---|
| `FoldersTests.CreateFolder_BoundaryLength_Returns201AndStored` – 1 és 50 karakter | határeset |
| `FoldersTests.CreateFolder_InvalidName_Returns400AndNoFolderCreated` – 51 karakter, `""`, `"   "`, `null`: 400, mappa nem jön létre | határeset / hibás bemenet |
| `FoldersTests.CreateFolder_MaxLengthPlusSurroundingSpaces_Returns201`, `CreateFolder_NameWithSurroundingSpaces_StoredTrimmed` | határeset |
| `FoldersTests.CreateFolder_DuplicateNameIgnoringCase_Returns409AndNoDuplicate` – `Árvíztűrő`/`árvíztűrő`/`ÁRVÍZTŰRŐ`/szóközös, `Munka`/`Munka`: 409, nincs duplikátum | hibás bemenet |
| `FoldersTests.CreateFolder_SameNameAsOtherUsersFolder_Returns201` – az egyediség felhasználónként | normál |
| `FoldersTests.CreateFolder_SameLettersWithoutAccents_Returns201` – az ékezet számít (V27) | határeset |
| `SchemaTests.AddFolder_SameNormalizedNameForSameOwner_ThrowsAndNoDuplicate`, `AddFolder_SameNameForDifferentOwners_Succeeds` – adatbázis-szintű egyedi index (D2) | hibás bemenet |
| `FoldersTests.MoveNote_ToAnotherFolder_VisibleOnlyInNewFolder` – egy jegyzet legfeljebb egy mappában | normál |
| `FoldersTests.MoveNote_IntoFolderAndOutWithNull_FiltersFollow` | normál |
| `FoldersTests.DeleteFolder_NonEmpty_Returns204AndNotesKeptFolderless` – a jegyzetek megmaradnak, `folderId: null`, a „Mappa nélküli” szűrőben látszanak | normál |
| `SchemaTests.DeleteFolder_FolderHasNote_NoteRemainsWithNullFolderId` (D4) | normál |
| `SharingTests.DeleteFolder_SharedNoteInFolder_RecipientKeepsAccess` (új, 09) – a mappatörlés a megosztást sem érinti | regresszió |
| `FoldersTests.FolderUse_ForeignVsMissingFolder_Identical404AndNothingChanged` (V20) | jogosulatlan művelet |

**Ellenőrzési mód – kézi (felület):** `07.md/2–6` (üres, szóközös, 51 karakteres név hibaüzenettel; `projektek` → „Ilyen
nevű mappád már van.”), `07.md/21–25` (egy mappa a „Mappa” mezőben, áthelyezés), `07.md/28–29` (nem üres mappa törlése: a
jegyzet a „Mappa nélküli” nézetben megmarad).

**Eredmény:** teljesül. Automatizált: `09-teljes-futas.txt`. Kézi: a 07. kézi próba fenti lépései az elvárt eredményt adták.

## Összesítés

| Feltétel | Automatizált | Kézi | Eredmény |
|---|---|---|---|
| AC1 | ✅ | `06.md/9–11`, `06.md/19–20` | teljesül |
| AC2 | ✅ (+1 új teszt) | `07.md/8–9`, `07.md/36`, AC2-K1–K2 (Chrome + Firefox) | teljesül |
| AC3 | ✅ | `07.md/16–20` | teljesül |
| AC4 | ✅ | `07.md/12–15` | teljesül |
| AC5 | ✅ | `08.md/4–9`, `08.md/17–18` | teljesül |
| AC6 | ✅ | `08.md/10–12`, `08.md/18–21` | teljesül |
| AC7 | ✅ | `08.md/13–16`, `08.md/22` | teljesül |
| AC8 | ✅ (+1 új regressziós teszt) | `07.md/2–6`, `07.md/21–29` | teljesül |

A 09. feladatban írt két új teszt elsőre átment; termékhibát nem találtak.

## Futási eredmények (2026-11-19)

| Parancs | Eredmény | Mentett kimenet |
|---|---|---|
| `dotnet test --logger "console;verbosity=normal"` | **220/220 átment**, 0 elbukott, 0 kihagyott (9,8 s) | `docs/testing/runs/09-teljes-futas.txt` |
| `npm test` (`notes-web`) | **5 tesztfájl, 23/23 átment** (Vitest 5.1.1) | `docs/testing/runs/09-npm-test.txt` |
| `npm run build` (`notes-web`) | sikeres, kilépési kód 0 (`tsc -b` + `vite build`) | `docs/testing/runs/09-npm-build.txt` |
| `npm run lint` (`notes-web`) | hibamentes, kilépési kód 0 (oxlint; átirányított kimenetnél nem ír összegzősort) | `docs/testing/runs/09-npm-lint.txt` |

A `09-teljes-futas.txt`-ben szereplő `Failed executing DbCommand` és `Hosting failed to start` naplósorok nem
tesztbukások. Ezek szándékosan kiváltott hibák naplói:
- a `SchemaTests` egyediségi tesztjei (`…ThrowsDbUpdateException…`, `…ThrowsAndNoDuplicate`) adatbázis-hibát váltanak ki;
- a `AuthTests.Startup_JwtKeyMissingOrTooShort_Throws` indítási hibát vált ki.

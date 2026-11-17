# Tervezési dokumentum – Szinkronizált jegyzetkezelő

Ez a dokumentum a `docs/spec.md` megvalósítási terve. Ütközés esetén a spec az erősebb;
ha a kettő között ellentmondást találsz, KÉRDEZZ.

## 1. Architektúra

```
React + TS (Vite)  ──HTTP/JSON──▶  ASP.NET Core Web API  ──EF Core──▶  SQLite
 (JWT Bearer fejléc)               Controllers → Services → AppDbContext
```

Solution-struktúra:

```
Notes.sln
├─ Notes.Api/
│  ├─ Controllers/   Auth, Notes, Folders (vékonyak: DTO be → service hívás → HTTP kód ki)
│  ├─ Services/      AuthService, NoteService, FolderService, NoteAccessService
│  ├─ Data/          AppDbContext, Entities/
│  ├─ Migrations/    EF Core migrációk (a dotnet ef generálja)
│  ├─ Dtos/          request/response modellek
│  ├─ Errors/        kivételek + központi hibakezelő
│  ├─ Limits.cs      hosszkorlátok
│  └─ Text.cs        Text.Normalize
├─ Notes.Tests/      xUnit (egység- és integrációs tesztek, TestApiFactory)
└─ notes-web/        Vite + React + TypeScript
```

## 2. Adatmodell

| Entitás | Mezők | Megkötések |
|---|---|---|
| User | Id (Guid), UserName, NormalizedUserName, PasswordHash, CreatedAt | egyedi `NormalizedUserName` |
| Folder | Id, OwnerId, Name, NormalizedName | egyedi `(OwnerId, NormalizedName)`; név 1–50 |
| Note | Id, OwnerId, FolderId?, Title, Content, CreatedAt, UpdatedAt | Title 1–100, Content ≤ 10 000 |
| NoteShare | Id, NoteId, UserId, Role (`Viewer`=0, `Editor`=1) | egyedi `(NoteId, UserId)` |

Kapcsolatok és törlési viselkedés:
- User 1─* Folder (kaszkád), User 1─* Note mint tulajdonos (kaszkád).
- Folder 0..1─* Note: `OnDelete(SetNull)` – mappa törlésekor a jegyzet megmarad, mappa nélküli lesz.
- Note 1─* NoteShare (kaszkád), User 1─* NoteShare mint címzett (kaszkád).
- A tulajdonos a `Note.OwnerId`; a `NoteShare`-ben nincs „Owner" szerep.
- `HasMaxLength` SQLite-on nem kényszerít: a hosszkorlátot a szerveri validáció adja.

Konstansok (`Limits`): TitleMax=100, ContentMax=10_000, FolderNameMax=50, UserNameMin=3,
UserNameMax=32, PasswordMin=8, PasswordMax=128.

`Text.Normalize(s) => s.Trim().ToUpperInvariant()`.

## 3. Hitelesítés

- JWT Bearer (HS256). Claim-ek: `sub` = felhasználó Guid, `name` = felhasználónév. Szerep nincs a tokenben.
- Lejárat: 60 perc, nincs refresh token. Lejárt/hibás token → 401, a kliens kijelentkeztet. Az ellenőrzés
  óraeltérés-tűrés nélkül fut (`ClockSkew = 0`, V16), így a token pontosan a lejáratkor érvénytelenné válik.
- `GET /api/auth/me` a token claimjeiből válaszol, adatbázis-lekérdezés nélkül (V17).
- Konfiguráció: `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiresMinutes` az `appsettings.json`-ban;
  `Jwt:Key` (legalább 32 bájt) NEM kerül a repóba: user-secrets / környezeti változó. A hiányzó
  kulcs indításkor hibát okoz. A tesztek saját kulcsot adnak.
- Jelszó: `PasswordHasher<User>` (PBKDF2), a teljes ASP.NET Identity nélkül.
- Belépési hiba (rossz jelszó VAGY ismeretlen felhasználó): 401, UGYANAZ a magyar hibaüzenet
  („Hibás felhasználónév vagy jelszó."), token nem készül. Hiányzó vagy üres mező a belépésnél szintén 401
  ugyanezzel az üzenettel (V15); a regisztrációnál a hiányzó mező továbbra is 400.
- Kliens: a token `sessionStorage`-ban (V13). A böngészőlap/ablak bezárásával a kliens elfelejti a tokent;
  új lapon újra be kell lépni. A szerveren a token a lejáratáig (60 perc) érvényes marad, visszavonás nincs.
  Az XSS-kockázat ugyanaz, mint `localStorage` esetén (ezt a tárolóváltás nem csökkenti).

## 4. Jogosultsági mátrix

`NoteAccessService.GetRole(userId, noteId)` → `Owner | Editor | Viewer | None`.

| Művelet | Tulajdonos | Szerkesztő | Olvasó | Idegen |
|---|---|---|---|---|
| Olvasás | ✅ | ✅ | ✅ | 404 |
| Cím/szöveg módosítása | ✅ | ✅ | 403 | 404 |
| Törlés | ✅ | 403 | 403 | 404 |
| Megosztás, visszavonás, megosztások listázása | ✅ | 403 | 403 | 404 |
| Mappába helyezés | ✅ | 403 | 403 | 404 |

A nem létező jegyzet és az idegen jegyzet válasza megkülönböztethetetlen (404, azonos törzs).
Az ellenőrzés minden kérésnél adatbázisból történik (visszavonás azonnal hat).
A láthatósági szűrés is a `NoteAccessService` feladata: a „saját jegyzetek” (`OwnedNotes`) és a 05. feladattól
a „Velem megosztott” lista lekérdezése is itt készül, a `role` értékét is innen kapja a válasz (V19).
A mappák tulajdon-ellenőrzése is itt van (`OwnedFolders`, `EnsureOwnFolderAsync`); idegen és nem létező mappa
minden végponton ugyanazt a 404-et adja (V20).
A `NoteShare`-sorok lekérdezése is itt van (`SharedNotes`, `SharesOf`, `ShareOf`). A `SharesOf` és a `ShareOf` nem
ellenőriz jogosultságot, ezért csak a sikeres `EnsureCanAsync(Share)` után hívható; más service csak írhat
megosztás-sort (felvétel, törlés), szűrni nem (V19).
Nem GUID formájú azonosítóra az `{id:guid}` útvonal-megkötés miatt általános 404 jön; ez nem árul el
semmit, mert ilyen azonosítójú jegyzet nem is létezhet.

## 5. REST API

```
POST   /api/auth/register            {userName, password}  → 201 {token, userName}
POST   /api/auth/login               {userName, password}  → 200 {token, userName}
GET    /api/auth/me                                         → 200 {id, userName}   (token ellenőrzésére)

GET    /api/notes                    saját jegyzetek (opcionális: ?folderId=<guid> | ?unfiled=true)
GET    /api/notes/shared             „Velem megosztott"
GET    /api/notes/{id}               tulajdonos, szerkesztő, olvasó
POST   /api/notes                    {title, content, folderId?}
PUT    /api/notes/{id}               {title, content}              tulajdonos, szerkesztő
DELETE /api/notes/{id}                                             tulajdonos
PUT    /api/notes/{id}/folder        {folderId | null}             tulajdonos

GET    /api/notes/{id}/shares        tulajdonos
POST   /api/notes/{id}/shares        {userName, role}              tulajdonos
DELETE /api/notes/{id}/shares/{userId}                             tulajdonos

GET    /api/folders                  [{id, name, noteCount}]
POST   /api/folders                  {name}
DELETE /api/folders/{id}
```

Státuszkódok (sikeres válasz): `POST` → 201 + `Location` fejléc + `NoteDetail`; `PUT` → 200 + `NoteDetail`;
`DELETE` → 204 üres törzzsel. A mappa- és megosztás-végpontok ugyanezt a mintát követik.
A `POST /api/folders` `Location` fejléce `/api/folders/{id}`, bár ezen az útvonalon nincs `GET` (csak `DELETE`,
ezért egy `GET` kérés oda 405-öt kapna); ez dokumentált, vállalt eltérés (V26).

Mappák és jegyzetek részletei (04. feladat):
- `GET /api/notes?folderId=<id>`: idegen vagy nem létező mappára 404, nem üres lista (V20).
- `unfiled=false` ugyanaz, mint a szűrő hiánya; 400 csak `folderId` + `unfiled=true` együtt (V21).
- `GET /api/folders`: a sorrend magyar ábécé szerinti, kis- és nagybetűtől független, a rendezés memóriában
  történik `hu-HU` kultúrával (V22).
- `PUT /api/notes/{id}/folder`: előbb a jegyzet jogosultsága, utána a mappa ellenőrzése, így a szerkesztő idegen
  mappára is 403-at kap, és a mappa létezéséről nem tud meg semmit (V25). Az `UpdatedAt` nem változik (V23).
- `PUT /api/notes/{id}` a törzsben küldött `folderId`-t figyelmen kívül hagyja; mappát csak a `/folder` végpont vált.
- Ha a mappát egy másik kérés közben törlik, a mappába helyezés és a mappás létrehozás is 404-et ad (nem 500-at).

Megosztás részletei (05. feladat):
- `role`: `"Viewer"` vagy `"Editor"`, kis- és nagybetűtől függetlenül (`"viewer"` is jó). Minden más szöveg
  (`"Owner"`, hiányzó mező) 400 `errors.role`; számként küldött érték (`"role": 1`) 400 az általános
  „A kérés formátuma érvénytelen.” üzenettel, mert a kérés már a beolvasásnál elbukik (V28).
- `userName`: hiányzó vagy üres → 400 `errors.userName`; formailag érvénytelen név → 404, mert ilyen felhasználó
  nem létezhet (V29).
- `POST` ellenőrzési sorrend: validáció → tulajdonos-ellenőrzés → címzett keresése → önmaga → ismétlés. A szerkesztő
  és az olvasó mindig 403-at, az idegen 404-et kap, így egyikük sem tudja meg, létezik-e egy felhasználónév; az
  önmagaddal való megosztás csak a tulajdonosnak ad 400-at (V30).
- `POST` → 201 + `ShareInfo` + `Location: /api/notes/{id}/shares/{userId}`, `GET` végpont nélkül (V31, mint V26).
- `GET /api/notes/{id}/shares`: felhasználónév szerint, a normalizált oszlopon, az adatbázisban rendezve; `hu-HU`
  rendezés nem kell, mert a felhasználónév csak ASCII karaktert tartalmazhat (V32).
- `GET /api/notes/shared`: `UpdatedAt` szerint csökkenő, egyezésnél a létrehozás ideje dönt (mint a saját listánál);
  a `folderId` itt is jelen van, mindig `null` (V24).

Válasz-DTO-k:
- `NoteSummary`: `id, title, updatedAt, role, ownerUserName, folderId` (a 04. feladattól a mező mindig jelen van;
  a tulajdonosnak a tényleges érték vagy `null`, a nem tulajdonosnak mindig `null`, V24)
- `NoteDetail`: `NoteSummary` + `content, createdAt`
- `ShareInfo`: `userId, userName, role`
- A `role` és `ShareRole` JSON-ban string: `"Owner" | "Editor" | "Viewer"` (`JsonStringEnumConverter`).

Hibakezelés: minden hiba ProblemDetails; a `detail` a felhasználónak szánt magyar üzenet.
Validációs hibánál (400) `errors` is van (mezőnév → üzenetek).
Feldolgozási sorrend módosító kérésnél: 1. a kérés validációja, 2. jogosultság-ellenőrzés, 3. módosítás (V18).

| Eset | Kód |
|---|---|
| Validációs hiba (üres/szóközös cím, túl hosszú cím/szöveg, érvénytelen mappanév, érvénytelen szerep, hibás regisztrációs adat) | 400 |
| Megosztás önmagaddal | 400 |
| Hiányzó/hibás/lejárt token, rossz jelszó | 401 |
| Jogosultság hiánya (olvasó ír/töröl; szerkesztő töröl/oszt) | 403 |
| Nem létező vagy idegen jegyzet / mappa; nem létező megosztási címzett; nem létező megosztás visszavonáskor | 404 |
| Foglalt felhasználónév, duplikált mappanév, ismételt megosztás | 409 |

Megvalósítási elv: a service-ek kivételt dobnak (`ValidationFailedException`, `NotFoundException`,
`ForbiddenException`, `ConflictException`, `UnauthorizedException` – az utóbbi a belépés 401-es hibájához),
egy központi `IExceptionHandler` alakítja ProblemDetails-szé. A JWT-middleware 401-es (`OnChallenge`) és
403-as (`OnForbidden`) válasza is ProblemDetails.
A DTO-k mezői nullable típusúak (`string?`), a validációt a service végzi (egységes üzenetek);
a `[ApiController]` automatikus modellvalidációja ne adjon más formátumú választ.

## 6. Validáció

- Cím: `Trim()` után 1–100 karakter; a levágott értéket tároljuk. Csak szóközből álló cím → 400.
- Szöveg: 0–10 000 karakter, nem trimmeljük; hiányzó (null) szöveg → 400, üres string megengedett.
- Karakterszámolás: `string.Length` (UTF-16 kódegység).
- Mappanév: `Trim()` után 1–50; egyediség `Text.Normalize` alapján felhasználónként. A normalizálás csak a kis- és
  nagybetűt egyesíti, az ékezetet nem: az „Árvíztűrő” és az „árvíztűrő” ütközik, az „Árvíztűrő” és az
  „Arvizturo” két külön név (V27).
- Felhasználónév: `Trim()` után 3–32 karakter, csak `A–Z a–z 0–9 _ . -`; egyediség `Text.Normalize` alapján.
- Jelszó: 8–128 karakter, és legalább egy kisbetű, egy nagybetű és egy számjegy (0–9). A kis- és
  nagybetű Unicode szerint értendő (`char.IsLower` / `char.IsUpper`, így az „á” és az „Á” is számít).
  Speciális karakter megengedett, de nem kötelező. A jelszót nem trimmeljük. Minden nem teljesülő
  szabály külön hibaüzenetet ad (`errors.password`), hogy a felhasználó lássa, mit kell javítania.
- A validáció az adatbázis módosítása előtt fut: sikertelen szerkesztés után a régi tartalom változatlan.

## 7. Frontend

- Útvonalak: `/login`, `/register`, `/notes` (védett).
- Fő nézet: oldalsáv (Összes, Mappa nélküli, mappák, „Velem megosztott"), jegyzetlista, szerkesztő. Az „Összes” a
  felhasználó SAJÁT összes jegyzete; a vele megosztottak csak a „Velem megosztott” nézetben látszanak (V36).
  A nézet, a mappa és a megnyitott jegyzet az URL-ben van (`?view=folder&folder=<id>`, `&note=<id>`, új jegyzetnél
  `&note=new`), így F5 után is megmarad (V37).
- Elrendezés: széles képernyőn (lg) a lista és a szerkesztő egymás mellett (`max-w-7xl`); keskeny képernyőn a
  megnyitott jegyzet a lista helyére kerül, „← Vissza a listához” linkkel (V43).
- Technika: React Router, TanStack Query, saját `src/api.ts` fetch-wrapper (token, hibák), Tailwind CSS v4 a hivatalos
  `@tailwindcss/vite` pluginnal (komponenskönyvtár nélkül, V33).
- Vizuális tervezés: a `frontend-design` plugin (Claude Code hivatalos marketplace, projekt hatókörrel telepítve)
  irányítja a felület megjelenését, Tailwind osztályokkal megvalósítva. A design tokenek (színek, betűtípus) a
  `src/index.css` `@theme` blokkjában vannak, nem szétszórva a komponensekben.
- Vizuális irány: régi FBI-aktákat idéző „aktadosszié” stílus, írógépes betűvel. Betűtípus: Courier Prime, a
  `@fontsource/courier-prime` npm csomagból, helyben kiszolgálva (nincs CDN, nincs külső kérés); az „ő” és „ű”
  betűt is tartalmazza (V35). A 07–08. feladat ugyanezt az irányt követi.
- Statikus ellenőrzés: oxlint (`npm run lint`), mert a jelenlegi Vite sablon ezt adja; TypeScript `strict` (V34).
- Fejlesztéskor a Vite proxyja továbbítja az `/api` hívásokat a backendnek (nincs CORS-konfiguráció). A backend
  címe csak a `vite.config.ts`-ben van, a `NOTES_API_URL` környezeti változóval felülírható.
- A gombokat a szerep alapján tiltjuk/rejtjük (olvasó csak olvas; szerkesztő nem lát törlést/megosztást),
  de a védelem a szerveren van.
- Karakterszámlálók (cím x/100, szöveg x/10 000), a szerver `detail` üzenetének megjelenítése.
- Szerkesztő (07. feladat):
  - Új jegyzet: üres szerkesztő, a `POST` csak az első Mentéskor megy el; mappa nézetben a `folderId` az aktuális
    mappa (V38).
  - Mappaváltás: a szerkesztő „Mappa” legördülője a Mentéssel megy el. A Mentés két lépés: előbb `PUT /notes/{id}`
    (ha a szöveg változott), utána `PUT /notes/{id}/folder` (ha a mappa változott). Ha a második elbukik (pl. a mappát
    közben törölték → 404), a szöveg mentve marad, a mappa nem változik, és a felhasználó hibaüzenetet lát (V39).
  - Kliensoldali validáció `maxLength` nélkül: a 101. karakter beírható, a számláló piros lesz, a Mentés nem megy el,
    és a szerverével azonos szövegű üzenet jelenik meg. A címszámláló a levágott (trimmelt) hosszt mutatja, ahogy a
    szerver számol. A szerver 400-as elutasítását a backend tesztek fedik le (V40).
  - Mentetlen módosítás: a lapon belüli navigáció (másik jegyzet, nézet) és a Kilépés megerősítést kér
    („Elveted a nem mentett módosításokat?”). A mappatörlés nem kér, mert nem zárja be a szerkesztőt. Az F5, a
    lapbezárás és a böngésző Vissza gombja nem kérdez rá (nincs `beforeunload`), ez vállalt korlát (V42, V45).
  - Ha a felhasználó az éppen megnyitott mappát törli, a „Mappa nélküli” nézet nyílik meg (V44).
  - A „Velem megosztott” lista a 07-ben csak olvasható; a megnyitás a szerep szerinti tiltással a 08-ban jön (V41).
- Szinkron: betöltéskor, mentés után, és „Frissítés" gombbal; valós idejű szinkron nincs. A TanStack Query
  ablakfókuszra nem tölt újra, és a 4xx válaszokat nem próbálja újra (azok nem átmeneti hibák).
- Elérhetetlen backend (a proxy üres 5xx-et ad): a kliens „A szerver nem érhető el.” jellegű üzenetet mutat.
- 401 válasz → kijelentkeztetés és irányítás a `/login`-ra. UI nyelve: magyar.

## 8. Tesztelési terv

Integrációs tesztek (`WebApplicationFactory<Program>`, in-memory SQLite) a spec elfogadási feltételeire:

| Feltétel | Teszt |
|---|---|
| 1 | Csak saját és megosztott jegyzet látszik; rossz jelszó 401 token nélkül |
| 2 | Mentés után a listában; új bejelentkezés („másik eszköz") után is |
| 3 | Cím 100 ✅/101 ❌, szöveg 10 000 ✅/10 001 ❌ (`[Theory]`); elutasításkor nem jön létre jegyzet |
| 4 | Üres és szóközös cím 400; sikertelen szerkesztés után változatlan tartalom |
| 5 | Megosztás sikeres; nem létező felhasználó, önmaga, ismétlés hibát ad, duplikáció nincs |
| 6 | Szerep × művelet mátrix (`[Theory]`) |
| 7 | Visszavonás után a címzettnek 404, a tulajdonosnál megmarad |
| 8 | Mappanév 1/50/51; kis-nagybetű és ékezetes duplikátum; mappatörlés után a jegyzetek megmaradnak |

Egységtesztek: `NoteAccessService`, validátorok.

A házi feladat követelményei szerint kötelezően bemutatandó (09–10. feladat, `docs/testing/`):
- az elfogadási feltételekhez rendelt ellenőrzési mód (automatizált teszt vagy reprodukálható kézi lépések),
- legalább 1 normál eset, 2 érdemi határeset, 1 hibás bemenet / jogosulatlan művelet, 1 regressziós eset,
- legalább egy teszt, amely egy ismert hibás változaton elbukik és a javítotton átmegy, a két futás
  mentett kimenetével (`docs/testing/runs/`); szándékos hiba esetén ezt egyértelműen jelölni kell,
- a változtatási igénynél regressziós és (ha van migráció) adatmegőrzési teszt.

## 9. Döntések

### Adatréteg (D)
- D1 Kulcsok: `Guid`.
- D2 Egyediség normalizált oszlopokkal (nem SQLite `NOCASE`: az nem kezeli az ékezeteket).
- D3 A tulajdonos a `Note.OwnerId`, nem `NoteShare` sor.
- D4 Mappa törlése: `SetNull`.
- D5 Jegyzet/felhasználó törlése kaszkádol a megosztásokra.
- D6 A séma csak migrációval változik (`EnsureCreated` csak a tesztben).
- D7 Időbélyegek UTC-ben. Az `AppDbContext` értékkonvertere a beolvasott értéket UTC-nek jelöli, így a
  JSON-ban `Z` utótaggal jelenik meg (az SQLite nem tárolja a `DateTimeKind`-ot).
- D8 A hosszkorlátok egyetlen helyen (`Limits`) vannak.

### Értelmezési döntések (v2.0 változásnaplóhoz) (V)
A fejlesztő által kifejezetten jóváhagyva vagy módosítva: V1, V5 (módosítva), V13 (módosítva), V14–V45.
- V1 Idegen felhasználó: 404 (nem 403).
- V2 A cím trimmelve tárolódik.
- V3 Karakterszámolás: UTF-16 `Length`.
- V4 Szerepmódosítás nincs: Olvasó→Szerkesztő csak visszavonás + újramegosztás útján.
- V5 Jelszó: 8–128 karakter, legalább egy kisbetű, egy nagybetű és egy számjegy; speciális karakter nem
  kötelező. (Fejlesztői döntés: az MI javaslata csak a hosszt írta elő, a fejlesztő kiegészítette.)
- V6 Felhasználónév: 3–32 karakter, `A–Z a–z 0–9 _ . -`, kis/nagybetűfüggetlenül egyedi.
- V7 Mappa átnevezése nincs.
- V8 Üres szöveg megengedett.
- V9 JWT 60 perc, nincs refresh token.
- V10 Megosztás önmagaddal → 400; nem létező felhasználó → 404; ismétlés → 409.
- V11 A regisztráció 201-gyel azonnal tokent ad (automatikus belépés).
- V12 `GET /api/auth/me` a spec forgatókönyvein túli kisegítő végpont.
- V13 A token a kliensen `sessionStorage`-ban van, hogy a böngésző bezárásával a kliens elfelejtse.
  (Fejlesztői döntés: az MI eredetileg `localStorage`-ot javasolt. Vállalt következmény: új lapon újra
  be kell lépni; a szerveroldali érvényesség és az XSS-kockázat nem változik.)
- V14 HTTPS-átirányítás (`UseHttpsRedirection`) csak Development környezeten kívül; fejlesztéskor a Vite
  proxy HTTP-n éri el a backendet. (A 01. feladat reviewere találta a sablonmaradványt; a fejlesztő jóváhagyta.)
- V15 Belépésnél a hiányzó vagy üres mező is 401, ugyanazzal az üzenettel, mint a rossz jelszó.
- V16 JWT-ellenőrzés `ClockSkew = 0` (az alapértelmezett 5 perces tűrés helyett).
- V17 A `/api/auth/me` a tokenből válaszol. Vállalt következmény: a token lejáratáig a benne lévő adat érvényes
  marad; mivel fiók törlése és átnevezése nincs (Nem célok), ez nem okoz eltérést.
- V18 Módosító kérésnél a validáció a jogosultság-ellenőrzés előtt fut. Következmény: érvénytelen törzsre az olvasó,
  az idegen és a nem létező jegyzet is 400-at kap (nem 403/404-et). Ez nem árul el semmit a jegyzet létezéséről,
  mert minden esetben azonos, és a spec 6. feltételével sem ütközik, mert ott a kérés érvényes.
- V19 Minden jogosultsági és láthatósági döntés a `NoteAccessService`-ben van, a listák szűrése, a mappák
  tulajdon-ellenőrzése és a megosztás-sorok lekérdezése is (a fejlesztő döntése: a többi service nem szűrhet
  `OwnerId` vagy `NoteShare` alapján; a 05. feladatban a szigorúbb változatot választotta, így a `ShareService`
  csak ír, a lekérdezéseket a `NoteAccessService` végzi).
- V20 Idegen és nem létező mappa mindenhol 404 (szűrő, létrehozás mappával, áthelyezés, törlés), üres lista helyett is.
- V21 `unfiled=false` = nincs szűrő; csak a `folderId` + `unfiled=true` együttes megadása 400.
- V22 A mappalista magyar ábécé szerint, kis- és nagybetűtől függetlenül rendezett, memóriában, `hu-HU` kultúrával.
  (A fejlesztő először az adatbázisbeli `OrderBy(Name)`-et választotta, majd a reviewer jelzése alapján, miszerint a
  kisbetűs és ékezetes nevek a „Z” után kerülnének, erre módosította.) Vállalt következmény: a sorrend a futtató
  gép magyar nyelvi támogatásától (ICU) függ; `InvariantGlobalization` nem kapcsolható be.
- V23 A mappába helyezés nem frissíti az `UpdatedAt`-et, mert a jegyzet tartalma nem változik.
- V24 A `folderId` mindig jelen van a jegyzetválaszban; a nem tulajdonosnak `null`.
- V25 `PUT /api/notes/{id}/folder`: előbb a jegyzet jogosultsága, utána a mappa ellenőrzése.
- V26 A `POST /api/folders` `Location` fejléce `/api/folders/{id}`, `GET` végpont nélkül.
- V27 A mappanév egyedisége csak a kis- és nagybetűt hagyja figyelmen kívül, az ékezetet nem (a spec csak
  kis- és nagybetű-függetlenséget ír elő).
- V28 A megosztási szerep kis- és nagybetűtől független (`"viewer"` is elfogadott; az MI eredetileg pontos egyezést
  javasolt, a fejlesztő módosította). Számként küldött szerep: 400, általános formátumhiba-üzenettel.
- V29 Formailag érvénytelen címzett-felhasználónév: 404 (nem található), nem 400.
- V30 Megosztásnál: validáció → tulajdonos-ellenőrzés → címzett → önmaga → ismétlés.
- V31 A megosztás `Location` fejléce `/api/notes/{id}/shares/{userId}`, `GET` végpont nélkül.
- V32 A megosztások listája a normalizált felhasználónév szerint, az adatbázisban rendezett.
- V33 Stílus: Tailwind CSS (v4, `@tailwindcss/vite`) a sima CSS helyett, a `frontend-design` pluginnal a vizuális
  tervezéshez; komponenskönyvtár (pl. MUI, shadcn) nincs. (A fejlesztő döntése a 06. feladat előtt; az eredeti terv
  sima CSS volt.)
- V34 Linter: oxlint az ESLint helyett, mert a jelenlegi Vite sablon ezt adja (a fejlesztő döntése a 06. feladatban).
- V35 Vizuális irány: „aktadosszié” (régi FBI-akták), Courier Prime betűvel a `@fontsource/courier-prime` csomagból,
  helyben kiszolgálva. (Az MI „vonalas füzet” irányt és Literata betűt javasolt; a fejlesztő saját irányt adott,
  és a felkínált betűk közül a valódi írógépes hatású Courier Prime-ot választotta a Consolas-szerű helyett.)
- V36 Az „Összes” nézet a saját jegyzeteket mutatja; a megosztottak külön, a „Velem megosztott” nézetben.
- V37 A megnyitott jegyzet is az URL-ben van (`&note=<id>`, `&note=new`), nem csak helyi állapotban.
- V38 Új jegyzet: a `POST` csak az első Mentéskor; mappa nézetben az aktuális mappába kerül.
- V39 A mappaváltás a Mentés gombbal megy el, nem azonnal (az MI azonnali küldést javasolt, a fejlesztő a Mentéshez
  kötötte, hogy egy művelet legyen). Következmény: kétlépéses mentés, részleges sikerrel (szöveg mentve, mappa nem).
- V40 Kliensoldali validáció `maxLength` nélkül, trimmelt címhosszal; a kézi próba a kliens tiltását ellenőrzi.
- V41 A „Velem megosztott” lista a 07-ben csak olvasható.
- V42 Mentetlen módosításnál a lapon belüli navigáció megerősítést kér; `beforeunload` nincs.
- V43 Kétoszlopos elrendezés széles képernyőn (`max-w-7xl`), keskenyen a szerkesztő a lista helyén.
- V44 A megnyitott mappa törlése után a „Mappa nélküli” nézet nyílik.
- V45 A Kilépés mentetlen módosításnál megerősítést kér; a mappatörlés nem, mert nem zárja be a szerkesztőt
  (a 07. feladat reviewere vetette fel, a fejlesztő döntött).

## 10. Nem célok

Valós idejű szinkron, csoportos megosztás, címkék/kategóriák, mappa átnevezése, szerepmódosítás,
fiók törlése, jelszó-visszaállítás, belépési kísérletek korlátozása, HTTPS-konfiguráció az alapon túl,
frontend automatizált tesztek (csak `build` és `lint` kötelező).

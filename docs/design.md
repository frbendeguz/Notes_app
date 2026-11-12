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
- Lejárat: 60 perc, nincs refresh token. Lejárt/hibás token → 401, a kliens kijelentkeztet.
- Konfiguráció: `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiresMinutes` az `appsettings.json`-ban;
  `Jwt:Key` (legalább 32 bájt) NEM kerül a repóba: user-secrets / környezeti változó. A hiányzó
  kulcs indításkor hibát okoz. A tesztek saját kulcsot adnak.
- Jelszó: `PasswordHasher<User>` (PBKDF2), a teljes ASP.NET Identity nélkül.
- Belépési hiba (rossz jelszó VAGY ismeretlen felhasználó): 401, UGYANAZ a magyar hibaüzenet
  („Hibás felhasználónév vagy jelszó."), token nem készül.
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

Válasz-DTO-k:
- `NoteSummary`: `id, title, updatedAt, role, ownerUserName, folderId?` (a `folderId` csak a tulajdonosnak)
- `NoteDetail`: `NoteSummary` + `content, createdAt`
- `ShareInfo`: `userId, userName, role`
- A `role` és `ShareRole` JSON-ban string: `"Owner" | "Editor" | "Viewer"` (`JsonStringEnumConverter`).

Hibakezelés: minden hiba ProblemDetails; a `detail` a felhasználónak szánt magyar üzenet.
Validációs hibánál (400) `errors` is van (mezőnév → üzenetek).

| Eset | Kód |
|---|---|
| Validációs hiba (üres/szóközös cím, túl hosszú cím/szöveg, érvénytelen mappanév, érvénytelen szerep, hibás regisztrációs adat) | 400 |
| Megosztás önmagaddal | 400 |
| Hiányzó/hibás/lejárt token, rossz jelszó | 401 |
| Jogosultság hiánya (olvasó ír/töröl; szerkesztő töröl/oszt) | 403 |
| Nem létező vagy idegen jegyzet / mappa; nem létező megosztási címzett; nem létező megosztás visszavonáskor | 404 |
| Foglalt felhasználónév, duplikált mappanév, ismételt megosztás | 409 |

Megvalósítási elv: a service-ek kivételt dobnak (`ValidationFailedException`, `NotFoundException`,
`ForbiddenException`, `ConflictException`), egy központi `IExceptionHandler` alakítja ProblemDetails-szé.
A DTO-k mezői nullable típusúak (`string?`), a validációt a service végzi (egységes üzenetek);
a `[ApiController]` automatikus modellvalidációja ne adjon más formátumú választ.

## 6. Validáció

- Cím: `Trim()` után 1–100 karakter; a levágott értéket tároljuk. Csak szóközből álló cím → 400.
- Szöveg: 0–10 000 karakter, nem trimmeljük; hiányzó (null) szöveg → 400, üres string megengedett.
- Karakterszámolás: `string.Length` (UTF-16 kódegység).
- Mappanév: `Trim()` után 1–50; egyediség `Text.Normalize` alapján felhasználónként.
- Felhasználónév: `Trim()` után 3–32 karakter, csak `A–Z a–z 0–9 _ . -`; egyediség `Text.Normalize` alapján.
- Jelszó: 8–128 karakter, és legalább egy kisbetű, egy nagybetű és egy számjegy (0–9). A kis- és
  nagybetű Unicode szerint értendő (`char.IsLower` / `char.IsUpper`, így az „á” és az „Á” is számít).
  Speciális karakter megengedett, de nem kötelező. A jelszót nem trimmeljük. Minden nem teljesülő
  szabály külön hibaüzenetet ad (`errors.password`), hogy a felhasználó lássa, mit kell javítania.
- A validáció az adatbázis módosítása előtt fut: sikertelen szerkesztés után a régi tartalom változatlan.

## 7. Frontend

- Útvonalak: `/login`, `/register`, `/notes` (védett).
- Fő nézet: oldalsáv (Összes, Mappa nélküli, mappák, „Velem megosztott"), jegyzetlista, szerkesztő.
- Technika: React Router, TanStack Query, saját `src/api.ts` fetch-wrapper (token, hibák), sima CSS (UI-könyvtár nélkül).
- Fejlesztéskor a Vite proxyja továbbítja az `/api` hívásokat a backendnek (nincs CORS-konfiguráció).
- A gombokat a szerep alapján tiltjuk/rejtjük (olvasó csak olvas; szerkesztő nem lát törlést/megosztást),
  de a védelem a szerveren van.
- Karakterszámlálók (cím x/100, szöveg x/10 000), a szerver `detail` üzenetének megjelenítése.
- Szinkron: betöltéskor, mentés után, és „Frissítés" gombbal; valós idejű szinkron nincs.
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
A fejlesztő által kifejezetten jóváhagyva vagy módosítva: V1, V5 (módosítva), V13 (módosítva), V14.
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

## 10. Nem célok

Valós idejű szinkron, csoportos megosztás, címkék/kategóriák, mappa átnevezése, szerepmódosítás,
fiók törlése, jelszó-visszaállítás, belépési kísérletek korlátozása, HTTPS-konfiguráció az alapon túl,
frontend automatizált tesztek (csak `build` és `lint` kötelező).

# API-leírás (v1.0)

A `Notes.Api` REST végpontjai a **megvalósított kód** alapján (`Notes.Api/Controllers/`, `Notes.Api/Services/`,
`Notes.Api/Program.cs`). A tervezett API a `docs/design.md` 5. szakaszában van; az eltéréseket a `docs/progress.md`
11-es megjegyzése sorolja fel.

Fejlesztői cím: `http://localhost:5098` (a `launchSettings.json` `http` profilja). A frontend a Vite proxyn keresztül,
relatív `/api/...` úton hívja.

## Általános szabályok

### Hitelesítés
- A `POST /api/auth/register` és a `POST /api/auth/login` kivételével **minden végpont** bejelentkezést kér
  (globális `FallbackPolicy`). A token a `Authorization: Bearer <token>` fejlécben megy.
- A token JWT (HS256), 60 percig érvényes, óraeltérés-tűrés nélkül (`ClockSkew = 0`). Claim-ek: `sub` (a felhasználó
  azonosítója), `name` (felhasználónév). Szerep nincs a tokenben, a jegyzetenkénti szerepet a szerver minden kérésnél
  az adatbázisból dönti el (`NoteAccessService`). Refresh token és kijelentkeztető végpont nincs.
- Hiányzó, hibás vagy lejárt token → **401**, `detail`: „Bejelentkezés szükséges, vagy a munkamenet lejárt.”

### Formátum
- JSON, camelCase mezőnevek. Az enumok stringek: a `role` értéke `"Owner"`, `"Editor"` vagy `"Viewer"`.
- Az időbélyegek UTC-ben vannak (`"2026-11-20T09:15:00.1234567Z"`).
- Az azonosítók GUID-ok. Ha az útvonalban nem GUID formájú azonosító áll (`{id:guid}` megkötés), a válasz általános
  **404** ProblemDetails, a „…nem található.” üzenet nélkül. Ilyen azonosítójú erőforrás nem is létezhet.

### Hibaválaszok
Minden hiba ProblemDetails (`application/problem+json`). A `detail` mező a felhasználónak szóló magyar üzenet.
Validációs hibánál (400) van `errors` is: mezőnév → üzenetek listája.

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "detail": "A megadott adatok érvénytelenek.",
  "errors": { "title": ["A cím megadása kötelező."] }
}
```

| Kód | Mikor |
|---|---|
| 400 | Validációs hiba (`detail`: „A megadott adatok érvénytelenek.”, mezőnként `errors`). Ha a kérés már a beolvasásnál elbukik (hibás JSON, üres törzs, rossz típusú mező, nem GUID query-paraméter), a `detail` és a mezőhöz tartozó üzenet is „A kérés formátuma érvénytelen.”; a mezőnév ilyenkor a JSON-útvonal (pl. `$.role`) vagy a paraméter neve (pl. `folderId`). Önmagaddal való megosztás. |
| 401 | Hiányzó, hibás vagy lejárt token; sikertelen belépés |
| 403 | „Nincs jogosultságod ehhez a művelethez.”: van hozzáférésed a jegyzethez, de ehhez a művelethez nincs elég jogod |
| 404 | Nem létező **vagy idegen** jegyzet/mappa (a két eset válasza azonos); nem létező megosztási címzett vagy megosztás |
| 405 | Létező útvonal, nem támogatott metódus (pl. `GET /api/folders/{id}`) |
| 409 | Foglalt felhasználónév, duplikált mappanév, ismételt megosztás |
| 500 | Váratlan hiba („Váratlan hiba történt.”) |

**Ellenőrzési sorrend módosító kérésnél (V18):** 1. a kérés validációja (400), 2. jogosultság (403/404), 3. módosítás.
Így pl. az olvasó érvénytelen törzsű `PUT` kérése 400-at kap, nem 403-at.

### Jogosultsági mátrix (jegyzetenként)

| Művelet | Tulajdonos | Szerkesztő | Olvasó | Idegen |
|---|---|---|---|---|
| Olvasás (`GET /api/notes/{id}`) | ✅ | ✅ | ✅ | 404 |
| Cím/szöveg módosítása (`PUT /api/notes/{id}`) | ✅ | ✅ | 403 | 404 |
| Törlés | ✅ | 403 | 403 | 404 |
| Mappába helyezés | ✅ | 403 | 403 | 404 |
| Megosztások listázása, megosztás, visszavonás | ✅ | 403 | 403 | 404 |

A mappák csak a tulajdonosukéi: idegen mappa minden végponton ugyanazt a 404-et adja, mint a nem létező.

### Válasz-DTO-k

| Név | Mezők |
|---|---|
| `AuthResponse` | `token`, `userName` |
| `MeResponse` | `id`, `userName` |
| `NoteSummary` | `id`, `title`, `updatedAt`, `role` (a hívó szerepe), `ownerUserName`, `folderId` |
| `NoteDetail` | a `NoteSummary` mezői + `content`, `createdAt` |
| `FolderSummary` | `id`, `name`, `noteCount` |
| `ShareInfo` | `userId`, `userName`, `role` (`"Viewer"` / `"Editor"`) |

A `folderId` mező mindig szerepel a válaszban. A tulajdonos a tényleges értéket látja (vagy `null`-t), a szerkesztő és
az olvasó mindig `null`-t, mert a mappák privátak (V24).

### Hosszkorlátok (`Notes.Api/Limits.cs`)
A hossz UTF-16 kódegységben értendő (`string.Length`, V3).

| Mező | Szabály |
|---|---|
| Felhasználónév | szóközök levágva, 3–32 karakter, csak angol betű, számjegy és `_ . -` |
| Jelszó | 8–128 karakter, legalább egy kisbetű, egy nagybetű és egy számjegy; nincs levágás |
| Jegyzet címe | szóközök levágva, 1–100 karakter |
| Jegyzet szövege | 0–10 000 karakter, kötelező mező (`""` megengedett, `null` nem); nincs levágás |
| Mappanév | szóközök levágva, 1–50 karakter, felhasználónként egyedi (kis- és nagybetűtől függetlenül, az ékezet számít) |

---

## Hitelesítés – `api/auth`

### `POST /api/auth/register`
Regisztráció; a felhasználó azonnal be is lép (V11). Hitelesítés nem kell.

Kérés:
```json
{ "userName": "anna", "password": "Titkos123" }
```
Válasz **201**:
```json
{ "token": "eyJhbGciOiJIUzI1NiIs...", "userName": "anna" }
```
Hibák:
- **400**: `errors.userName` / `errors.password`. Minden nem teljesülő szabály külön üzenetet ad, pl. „A felhasználónév
  3–32 karakter hosszú lehet.”, „A jelszónak legalább egy nagybetűt kell tartalmaznia.” Hiányzó mező: „A felhasználónév
  megadása kötelező.” / „A jelszó megadása kötelező.” Hibás JSON vagy üres törzs: „A kérés formátuma érvénytelen.”
- **409**: „Ez a felhasználónév már foglalt.” (kis- és nagybetűtől függetlenül: `Anna` = `anna`)

### `POST /api/auth/login`
Belépés. Hitelesítés nem kell. A felhasználónév kis- és nagybetűtől független, a körülötte álló szóközök nem számítanak.

Kérés: mint a regisztrációnál. Válasz **200**: `AuthResponse`, a `userName` a regisztráláskori írásmóddal.

Hibák:
- **401**: „Hibás felhasználónév vagy jelszó.” Ugyanez a válasz rossz jelszóra, ismeretlen felhasználóra, valamint
  hiányzó vagy üres mezőre is (V15), így a válaszból nem derül ki, létezik-e a felhasználó. Token nem készül.

### `GET /api/auth/me`
A token ellenőrzése. A token claim-jeiből válaszol, adatbázis-lekérdezés nélkül (V17).

Válasz **200**:
```json
{ "id": "3f2b8c1e-6a0d-4c57-9a2e-0b1d5e7f9a10", "userName": "anna" }
```
Hibák: **401** (hiányzó, hibás vagy lejárt token).

---

## Jegyzetek – `api/notes`

### `GET /api/notes`
A hívó **saját** jegyzetei, `updatedAt` szerint csökkenő sorrendben (egyezésnél a `createdAt` dönt, szintén
csökkenően). Minden elem `role` mezője `"Owner"`.

Opcionális szűrők (query):
- `?folderId=<guid>`: csak az adott saját mappa jegyzetei;
- `?unfiled=true`: csak a mappa nélküli jegyzetek. Az `unfiled=false` ugyanaz, mint a szűrő hiánya (V21).

Válasz **200**:
```json
[
  {
    "id": "8d6f0a52-1b3c-4e7d-9f21-6c4a0e5b7d33",
    "title": "Bevásárlás",
    "updatedAt": "2026-11-20T09:15:00.1234567Z",
    "role": "Owner",
    "ownerUserName": "anna",
    "folderId": "c1a7e9d4-2f68-4b0a-8e35-7d9c1b2a4f60"
  }
]
```
Hibák:
- **400** `errors.unfiled`: „A mappa szerinti és a mappa nélküli szűrés egyszerre nem adható meg.” (`folderId` +
  `unfiled=true` együtt)
- **400** `errors.folderId`: „A kérés formátuma érvénytelen.” (nem GUID `folderId`)
- **404**: „A mappa nem található.” (idegen vagy nem létező mappa a szűrőben, V20; nem üres lista)

### `GET /api/notes/shared`
A „Velem megosztott” lista: a hívóval megosztott jegyzetek, `updatedAt` szerint csökkenő sorrendben (egyezésnél a
`createdAt` dönt). A `role` a hívó szerepe (`"Editor"` vagy `"Viewer"`), a `folderId` mindig `null` (V24).

Válasz **200**:
```json
[
  {
    "id": "5e0c3b7a-9d24-4f18-a6b2-1c8e7f0d3a95",
    "title": "Közös terv",
    "updatedAt": "2026-11-20T08:40:12.5550000Z",
    "role": "Editor",
    "ownerUserName": "bela",
    "folderId": null
  }
]
```

### `GET /api/notes/{id}`
Egy jegyzet részletei. Jogosultság: tulajdonos, szerkesztő, olvasó.

Válasz **200** (`NoteDetail`):
```json
{
  "id": "8d6f0a52-1b3c-4e7d-9f21-6c4a0e5b7d33",
  "title": "Bevásárlás",
  "updatedAt": "2026-11-20T09:15:00.1234567Z",
  "role": "Owner",
  "ownerUserName": "anna",
  "folderId": null,
  "content": "tej, kenyér",
  "createdAt": "2026-11-20T09:00:00.0000000Z"
}
```
Hibák:
- **404**: „A jegyzet nem található.” (nem létező vagy idegen jegyzet: a két válasz törzse azonos, V1)

### `POST /api/notes`
Új jegyzet; a hívó lesz a tulajdonosa.

Kérés (a `folderId` elhagyható; ha megadod, a saját mappád kell legyen):
```json
{ "title": "Bevásárlás", "content": "tej, kenyér", "folderId": null }
```
Válasz **201** + `Location` fejléc (a jegyzet `GET` címe, pl. `http://localhost:5098/api/notes/8d6f…`) + `NoteDetail`.

Hibák:
- **400** `errors.title`: „A cím megadása kötelező.” (hiányzó, üres vagy csak szóközből álló cím) / „A cím legfeljebb
  100 karakter lehet.”
- **400** `errors.content`: „A szöveg megadása kötelező.” (hiányzó/`null`) / „A szöveg legfeljebb 10000 karakter lehet.”
- **404**: „A mappa nem található.” (idegen vagy nem létező mappa, vagy a mappát közben törölték)

Elutasított kérésnél a jegyzet nem jön létre.

### `PUT /api/notes/{id}`
Cím és szöveg módosítása. Jogosultság: tulajdonos, szerkesztő. Az `updatedAt` frissül. Egyidejű szerkesztésnél az
utolsó mentés érvényes (nincs verziókezelés).

Kérés:
```json
{ "title": "Bevásárlás (péntek)", "content": "tej, kenyér, alma" }
```
Válasz **200**: a módosított `NoteDetail` (a `role` a hívó szerepe).

Hibák:
- **400**: mint a `POST`-nál. A validáció a jogosultság előtt fut, ezért érvénytelen törzsre az olvasó és az idegen is
  400-at kap (V18). Elutasított kérésnél a tárolt jegyzet nem változik.
- **403**: olvasó
- **404**: „A jegyzet nem található.” (nem létező vagy idegen)

> **Dokumentált viselkedés:** a `PUT /api/notes/{id}` a törzsben küldött `folderId` mezőt **figyelmen kívül hagyja**
> (hiba nélkül). Mappát váltani csak a `PUT /api/notes/{id}/folder` végponttal lehet.

### `PUT /api/notes/{id}/folder`
A jegyzet áthelyezése egy saját mappába, vagy mappa nélküli állapotba (`null`). Jogosultság: tulajdonos. Az
`updatedAt` **nem** változik (V23). Egy jegyzet legfeljebb egy mappában lehet.

Kérés:
```json
{ "folderId": "c1a7e9d4-2f68-4b0a-8e35-7d9c1b2a4f60" }
```
(`{ "folderId": null }` vagy `{}` → mappa nélküli.)

Válasz **200**: a `NoteDetail` az új `folderId`-vel.

Hibák (ebben a sorrendben ellenőrizve, V25):
- **400**: „A kérés formátuma érvénytelen.” (nem GUID `folderId`; már a kérés beolvasásánál)
- **404**: „A jegyzet nem található.” (nem létező vagy idegen jegyzet)
- **403**: szerkesztő vagy olvasó (idegen mappa esetén is 403, így a mappa létezéséről nem tud meg semmit)
- **404**: „A mappa nem található.” (idegen, nem létező vagy közben törölt mappa)

### `DELETE /api/notes/{id}`
Jegyzet törlése, a megosztásaival együtt. Jogosultság: tulajdonos.

Válasz **204**, üres törzzsel.

Hibák: **403** (szerkesztő, olvasó; a jegyzet megmarad), **404** „A jegyzet nem található.”

---

## Megosztás – `api/notes/{id}/shares`

Mindhárom végpont csak a jegyzet tulajdonosának engedett. A szerkesztő és az olvasó **403**-at, az idegen **404**-et
(„A jegyzet nem található.”) kap.

### `GET /api/notes/{id}/shares`
A jegyzet megosztásai a címzett felhasználóneve szerint rendezve (V32).

Válasz **200**:
```json
[
  { "userId": "a9d3f6b2-0c71-4e8a-b5d4-2e6f1a7c9b08", "userName": "bela", "role": "Editor" },
  { "userId": "4b7e1c95-d382-4a6f-9e0b-8c2d5f3a1e76", "userName": "cecil", "role": "Viewer" }
]
```

### `POST /api/notes/{id}/shares`
Megosztás egy létező felhasználóval, felhasználónév alapján.

Kérés:
```json
{ "userName": "bela", "role": "Editor" }
```
A `role` értéke `"Viewer"` (olvasó) vagy `"Editor"` (szerkesztő), kis- és nagybetűtől függetlenül (`"viewer"` is jó).
A címzett neve is kis- és nagybetűtől független.

Válasz **201** + `Location: /api/notes/{id}/shares/{userId}` + `ShareInfo`:
```json
{ "userId": "a9d3f6b2-0c71-4e8a-b5d4-2e6f1a7c9b08", "userName": "bela", "role": "Editor" }
```

Hibák, az ellenőrzés sorrendjében (V30):
1. **400** `errors.userName`: „A felhasználónév megadása kötelező.” (hiányzó vagy üres) és/vagy `errors.role`: „A szerep
   csak „Viewer” (olvasó) vagy „Editor” (szerkesztő) lehet.” (minden más szöveg, pl. `"Owner"`, vagy hiányzó mező)
2. **403** (szerkesztő, olvasó) / **404** „A jegyzet nem található.” (idegen vagy nem létező jegyzet)
3. **404**: „Nincs ilyen nevű felhasználó.” (formailag érvénytelen név esetén is, V29)
4. **400** `errors.userName`: „A jegyzetet saját magaddal nem oszthatod meg.”
5. **409**: „A jegyzet már meg van osztva ezzel a felhasználóval.” (a meglévő szerep nem változik; szerepcsere:
   visszavonás és újramegosztás, V4)

Mivel a jogosultság a címzett keresése előtt dől el, a nem tulajdonos a válaszból nem tudja meg, létezik-e egy
felhasználónév.

> **Dokumentált viselkedés (V28):** a számként küldött szerep (`"role": 1`) a kérés beolvasásánál elbukik, ezért nem az
> `errors.role` üzenetet kapja, hanem az általános formátumhibát: 400, `detail` és `errors["$.role"]`: „A kérés
> formátuma érvénytelen.”

> **Dokumentált viselkedés (V31):** a `Location` fejléc a megosztást azonosítja, de ezen az útvonalon **nincs `GET`**,
> csak `DELETE`. A `GET` kérés 405-öt kap.

### `DELETE /api/notes/{id}/shares/{userId}`
Megosztás visszavonása. A `{userId}` a címzett azonosítója (`ShareInfo.userId`). A visszavonás azonnal hat: a címzett a
következő kérésnél már 404-et kap, a jegyzet a tulajdonosnál megmarad.

Válasz **204**, üres törzzsel.

Hibák: **403** / **404** (jogosultság, lásd fent); **404**: „A megosztás nem található.” (a jegyzet nincs megosztva
ezzel a felhasználóval).

---

## Mappák – `api/folders`

Csak a hívó saját mappái. Átnevezés nincs (V7).

### `GET /api/folders`
A saját mappák a bennük lévő jegyzetek számával, magyar ábécérendben, kis- és nagybetűtől függetlenül (`hu-HU`,
memóriában rendezve, V22).

Válasz **200**:
```json
[
  { "id": "c1a7e9d4-2f68-4b0a-8e35-7d9c1b2a4f60", "name": "Ábel", "noteCount": 2 },
  { "id": "7f3e2d1c-5b4a-4968-8776-655443322110", "name": "Munka", "noteCount": 0 }
]
```

### `POST /api/folders`
Új mappa.

Kérés:
```json
{ "name": "Munka" }
```
Válasz **201** + `Location: /api/folders/{id}` + `FolderSummary` (`noteCount: 0`).

Hibák:
- **400** `errors.name`: „A mappa nevének megadása kötelező.” / „A mappa neve legfeljebb 50 karakter lehet.”
- **409**: „Ilyen nevű mappád már van.” (kis- és nagybetűtől függetlenül: `Munka` = `MUNKA`; az ékezet viszont
  számít, az `Árvíztűrő` és az `Arvizturo` két külön név)

> **Dokumentált viselkedés (V26):** a `Location` fejléc a létrehozott mappát azonosítja, de ezen az útvonalon **nincs
> `GET`**, csak `DELETE`. A `GET` kérés 405-öt kap.

### `DELETE /api/folders/{id}`
Saját mappa törlése. A benne lévő jegyzetek megmaradnak, és mappa nélküliek lesznek (adatbázis: `ON DELETE SET NULL`).
Ha a jegyzet meg volt osztva, a címzett továbbra is eléri.

Válasz **204**, üres törzzsel.

Hibák: **404** „A mappa nem található.” (idegen vagy nem létező mappa).

---

## Fejlesztői végpont

### `GET /openapi/v1.json`
Az ASP.NET Core által generált OpenAPI-leírás. Csak `Development` környezetben érhető el, hitelesítés nélkül.
(A `dotnet run --launch-profile http` Developmentben indít.)

## Végpontok összesítve

| Metódus | Útvonal | Jogosultság | Siker |
|---|---|---|---|
| POST | `/api/auth/register` | nyilvános | 201 `AuthResponse` |
| POST | `/api/auth/login` | nyilvános | 200 `AuthResponse` |
| GET | `/api/auth/me` | bejelentkezett | 200 `MeResponse` |
| GET | `/api/notes` | bejelentkezett (saját jegyzetek) | 200 `NoteSummary[]` |
| GET | `/api/notes/shared` | bejelentkezett | 200 `NoteSummary[]` |
| GET | `/api/notes/{id}` | tulajdonos, szerkesztő, olvasó | 200 `NoteDetail` |
| POST | `/api/notes` | bejelentkezett | 201 `NoteDetail` + `Location` |
| PUT | `/api/notes/{id}` | tulajdonos, szerkesztő | 200 `NoteDetail` |
| PUT | `/api/notes/{id}/folder` | tulajdonos | 200 `NoteDetail` |
| DELETE | `/api/notes/{id}` | tulajdonos | 204 |
| GET | `/api/notes/{id}/shares` | tulajdonos | 200 `ShareInfo[]` |
| POST | `/api/notes/{id}/shares` | tulajdonos | 201 `ShareInfo` + `Location` |
| DELETE | `/api/notes/{id}/shares/{userId}` | tulajdonos | 204 |
| GET | `/api/folders` | bejelentkezett (saját mappák) | 200 `FolderSummary[]` |
| POST | `/api/folders` | bejelentkezett | 201 `FolderSummary` + `Location` |
| DELETE | `/api/folders/{id}` | a mappa tulajdonosa | 204 |
| GET | `/openapi/v1.json` | nyilvános, csak Development | 200 |

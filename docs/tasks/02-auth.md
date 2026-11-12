# 02. feladat: Regisztráció, belépés, JWT, hibakezelés

## Cél
Működő regisztráció és belépés JWT-vel, központi hibakezeléssel (ProblemDetails), és egy
teszt-segéd, amellyel a későbbi tesztek hitelesített klienst kapnak.

## Előfeltétel
01 kész: build és teszt zöld, entitások, `Limits`, `Text.Normalize`, `TestApiFactory`.
A `dotnet-ef` helyi eszközként van a repó gyökerében (`dotnet-tools.json`): ha nem fut, `dotnet tool restore`.

## Olvasd el először
- `docs/design.md`: 3. Hitelesítés, 5. REST API (auth végpontok, hibakezelés), 6. Validáció, V5, V6, V9, V11, V12
- `docs/spec.md`: 1. elfogadási feltétel (belépés, hibás jelszó)

## Hatókör
### SZEREPEL
- `Errors/`: `ValidationFailedException` (mezőnév → üzenetek), `NotFoundException`, `ForbiddenException`,
  `ConflictException` + központi `IExceptionHandler` → ProblemDetails (a `detail` magyar üzenet; validációnál `errors` is);
  401 esetén is ProblemDetails
- `AuthService`: regisztráció, belépés, `PasswordHasher<User>`, JWT kiállítás
- JWT Bearer konfiguráció (`Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiresMinutes` az appsettings-ben; `Jwt:Key` user-secrets/környezeti változó;
  hiányzó kulcs → induláskor hiba); alapértelmezett authorize: minden végpont védett, kivéve az auth végpontokat
- Fejlesztői JWT-kulcs: futtasd a `dotnet user-secrets init --project Notes.Api` parancsot, majd írd le a fejlesztőnek
  a beállító parancsot (`dotnet user-secrets set "Jwt:Key" "<legalább 32 bájtos véletlen érték>" --project Notes.Api`),
  és várd meg, amíg jelzi, hogy beállította. A kulcs értékét ne te generáld és ne írd ki.
- Aktuális felhasználó kinyerése (pl. `ClaimsPrincipal` extension vagy `ICurrentUser`)
- Végpontok: `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/auth/me`
- Regisztrációs validáció a design 6. szakasza szerint; foglalt (normalizált) név → 409; a felhasználónevet `Trim()` után tároljuk,
  a megjelenített `UserName` az eredeti (trimmelt) írásmód
- Teszt-segéd: `AuthHelper` (regisztrál egyedi felhasználót, visszaad egy `HttpClient`-et beállított Bearer fejléccel)
- `TestApiFactory` kiegészítése a tesztkulccsal. A kulcs a konfigurációból jön; ha a `Program.cs`-beli közvetlen olvasás nem
  felülírható a `WebApplicationFactory`-ből, használj halasztott options-kötést vagy környezeti változót. Dokumentáld a választott megoldást egy kommentben.

- `Program.cs` middleware-sorrend rendbetétele (01-ből örökölt sablonmaradvány): `UseAuthentication()` a
  `UseAuthorization()` ELŐTT, a központi hibakezelő a pipeline elején. `UseHttpsRedirection()` csak
  Development környezeten kívül fusson, mert a Vite proxy HTTP-n hívja a backendet, és az átirányítás
  megtörné (V14).

### NEM szerepel
- Jegyzet, mappa, megosztás végpontok; `NoteAccessService`; frontend
- Refresh token, jelszó-visszaállítás, belépési korlátozás

## Elvárt kimenetek
- `AuthController`, `AuthService`, `Dtos/Auth*`, `Errors/*`, módosított `Program.cs`, `appsettings.json`, `AuthHelper`

## Kötött döntések
V5 (jelszó 8–128, kisbetű + nagybetű + számjegy, speciális karakter nem kötelező), V6 (felhasználónév szabályok), V9 (60 perc), V11 (regisztráció 201 + token), V12 (`/me`),
belépési hiba: ugyanaz az üzenet rossz jelszóra és ismeretlen felhasználóra. A tokenben nincs szerep.

## Tesztek (`Notes.Tests/AuthTests.cs`)
- Sikeres regisztráció: 201, token, a token működik a `/me`-n
- Foglalt felhasználónév (kis/nagybetű eltéréssel is) → 409
- Hibás regisztrációs adatok `[Theory]`: túl rövid/hosszú név, tiltott karakter → 400 `errors`-szal
- Jelszószabályok `[Theory]`: 7 karakter ❌, 8 karakter minden szabállyal ✅, 128 ✅, 129 ❌; nincs kisbetű ❌,
  nincs nagybetű ❌, nincs számjegy ❌; ékezetes nagybetűvel („Árvíz123”) ✅; speciális karakter nélkül ✅;
  több hiba egyszerre → mindegyik szabály külön üzenetként szerepel az `errors.password` alatt;
  elutasításkor a felhasználó nem jön létre
- Sikeres belépés: 200 + token
- Rossz jelszó → 401, nincs token; ismeretlen felhasználó → 401, a hibaüzenet azonos a rossz jelszóéval
- Token nélkül `/me` → 401; hibás/lejárt token → 401
- Jelszó nem tárolódik nyers formában (hash ≠ jelszó)

## Kész, ha
- [ ] `dotnet build` és `dotnet test` zöld
- [ ] a `Jwt:Key` nincs a repóban (`git grep` ellenőrzés)
- [ ] a hibaválaszok ProblemDetails formátumúak
- [ ] `docs/progress.md` 02 kipipálva

## Megállj és kérdezz, ha
- a JWT-kulcs tesztbe juttatására nincs működő megoldás
- új csomag kellene a JWT Bearer-en kívül

## Commit
`Add registration, login, JWT auth and central error handling`

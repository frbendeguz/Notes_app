# 08. feladat: Frontend – megosztás

## Cél
Megosztás kezelése a tulajdonosnak, és a megosztott jegyzetek szerep szerinti megjelenítése a címzettnek.

## Előfeltétel
07 kész.

## Olvasd el először
- `docs/design.md`: 4. Jogosultsági mátrix, 5. REST API (shares), 7. Frontend, V4, V10
- `docs/spec.md`: 5., 6., 7. elfogadási feltétel

## Hatókör
### SZEREPEL
- A 06-ban jóváhagyott vizuális irány folytatása (V35: aktadosszié, Courier Prime, a `@theme` tokenekkel); új
  megjelenési elem a meglévő tokenekre épüljön.
- Megosztás ablak/panel a tulajdonosnak: felhasználónév + szerep (Olvasó/Szerkesztő) megadása, a jelenlegi megosztások listája, visszavonás
- Hibák érthető megjelenítése: nem létező felhasználó (404), önmaga (400), ismételt megosztás (409)
- „Velem megosztott” nézet: lista és megnyitás; a jegyzeten látszik a szerep és a tulajdonos neve
- Szerep szerinti UI: Olvasónál a szerkesztő csak olvasható, nincs Mentés/Törlés/Megosztás; Szerkesztőnél a cím és a szöveg módosítható, de nincs Törlés/Megosztás/Mappába helyezés
- Ha egy megnyitott jegyzet közben elérhetetlenné válik (404 a visszavonás miatt vagy 403), a kliens érthető üzenetet ad és frissíti a listát
- A kliens szerepfüggő tiltása csak kényelmi réteg: a szerver 403-át is kezelni kell

### NEM szerepel
- Szerepmódosítás (V4), valós idejű értesítés a megosztásról

## Kész, ha
- [ ] `npm run build`, `npm run lint` és `npm test` hibamentes
- [ ] a kézi ellenőrzés lépései megírva a `docs/testing/manual/08.md` fájlba: számozott, reprodukálható lépések
      PowerShell-parancsoknál a JSON törzset UTF-8 bájtokként küldd (`[Text.Encoding]::UTF8.GetBytes(...)`,
      `-ContentType "application/json; charset=utf-8"`), mert a Windows PowerShell 5.1 a szöveges törzset Latin-1
      kódolással küldi, és az ékezetes betűk miatt a kérés 400-at kap (a 06. kézi próbájában derült ki).
      elvárt eredménnyel (a böngészős próbát a fejlesztő végzi, az agent nem tud böngészőt kezelni). Lefedi:
      két böngészőprofillal: megosztás Olvasóként → csak olvas; Szerkesztőként → módosít, de nem töröl/oszt;
      visszavonás után a frissítéskor eltűnik; nem létező felhasználó, önmaga, ismétlés hibaüzenettel
- [ ] `dotnet test` továbbra is zöld
- [ ] `docs/progress.md` 08 kipipálva; a fejlesztő a commit előtt végrehajtja a kézi lépéseket, és az eredményt
      a `docs/testing/manual/08.md`-be írja (hiba esetén új munkamenetben javítjuk)

## Commit
`Add sharing UI and role-based views to frontend`

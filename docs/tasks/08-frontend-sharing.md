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
- Megosztás ablak/panel a tulajdonosnak: felhasználónév + szerep (Olvasó/Szerkesztő) megadása, a jelenlegi megosztások listája, visszavonás
- Hibák érthető megjelenítése: nem létező felhasználó (404), önmaga (400), ismételt megosztás (409)
- „Velem megosztott” nézet: lista és megnyitás; a jegyzeten látszik a szerep és a tulajdonos neve
- Szerep szerinti UI: Olvasónál a szerkesztő csak olvasható, nincs Mentés/Törlés/Megosztás; Szerkesztőnél a cím és a szöveg módosítható, de nincs Törlés/Megosztás/Mappába helyezés
- Ha egy megnyitott jegyzet közben elérhetetlenné válik (404 a visszavonás miatt vagy 403), a kliens érthető üzenetet ad és frissíti a listát
- A kliens szerepfüggő tiltása csak kényelmi réteg: a szerver 403-át is kezelni kell

### NEM szerepel
- Szerepmódosítás (V4), valós idejű értesítés a megosztásról

## Kész, ha
- [ ] `npm run build` és `npm run lint` hibamentes
- [ ] kézi próba két böngészőprofillal (írd le): megosztás Olvasóként → csak olvas; Szerkesztőként → módosít, de nem töröl/oszt;
      visszavonás után a frissítéskor eltűnik; nem létező felhasználó, önmaga, ismétlés hibaüzenettel
- [ ] `dotnet test` továbbra is zöld
- [ ] `docs/progress.md` 08 kipipálva

## Commit
`Add sharing UI and role-based views to frontend`

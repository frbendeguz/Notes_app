# 07. feladat: Frontend – szerkesztő és mappák

## Cél
A tulajdonos létrehozhat, szerkeszthet, törölhet jegyzetet, kezelheti a mappáit, és mappába helyezheti a jegyzeteit.

## Előfeltétel
06 kész.

## Olvasd el először
- `docs/design.md`: 5. REST API (notes, folders), 6. Validáció, 7. Frontend
- `docs/spec.md`: 2., 3., 4., 8. elfogadási feltétel

## Hatókör
### SZEREPEL
- Új jegyzet létrehozása; ha mappa nézetben vagyunk, abba kerül (`POST` `folderId`-val)
- Szerkesztő nézet: cím és szöveg, karakterszámlálók (x/100, x/10 000), Mentés, Törlés (megerősítéssel)
- A szerver hibaüzenetének (`detail`, `errors`) megjelenítése; sikertelen mentés után a szerkesztő ne veszítse el a beírt szöveget
- Kliensoldali segítő validáció (üres/szóközös cím, hosszkorlát) – a szerveri validáció marad a mérvadó
- Mappák az oldalsávban: lista (`noteCount`-tal; a sorrendet az API adja magyar ábécé szerint, a kliens ne rendezze újra, V22), létrehozás, törlés (megerősítéssel; a jegyzetek megmaradnak), szűrés mappa szerint és „Mappa nélküli”
- Jegyzet mappába helyezése / mappából kivétele (`PUT /api/notes/{id}/folder`)
- Mentés/törlés után a lista és a mappaszámlálók frissülnek (query invalidálás)

### NEM szerepel
- Megosztás UI, a szerepek szerinti tiltás a megosztott jegyzeteken (08)
- Mappa átnevezése (V7)

## Kész, ha
- [ ] `npm run build` és `npm run lint` hibamentes
- [ ] a kézi ellenőrzés lépései megírva a `docs/testing/manual/07.md` fájlba: számozott, reprodukálható lépések
      elvárt eredménnyel (a böngészős próbát a fejlesztő végzi, az agent nem tud böngészőt kezelni). Lefedi:
      létrehozás → lista; szerkesztés; törlés; 100/101 karakteres cím és 10 000/10 001 karakteres szöveg;
      üres cím hibaüzenettel; duplikált mappanév hibaüzenettel; mappatörlés után a jegyzetek „Mappa nélküli” alatt vannak
- [ ] `dotnet test` továbbra is zöld
- [ ] `docs/progress.md` 07 kipipálva; a fejlesztő a commit előtt végrehajtja a kézi lépéseket, és az eredményt
      a `docs/testing/manual/07.md`-be írja (hiba esetén új munkamenetben javítjuk)

## Commit
`Add note editor and folder management to frontend`

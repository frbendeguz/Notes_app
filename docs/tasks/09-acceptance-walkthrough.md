# 09. feladat: Elfogadási feltételek ellenőrzése és tesztjelentés

## Cél
Bizonyíték arra, hogy a spec mind a 8 elfogadási feltétele teljesül: minden feltételhez hozzárendelt
ellenőrzési mód (automatizált teszt vagy reprodukálható kézi lépések), és a házi feladat által
kötelezően bemutatandó tesztesetek megjelölése.

## Előfeltétel
01–08 kész.

## Olvasd el először
- `docs/spec.md` (elfogadási feltételek és bizonytalanságok), `docs/design.md` 8–10. szakasz
- `docs/ai/ai-naplo.md` (a korábbi feladatok elbukó tesztjei és megjegyzései)

## Hatókör
### SZEREPEL
- `docs/testing/acceptance.md`, feltételenként (AC1–AC8) egy szakasz:
  - **Ellenőrzési mód:** automatizált teszt(ek) `Osztály.Metódus` alakban, VAGY kézi ellenőrzés
    számozott, reprodukálható lépésekkel és elvárt eredménnyel (a tisztán vizuális dolgokra, pl. a
    „Velem megosztott” lista megjelenése, hibaüzenet a felületen)
  - **Kategória:** normál / határeset / hibás bemenet / jogosulatlan művelet / regresszió
  - **Eredmény:** teljesül / részben / nem teljesül, a futás hivatkozásával
- „Kötelező esetek” táblázat az `acceptance.md` elején, a pontozási útmutató szerint, konkrét tesztnévvel:
  legalább **1 normál eset**, **2 érdemi határeset** (pl. cím 100/101, szöveg 10 000/10 001),
  **1 hibás bemenet vagy jogosulatlan művelet** (pl. olvasó szerkesztési kísérlete → 403, változatlan
  jegyzet) és **1 regressziós eset**. Regressziós esetnek most jelölj ki egy tesztet, amely egy korábbi
  feladat működését védi egy későbbi feladat változtatásaival szemben (pl. mappatörlés után a
  megosztás továbbra is működik), és írd le, mit véd. A változtatási igénynél ez a lista bővül.
- Ha egy feltételhez nincs automatizált teszt, és programozott szabályról van szó, írd meg a hiányzót
  (csak tesztet, termékkódot nem). A „másik eszközről belépés” (AC2) tesztje: ugyanaz a fiók két
  független klienssel.
- Ha a tesztek hibát találnak, ne javítsd ki csendben: írd a `docs/progress.md` megjegyzései közé,
  mentsd el a kimenetet (`docs/testing/runs/`), és kérdezz.
- A teljes futás mentése: `dotnet test --logger "console;verbosity=normal" > docs/testing/runs/09-teljes-futas.txt`,
  valamint `npm run build` és `npm run lint` eredménye az `acceptance.md` végére.
- A `docs/design.md` V1–V13 döntéseinek ellenőrzése: megvalósult-e mindegyik úgy, ahogy le van írva
  (eltérés → lista a `docs/progress.md`-ben).

### NEM szerepel
- Új funkció, refaktor, a `docs/spec.md` módosítása
- A hibás–javított futás bemutatása (10. feladat), README (11. feladat)

## Kész, ha
- [ ] `docs/testing/acceptance.md` mind a 8 feltételt lefedi ellenőrzési móddal és eredménnyel
- [ ] a „Kötelező esetek” táblázatban mind az 5 kategória konkrét teszttel szerepel
- [ ] `docs/testing/runs/09-teljes-futas.txt` létezik, `dotnet test` zöld, `npm run build` és `npm run lint` hibamentes
- [ ] az eltérések és nyitott kérdések listája a `docs/progress.md`-ben
- [ ] MI-napló bejegyzés (fázis: tesztelés), `docs/progress.md` 09 kipipálva

## Commit
`Add acceptance test report and fill test gaps`

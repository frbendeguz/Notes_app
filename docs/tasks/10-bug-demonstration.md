# 10. feladat: Hibafeltárás bemutatása (elbukó és javított futás)

## Cél
Futtatással igazolni, hogy legalább egy teszt egy ismert hibás változaton elbukik, a javított
változaton pedig átmegy, és megmagyarázni a hibát és az elvárt működést. (Pontozás: „Tesztelés és
hibafeltárás”, 5 pont.)

## Előfeltétel
09 kész.

## Olvasd el először
- `docs/ai/ai-naplo.md` „Elbukó tesztek valódi hiba miatt” sorai és a `docs/testing/runs/` tartalma
- `docs/testing/acceptance.md`

## Hatókör
### SZEREPEL
Két út van; a választást a tervben indokold, és kérd a fejlesztő jóváhagyását.

**A) Valódi fejlesztési hiba** (ha a 01–09. feladat során volt ilyen, és megvan az elbukó és az átmenő futás kimenete):
- `docs/testing/hibafeltaras.md`: a hiba leírása, az elvárt működés (melyik AC / design szabály),
  a tesztek neve, a hibás kódrészlet (a hibás commit vagy diff hivatkozásával) és a javítás,
  a két futás kimenetére mutató hivatkozás.

**B) Szándékosan hibássá tett változat** (ha nem volt alkalmas valódi hiba):
- Válassz egy egyértelmű, egy soros hibát egy lényeges szabályban, amelyet egy meglévő teszt lefed
  (pl. a címhossz ellenőrzésében `>` helyett `>=`, így a pontosan 100 karakteres cím is elbukik; vagy az
  olvasó szerepnél hiányzó jogosultság-ellenőrzés).
- Hozd létre a hibát, futtasd a releváns teszte(ke)t, mentsd a kimenetet:
  `docs/testing/runs/10-szandekos-hiba-elbukik.txt`; mentsd a hibát patch-fájlként:
  `git diff > docs/testing/10-szandekos-hiba.patch`.
- Állítsd vissza a helyes kódot (ez a lépés rákérdez), futtasd újra, mentsd:
  `docs/testing/runs/10-szandekos-hiba-atmegy.txt`.
- `docs/testing/hibafeltaras.md`: a fájl elején egyértelműen: **„Szándékosan hibássá tett változat,
  nem valódi fejlesztési hiba.”** Utána: a beültetett hiba, miért ezt a szabályt választottuk, az elvárt
  működés, a teszt neve, miért bukik el (a hibaüzenet értelmezése), a két futás hivatkozása, és hogyan
  reprodukálható (`git apply docs/testing/10-szandekos-hiba.patch`).

Mindkét esetben: a `docs/testing/acceptance.md` „Kötelező esetek” részéből hivatkozz a `hibafeltaras.md`-re.

### NEM szerepel
- A szándékos hiba bennhagyása a kódban (a végén a kód a helyes állapotban van, a teljes tesztkészlet zöld)
- Szándékos hiba feltüntetése valódi fejlesztési hibaként

## Kész, ha
- [ ] `docs/testing/hibafeltaras.md` elkészült, és a két futás kimenete a `docs/testing/runs/` alatt van
- [ ] a hibás futásban a kiválasztott teszt elbukik, a javítottban átmegy (a kimenetek ezt mutatják)
- [ ] a teljes `dotnet test` zöld, a kód nem tartalmazza a szándékos hibát (`git diff` csak dokumentációt mutat)
- [ ] MI-napló bejegyzés (fázis: tesztelés), `docs/progress.md` 10 kipipálva

## Commit
`Document failing and fixed test run`

# 07b. feladat: A 07. kézi próbájában talált két hiba javítása

## Cél
A 07. feladat kézi ellenőrzésében két lépés elbukott (25. és 31.). Mindkettőt ki kell javítani; a 31-es
hibára előbb automatizált frontend-teszt készül, amely a hibás változaton elbukik, és csak utána jön a
javítás. Ez a hiba lesz a 10. feladat „valódi fejlesztési hibája” (V48).

## Előfeltétel
- A 07. feladat a két hibával együtt commitolva van (ez a „hibás változat”). Írd be a commit hash-ét a naplóba.
- `npm run build`, `npm run lint` és `dotnet test` zöld.

## A két hiba (a fejlesztő kézi próbája alapján)
- **25. lépés:** mappa nézetben egy jegyzet mappáját „(nincs mappa)”-ra állítva és mentve a jegyzet nyitva marad,
  de a bal oldalon a régi mappa nézete marad kiválasztva, és a jegyzet már nincs a listában.
- **31. lépés:** leállított backend mellett a Mentés után a szerkesztő helyén csak „A szerver nem érhető el.
  Ellenőrizd, hogy fut-e a backend.” üzenet és egy Bezárás gomb látszik. A backend újraindítása után a begépelt
  szöveg elveszett, így menteni sem lehet. Ez sérti a szabályt: „sikertelen mentésnél a beírt szöveg megmarad”.

## Olvasd el először
- `docs/design.md` 7. szakasz (különösen a „Szerkesztő” blokk), V39, V42, V44, valamint V46–V48
- `docs/testing/manual/07.md` 25. és 31. lépés
- `.claude/rules/frontend.md`

## Hatókör
### SZEREPEL
1. **Diagnózis (a tervben, kód előtt):** mindkét hiba pontos oka fájl:sor hivatkozással. A 31-esnél azt is írd le,
   melyik lépésben vész el a beírt szöveg (pl. a szerkesztő komponens lecsatolódik, vagy az állapot felülíródik).
2. **Tesztkörnyezet (V48):** fejlesztői függőségként `vitest`, `@testing-library/react`, `jsdom` (más csomag
   csak engedéllyel). A `vitest` beállítása a `vite.config.ts`-ben (`test.environment: "jsdom"`), `"test": "vitest run"`
   script a `package.json`-ban. Globális tesztfüggvények nélkül: a tesztek a `vitest`-ből importálnak. A tesztfájlok
   a forrás mellett vannak (`*.test.tsx`). A `npm run build` és a `npm run lint` a tesztfájlokkal is hibamentes maradjon.
3. **Teszt a 31-es hibára, a javítás ELŐTT:** a szerkesztőben (valós `QueryClientProvider`-rel) a felhasználó
   módosítja a szöveget, a mentés hálózati hibával vagy törzs nélküli 5xx-szel bukik el (a `fetch` mockolva).
   Elvárás: a szerkesztő látható marad, a beírt szöveg megvan, a hibaüzenet a szerkesztőn belül jelenik meg; majd a
   `fetch` sikeresre váltása után a Mentés újra megnyomható, és a kérés törzse a beírt szöveget tartalmazza.
   - Futtasd a hibás kódon, és mentsd: `npm test -- <tesztfájl> > ../docs/testing/runs/07b-<tesztnév>-elbukik.txt`.
     A kimenetben az elbukásnak a **hiányzó szöveg vagy szerkesztő** miatti állításon kell történnie, nem beállítási
     vagy importhibán. Ha nem így van, javítsd a tesztet, és futtasd újra, mielőtt a termékkódhoz nyúlsz.
4. **A 31-es hiba javítása (V47):** a mentési hiba a szerkesztőn belül jelenjen meg, a szerkesztő és a beírt szöveg
   maradjon meg, és a backend újraindítása után a Mentés újra sikerüljön. A teszt fusson át; mentsd:
   `../docs/testing/runs/07b-<tesztnév>-atmegy.txt`.
5. **A 25-ös hiba javítása (V46):** mentés után, ha a jegyzet mappája megváltozott, és a jelenlegi nézet egy mappa vagy
   a „Mappa nélküli”, a nézet váltson arra, ahol a jegyzet most van (az új mappa, vagy „Mappa nélküli”, ha nincs
   mappa); a jegyzet megnyitva marad. Az „Összes” nézetben nincs váltás. Ha a nézet kiszámítása tiszta függvénybe
   kerül, arra írj egységtesztet.
6. **Kézi lépések:** a `docs/testing/manual/07.md` 25. és 31. lépésének elvárt eredménye a javított viselkedés
   szerint; a fejlesztő korábbi eredménysorait hagyd meg, és jelöld, hogy újrafuttatásra várnak.

### NEM szerepel
- A 08. feladat elemei (megosztott jegyzet megnyitása, szerep szerinti tiltás), egyéb refaktor, backend-módosítás
- `beforeunload` (V42 marad)
- Tesztek a frontend más részeire (csak a két hibához kapcsolódók)

## Kész, ha
- [ ] a diagnózis a naplóban, fájl:sor hivatkozással
- [ ] `docs/testing/runs/07b-…-elbukik.txt` a hibás kódon az állításon bukik el; `…-atmegy.txt` a javítás után zöld
- [ ] `npm test`, `npm run build`, `npm run lint` hibamentes; `dotnet test` zöld
- [ ] a 07.md 25. és 31. lépése frissítve
- [ ] MI-napló bejegyzés (fázis: tesztelés), benne a hibás változat commit hash-e, „Döntési eset jelölt?” sorban a
      10. feladatra való utalással; `docs/progress.md` 07b kipipálva

## Megállj és kérdezz, ha
- a 31-es hiba oka nem a szerkesztő állapotának elvesztése, hanem valami más (pl. a backend viselkedése)
- a teszt csak a környezet erős átalakításával (új csomagok, globális beállítás) írható meg

## Commit
`Fix editor draft loss on failed save and folder view after move` (a mentett futások és a tesztkörnyezet is ebbe kerül)

# 11. feladat: README, API-leírás és MI-használat leírása (2. mérföldkő)

## Cél
A 2. mérföldkő („első működő verzió”) dokumentációja: indítási és tesztfuttatási útmutató, API-leírás,
és az MI-használat általános leírása. Ezután a fejlesztő rögzíti az első verziót (`v1.0` címke).

## Előfeltétel
10 kész.

## Olvasd el először
- `docs/spec.md`, `docs/design.md` 1., 3., 5. szakasz, `docs/testing/acceptance.md`, `docs/testing/hibafeltaras.md`
- `docs/ai/00-tervezes-osszefoglalo.md`, `docs/ai/ai-naplo.md`, `docs/claude-code-utmutato.md`
- `CLAUDE.md` és a `.claude/` tartalma

## Hatókör
### SZEREPEL
- **`README.md`** (a repó gyökerében, magyarul):
  - a projekt egy bekezdésben, a stack, a mappaszerkezet röviden
  - előfeltételek (pontos .NET és Node verzió a projektfájlokból)
  - **indítási útmutató** lépésenként tiszta klónból: `dotnet tool restore` (helyi `dotnet-ef`), JWT-kulcs
    beállítása user-secrets-szel (a parancs, érték nélkül), adatbázis (migráció), backend indítása,
    frontend telepítése és indítása, a használt portok
  - **tesztfuttatási útmutató:** `dotnet test`, egy teszt szűrése, `npm run build` / `npm run lint`,
    hol vannak a mentett futások
  - hivatkozások: spec, design, tesztjelentés, hibafeltárás, API-leírás, MI-használat, MI-napló
  - ismert korlátok (a spec bizonytalanságai: nincs valós idejű szinkron, last write wins stb.)
- **`docs/api.md`**: a végpontok a MEGVALÓSÍTOTT kódból (nem a design-ból): metódus, útvonal,
  jogosultság, kérés és válasz példa, hibakódok. Ha a kód eltér a `design.md` 5. szakaszától, sorold fel
  az eltéréseket a `docs/progress.md`-ben (a design-t ne módosítsd).
- **`docs/ai/mi-hasznalat.md`** (a README-ből hivatkozva):
  - választott eszköz és modell, felület; munkamódszer röviden (egy feladat = egy munkamenet, terv →
    jóváhagyás → megvalósítás → ellenőrzés → reviewer → fejlesztői átnézés)
  - **az MI-fejlesztést támogató elemek a kódbázisban**, mindegyiknél: mi a szerepe, és hol látszik a
    használata (pl. a napló melyik bejegyzésében blokkolt a hook, mit talált a reviewer):
    `CLAUDE.md`, `.claude/rules/`, `.claude/settings.json`, `.claude/hooks/`, `.claude/skills/task`,
    `.claude/skills/change-request`, `.claude/agents/task-reviewer.md`, `docs/tasks/`, valamint a
    projektbe telepített külső pluginok és skillek (forrás, cél, hol látszik a használatuk)
  - MI-használat fázisonként (tervezés, megvalósítás, tesztelés; később változtatási igény), mindegyiknél
    bizonyítékra mutató hivatkozással (napló bejegyzés, teszt, futás)
  - a módszer korlátai (a naplóban és a 0. pontban tapasztaltak alapján)
  - „Döntési esetek” szakasz: hivatkozás a `docs/ai/dontesi-esetek.md`-re (a végleges hármat a fejlesztő
    választja ki és írja meg; te legfeljebb jelölteket javasolj a naplóból, fázisonként egyet)

### NEM szerepel
- A döntési esetek végleges szövege (a fejlesztő írja; a védésen magyaráznia kell)
- Kódváltoztatás (ha az útmutató írása közben hibát találsz, jelezd)

## Kész, ha
- [ ] a README indítási lépései egy tiszta klónon végrehajthatók: a te ellenőrzésed `dotnet build`,
      `dotnet test`, `npm ci`, `npm run build`; a fejlesztő tiszta klónból kipróbálja (írd le neki a lépéseket)
- [ ] a README minden hivatkozása létező fájlra mutat
- [ ] `docs/api.md` minden megvalósított végpontot tartalmaz
- [ ] `docs/ai/mi-hasznalat.md` minden felsorolt elemnél konkrét használati bizonyítékra hivatkozik
- [ ] MI-napló bejegyzés (fázis: dokumentáció), `docs/progress.md` 11 kipipálva

## Utána (a fejlesztő teendője)
A commit és push után: `git tag -a v1.0 -m "Első működő verzió"` és `git push origin v1.0`. A repó
legyen publikus. Ez a „rögzített első verzió”, ebből indul a változtatási igény.

## Commit
`Add README, API reference and AI usage overview`

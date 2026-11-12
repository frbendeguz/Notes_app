---
name: task
description: Egy feladat elindítása a docs/tasks/ alól a projekt munkamódszere szerint (terv, jóváhagyás, megvalósítás, ellenőrzés, beszámoló). Csak akkor, ha a fejlesztő a /task NN paranccsal kéri.
argument-hint: <feladatszám, pl. 01>
disable-model-invocation: true
---

# Feladat indítása: $ARGUMENTS

A fejlesztő a(z) **$ARGUMENTS** számú feladatot kérte. Kizárólag ezt az egy feladatot dolgozd ki.

## 0. Ellenőrzés
- Keresd meg a `docs/tasks/$ARGUMENTS-*.md` fájlt. Ha nincs ilyen, vagy több is van, állj meg és kérdezz.
- Nézd meg a `docs/progress.md`-t: ha a feladat előfeltétele (az előző feladat) nincs kipipálva,
  jelezd, és kérdezd meg, folytassuk-e.
- `git status`: ha van nem commitolt változás, jelezd, mielőtt bármit tervezel.

## 1. Felkészülés (csak olvasás)
- Olvasd el a teljes feladatfájlt.
- Olvasd el a `docs/design.md` és a `docs/spec.md` azon szakaszait, amelyeket a feladat „Olvasd el először”
  része felsorol. A többit csak akkor, ha a feladat megértéséhez szükséges.
- Nézd át a meglévő kódot, amelyre a feladat épül.

## 2. Terv (a jóváhagyásig nincs fájlmódosítás)
Mutasd be a tervet ebben a szerkezetben:
1. **Megértés:** a feladat célja 1–2 mondatban, a saját szavaiddal.
2. **Fájlok:** létrehozandó és módosítandó fájlok listája, mindegyikhez egy sor arról, mi kerül bele.
3. **Sorrend:** a lépések sorrendje (a teszteket a hozzájuk tartozó kóddal együtt).
4. **Tesztek:** a tervezett tesztesetek listája, a feladat „Tesztek” része alapján.
5. **Csomagok / migráció:** új csomag vagy migráció kell-e (csak a feladatban engedélyezett).
6. **Nyitott kérdések:** bármi, ami a feladatban, a design-ban vagy a spec-ben nem egyértelmű.
   Ha van ilyen, a kérdéseket tedd fel, és ne találj ki rájuk választ.
Ezután várd meg a jóváhagyást.

## 3. Megvalósítás (jóváhagyás után)
- Kövesd a jóváhagyott tervet. Ha menet közben el kell térni tőle, állj meg, és írd le, miért.
- Tartsd be a feladat „NEM szerepel” listáját és a `.claude/rules/` szabályait.
- Ha egy hook vagy jogosultsági szabály blokkol valamit, ne próbáld más úton megkerülni: jelezd.
- Ha egy lépés a fejlesztő közreműködését igényli (pl. titok beállítása), írd le a pontos parancsot,
  és várj, amíg jelzi, hogy megcsinálta.
- **Valódi hiba megőrzése:** ha egy már megírt teszt a termékkód hibája miatt bukik el (nem azért, mert
  a kód még nincs kész), a javítás ELŐTT mentsd el a futás kimenetét a
  `docs/testing/runs/NN-<tesztnév>-elbukik.txt` fájlba, a javítás után pedig a
  `docs/testing/runs/NN-<tesztnév>-atmegy.txt` fájlba (pl. `dotnet test --filter <név> > fájl`).
  Írd le a naplóban, mi volt a hiba. Ez a hibafeltárás bemutatásának bizonyítéka lehet.

## 4. Ellenőrzés
- Futtasd le a feladat „Kész, ha” listájának minden ellenőrizhető pontját (build, teszt, lint stb.).
- Ha valami nem teljesül, javítsd. Ha három próbálkozás után sem sikerül, állj meg, és írd le a helyzetet.
- **MI-napló:** másold a `docs/ai/ai-naplo.md` végén lévő sablont a fájl végére, és töltsd ki
  tényszerűen (terv és a fejlesztő módosításai, eltérések, blokkolt műveletek, elbukó tesztek,
  ellenőrzések eredménye). A „Reviewer” sort a következő lépés után töltsd ki; a „Fejlesztői döntés” és
  az „Ellenőrzés (fejlesztő)” sort hagyd üresen, azt a fejlesztő tölti ki. Ne szépíts: a napló a beadandó
  dokumentáció nyersanyaga.
- Ezután indítsd el a `task-reviewer` subagentet ezzel: „Ellenőrizd a(z) $ARGUMENTS. feladatot.”
  A megállapításait vagy javítsd, vagy (ha nem értesz velük egyet) indokold a beszámolóban, és írd be
  őket a napló „Reviewer” sorába.

## 5. Beszámoló és lezárás
- Pipáld ki a feladatot a `docs/progress.md`-ben, de csak ha minden „Kész, ha” pont teljesül.
  A felmerült nyitott kérdéseket és javaslatokat írd a „Megjegyzések / nyitott kérdések” részbe.
- Beszámoló a fejlesztőnek (tömören):
  - mi készült el (fájlok szintjén),
  - a „Kész, ha” pontok eredménye egyenként (teljesül / nem teljesül, bizonyítékkal: pl. tesztek száma),
  - a reviewer megállapításai és mit tettél velük,
  - eltérések a tervtől, nyitott kérdések, javaslatok (pl. CLAUDE.md vagy design módosítása),
  - javasolt commit üzenet (a feladatfájl „Commit” része alapján),
  - **Felkészülés a védésre:** 2–4 pont arról, mit érdemes a fejlesztőnek ebből a feladatból mélyen
    megértenie (fájl:sor hivatkozással), és egy-egy kérdés, amire a védésen MI nélkül tudnia kell
    válaszolni (pl. „Mit ad vissza a PUT, ha az olvasó üres címmel próbál menteni, és miért?”).
- Ne commitolj, amíg a fejlesztő nem kéri.

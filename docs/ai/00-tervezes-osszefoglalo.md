# 0. Tervezés és az agent-munkakörnyezet kialakítása (MI-támogatással)

> Fejlesztési fázis: **követelményelemzés és tervezés**. Ez a fejlesztés első MI-támogatott fázisa;
> a megvalósítás (01–08. feladat) és a tesztelés (09–10. feladat) a következő kettő.

| | |
|---|---|
| Eszköz | Claude (claude.ai webes felület), modell: Claude Opus 5.5 |
| Időszak | 2026. november, a specifikáció beadása után, a megvalósítás előtt |
| Átadott kontextus | a beadott specifikáció teljes szövege; később a házi feladat követelményeit tartalmazó PDF |
| Kimenet | `docs/design.md`, `docs/tasks/01–11`, `CLAUDE.md`, `.claude/` (szabályok, jogosultságok, hookok, skillek, subagent) |

## Mit csináltunk, lépésenként

1. **Tervjavaslat a specifikációból.** Az MI a spec alapján architektúrát, adatmodellt, jogosultsági
   mátrixot, REST API-t, validációs szabályokat, frontend-vázlatot, tesztelési tervet és megvalósítási
   sorrendet javasolt, valamint összegyűjtötte, mit hagy nyitva a spec (később V1–V13 döntések).
   *Fejlesztői döntés:* a sorrendet elfogadtam.
2. **Az első lépés kidolgozása.** Az MI először kódszinten dolgozta ki (entitások, DbContext, tesztek).
   *Fejlesztői döntés:* elvetettem, mert tervszintű részletezést kértem, nem kódot. Az új változat
   döntéstáblát (D1–D8), feladatbontást függőségekkel, sémateszt-tervet, kockázatokat és „kész”-kritériumot
   tartalmaz. Tanulság: a kódot az agent írja majd; a tervezési fázisban a döntéseket kell rögzíteni.
3. **Az agent bevonásának módszere.** Hogyan adagoljuk a munkát (egy feladat = egy munkamenet = egy
   commit, előbb terv, kimondott hatókör és kizárások, ellenőrizhető „kész” feltétel), milyen sablonnal
   írjuk a feladatokat, mi kerüljön az agent utasításfájljába, és milyen fájlt használnak más agentek
   (`AGENTS.md`). Az agent-specifikus állításokat az MI a hivatalos Claude Code dokumentáció alapján,
   webes kereséssel ellenőrizte (pl. a Claude Code a `CLAUDE.md`-t olvassa, az `AGENTS.md` importálható).
4. **Agent-független csomag.** *Fejlesztői döntés:* mivel az agent még nem volt eldöntve, előbb
   agent-független változat készült (`AGENTS.md` mint fő fájl + `CLAUDE.md` import), 9 feladatfájllal.
5. **Átállás Claude Code-ra.** *Fejlesztői döntés:* a Claude Code lett a választott agent. A csomag
   átalakult a Claude Code saját mechanizmusaira:
   - rövid `CLAUDE.md` + útvonalhoz kötött szabályok (`.claude/rules/`: backend, teszt, frontend),
   - jogosultságok (`.claude/settings.json`): engedélyezett, rákérdező és tiltott parancsok; plan alapmód,
   - hookok Node-ban: a spec, a design, a migrációk és a titkok védelme (szerkesztésre és shell-parancsra
     is), valamint a haladás betöltése a munkamenet elején,
   - `/task NN` skill a feladatonkénti munkafolyamathoz és `task-reviewer` subagent a független ellenőrzéshez.
6. **Fejlesztői kérdések.** GitHub-repó és indítás (VS Code bővítmény, tartaléknak a CLI), valamint
   a dokumentáció és a változásnapló elkészítése. *Fejlesztői döntés:* README futtatási útmutatóval,
   API-leírás, MI-napló az első feladattól, és ez az összefoglaló mint 0. pont.
7. **Igazítás a házi feladat követelményeihez.** A követelmény-PDF alapján bekerült: tesztkategóriák
   (normál, két határeset, hibás bemenet/jogosulatlan művelet, regresszió), a hibás–javított tesztfutás
   bemutatása (10. feladat), README és MI-leírás (11. feladat), mérföldkövek, a rögzített első verzió
   címkézése, a változtatási igény munkafolyamata (`/change-request`) és a döntési esetek sablonja.
8. **Az értelmezési döntések átnézése.**
   - *V1 (idegen felhasználó → 404):* jóváhagytam.
   - *V5 (jelszó):* az MI csak 8–128 karakteres hosszt javasolt. Kiegészítettem: legalább egy kisbetű,
     egy nagybetű és egy számjegy kell, speciális karakter nem kötelező. Az MI ezt pontosította
     (Unicode kis- és nagybetű, így az „Á” is számít; minden nem teljesülő szabály külön hibaüzenet),
     és bekerült a 02. feladat tesztjei közé.
   - *V13 (token tárolása):* az MI `localStorage`-ot javasolt. Felvetettem a `sessionStorage`-t, hogy a
     böngésző bezárásával a token is megszűnjön. Az MI elemezte a következményeket: a kliens a lap
     bezárásakor elfelejti a tokent, de új lapon is újra be kell lépni; a token a szerveren a lejáratáig
     érvényes marad; az XSS-kockázatot a tárolóváltás nem csökkenti (az ellen a `HttpOnly` cookie védene,
     de az CSRF-védelmet és több munkát igényel). Ezek ismeretében a `sessionStorage` mellett döntöttem.

## Hogyan ellenőriztük az MI javaslatait

- **Dokumentáció:** a Claude Code-ra vonatkozó formátumokat (beállítások, hookok, skillek, subagentek,
  szabályfájlok) az MI a hivatalos dokumentációval vetette össze. Két ismert bizonytalanság maradt,
  ezeket az első futtatáskor kell ellenőrizni (`docs/claude-code-utmutato.md`): a szabályfájlok
  `paths:` fejlécének betöltése, és a `/task` indítása a `disable-model-invocation` beállítással.
- **Hookok:** a két védő hookot 21 szimulált esettel futtattuk le (tiltandó és engedélyezett műveletek,
  relatív és abszolút út, nagybetűs írásmód, hibás bemenet); mind a várt eredményt adta.
- **Konfiguráció:** a `settings.json` JSON-ként, a skillek, a subagent és a szabályfájlok fejléce
  YAML-ként érvényes.
- **Első futtatás a saját gépen (2026-11-12, Claude Code 2.1.322, VS Code bővítmény, Windows):** a
  `/doctor` jelentése szerint minden konfigurációs fájl hibátlan, a szabályfájlok `paths` szerint, a skillek
  csak paranccsal töltődnek be, a `task-reviewer` subagent felismert, az állandó kontextusterhelés kb.
  1k token, a SessionStart hook 204 ms alatt fut le. A diagnosztika közben a `curl` hívást a projekt
  tiltólistája blokkolta, ami azt mutatja, hogy a jogosultsági szabályok érvényesülnek. Eltérés a tervtől:
  a `/agents` varázsló megszűnt, ezért a subagent ellenőrzése az útmutatóban módosult.

## A módszer korlátai, amiket ebben a fázisban tapasztaltunk

- Az utasításfájlok (`CLAUDE.md`, szabályok) csak kontextust adnak, nem kényszerítenek. Ami nem sérülhet,
  azt jogosultsági szabály vagy hook védi; a Bash-hook viszont heurisztika, nem teljes védelem.
- **Incidens:** az MI munkaterülete a beszélgetés közben újraindult, ezért egy fájlíró parancs nem a
  projektmappában, hanem a munkaterület gyökerében futott le. Az MI által javasolt takarító törlést
  megállítottam, amíg nem kaptam magyarázatot arról, mi és miért került oda; a magyarázat után
  jóváhagytam. Tanulság: az MI által kezdeményezett törlő vagy visszafordíthatatlan műveletet ellenőrizni
  kell. A projekt beállítása ugyanezt az elvet követi: a törlő és visszaállító parancsok rákérdeznek
  vagy tiltottak.

## Döntési eset jelöltek ebből a fázisból

A végleges három döntési esetet a `docs/ai/dontesi-esetek.md` tartalmazza; ezek a jelöltek akkor
válnak teljessé, amikor a bizonyító teszt elkészült.

| Jelölt | MI-javaslat | Bizonyíték, amely majd igazolja |
|---|---|---|
| Kis/nagybetűfüggetlen egyediség | Normalizált oszlop egyedi indexszel, nem SQLite `NOCASE` (az nem kezeli az ékezeteket) | 01. feladat sématesztje „Árvíztűrő” / „árvíztűrő” párral |
| Idegen felhasználó válasza | 404 a 403 helyett, hogy ne derüljön ki a jegyzet létezése (a spec mindkettőt engedi) | 03. és 05. feladat jogosultsági tesztjei (azonos válasz idegenre és nem létezőre) |
| Védelem utasítás helyett | A spec/design/migrációk védelme hookkal és jogosultsággal, nem csak a `CLAUDE.md` szövegével | A hookok 21 esetes próbája; a fejlesztés közben blokkolt kísérletek a naplóban |
| Jelszószabály (módosított javaslat) | Csak hosszkorlát; a fejlesztő kis-/nagybetű- és számkövetelménnyel egészítette ki | 02. feladat jelszószabály-tesztjei (határok, hiányzó karakterosztályok, ékezetes nagybetű) |
| Token tárolása (módosított javaslat) | `localStorage`; a fejlesztő `sessionStorage`-t választott a következmények ismeretében | 06. feladat kézi próbája (lap bezárása után újra belépés), a frontend-szabály |

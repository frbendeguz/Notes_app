# MI-használat

Ez a dokumentum leírja, hogyan használtuk az MI-t a jegyzetkezelő fejlesztésében: milyen eszközzel, milyen
munkamódszerrel, milyen támogató elemekkel a kódbázisban, és ezek hol látszanak. Minden állításnál a bizonyítékra
hivatkozunk. A részletes, feladatonkénti nyersanyag az [MI-napló](ai-naplo.md) (a továbbiakban: napló, a bejegyzés
számával, pl. „napló 04”).

Állapot: v1.0 (2. mérföldkő). A változtatási igény fázisát a 3. mérföldkőnél egészítjük ki.

## Eszköz, modell, felület

| Fázis | Eszköz | Modell | Felület |
|---|---|---|---|
| Követelményelemzés és tervezés (0. pont) | Claude (claude.ai) | Claude Opus 5.5 | webes csevegőfelület |
| Megvalósítás, tesztelés, dokumentáció (01–11) | Claude Code (első futtatáskor 2.1.322) | Claude Opus 5.5 | VS Code bővítmény, Windows |

Forrás: [`00-tervezes-osszefoglalo.md`](00-tervezes-osszefoglalo.md), a napló „Eszköz” sorai.

## Munkamódszer

A munkát kisebb, előre megírt feladatokra bontottuk (`docs/tasks/NN-*.md`). Egy feladat egy Claude Code-munkamenet
(`/clear`, majd `/task NN`), és egy commit. A menet:

1. **Terv:** a munkamenet plan módban indul. Az agent beolvassa a feladatot és a hivatkozott design-szakaszokat, majd
   tervet ad: fájlok, sorrend, tesztek, nyitott kérdések. A nem egyértelmű pontokat kérdésként teszi fel, nem dönt
   helyettünk.
2. **Jóváhagyás:** a fejlesztő a kérdésekre választ ad, módosíthat a terven, és csak ezután engedi a fájlmódosítást.
3. **Megvalósítás:** a feladat „NEM szerepel” listáján belül, a projekt szabályai szerint.
4. **Ellenőrzés:** a feladat „Kész, ha” pontjai (build, teszt, lint), valamint MI-napló bejegyzés.
5. **Reviewer:** a független `task-reviewer` subagent átnézi a változást. A megállapításait az agent javítja, vagy
   indoklással elveti.
6. **Fejlesztői átnézés:** a fejlesztő átnézi a diffet, maga is lefuttatja a teszteket, a felületi feladatoknál a kézi
   lépéseket, és kitölti a napló „Fejlesztői döntés” és „Ellenőrzés (fejlesztő)” sorát. A commit csak ezután készül.

Példa egy terv fejlesztői módosítására: napló 03, ahol a lista szűrése a fejlesztő kérésére a `NoteAccessService`-be
került. Példa saját futtatásra: napló 01 („…hogy megbizonyosodjak arról, hogy … nem csak az agent hallucinált”, 8/8).

## Az MI-fejlesztést támogató elemek a kódbázisban

| Elem | Szerepe | Hol látszik a használata |
|---|---|---|
| [`CLAUDE.md`](../../CLAUDE.md) | Az agent állandó projektutasítása: stack, parancsok, alapszabályok (rétegek, hibakódok, titkok), nyelv, munkamódszer. Minden munkamenet elején betöltődik. | A napló minden „Kontextus” sorában szerepel. Az utolsó szabályát (fájlműveletekhez a dedikált eszközöket kell használni, ne shell-parancsot) a fejlesztő a 08 után vette fel, mert a Bash-hook ismételten blokkolt (napló 08, „Korlát (hook)”). A „ha ugyanazt a hibát kétszer javítom” szabály alapján került a rövid tesztnevek tilalma a tesztelési szabályok közé (napló 02, fejlesztői döntés). |
| [`.claude/rules/`](../../.claude/rules/) (`backend.md`, `testing.md`, `frontend.md`) | Útvonalhoz kötött konvenciók (`paths:` fejléc). Csak akkor töltődnek be, ha az agent a `Notes.Api/`, `Notes.Tests/` vagy `notes-web/` alatti fájlhoz nyúl, így nem terhelik a kontextust feleslegesen. | A betöltést a fejlesztő a `.claude/logs/instructions.log`-ban ellenőrizte (`path_glob_match`, napló 02, „Ellenőrzés (fejlesztő)”). A reviewer a `backend.md` V19-es szabályára hivatkozva találta meg a `ShareService` megosztás-lekérdezéseit, ezek a `NoteAccessService`-be kerültek (napló 05). A `frontend.md` miatt vette fel az agent a `"strict": true`-t a `tsconfig.app.json`-ba (napló 06). A `testing.md` „mellékhatás hiánya” szabálya alapján egészítette ki a reviewer a teszteket (napló 02, 05). |
| [`.claude/settings.json`](../../.claude/settings.json) | Jogosultságok: engedélyezett (build, teszt, `npm ci`, olvasó git), rákérdező (csomag hozzáadása, `user-secrets`, `git add/commit/checkout`) és tiltott (`git push`, `rm -rf`, `curl`, `wget`, a spec és a design szerkesztése, `.env` és `secrets.json` olvasása) parancsok. Plan alapmód. A hookok bekötése. Az `enabledPlugins` a projektbe telepített pluginnal. | A `/doctor` futtatása közben a `curl` hívást a tiltólista blokkolta (00, „Első futtatás”). A `dotnet user-secrets init` rákérdezett, a fejlesztő elutasította és maga futtatta, a JWT-kulcsot is ő állította be (napló 02). A backend indítását és `curl`-os kipróbálását a jogosultság elutasította (napló 02, 06), ezért a felületi próbák kézi lépésekként a fejlesztőhöz kerültek (`docs/testing/manual/`). |
| [`.claude/hooks/`](../../.claude/hooks/) | Node-szkriptek, amelyek az agent döntésétől függetlenül futnak. `protect-files.mjs`: tiltja a spec, a design, a meglévő migrációk és a titokfájlok szerkesztését. `guard-bash.mjs`: shell-paranccsal is tiltja ugyanezt (heurisztika: védett útvonal + írásra utaló jel). `session-context.mjs`: a munkamenet elején a haladást és a git állapotot az agent kontextusába adja. `log-instructions.mjs`: naplózza, melyik utasításfájl mikor töltődött be. | A két védő hookot a tervezéskor 21 szimulált esettel próbáltuk ki (00, „Hogyan ellenőriztük”). A `guard-bash` a fejlesztés közben nyolcszor blokkolt: napló 02 (2), 06 (2), 07 (1), 08 (2), valamint ennek a feladatnak az első olvasó parancsát (napló 11). Mindegyik olvasó vagy naplóíró parancs volt, amelynek a szövegében a design neve szerepelt; a védett fájl módosítására irányuló kísérlet nem volt köztük. A `protect-files` blokkolásáról nincs naplózott eset. A `session-context` kimenete minden munkamenet elején megjelenik (pl. „Haladás (docs/progress.md): 12/13 számozott feladat kész.”); első futtatáskor 204 ms alatt futott le (00). A `log-instructions` használata: napló 02, fejlesztői ellenőrzés. |
| [`.claude/skills/task`](../../.claude/skills/task/SKILL.md) | A `/task NN` parancs: a fenti munkamódszer lépései (ellenőrzés, felkészülés, terv szerkezete, megvalósítás, a valódi hibák futásainak mentése, „Kész, ha”, napló, reviewer, beszámoló „Felkészülés a védésre” ponttal). | Minden 01–11 bejegyzés ennek a szerkezetét követi. A „valódi hiba megőrzése” lépés eredménye: `docs/testing/runs/07b-failed-save-keeps-draft-elbukik.txt` / `…-atmegy.txt` (napló 07b). |
| [`.claude/skills/change-request`](../../.claude/skills/change-request/SKILL.md) | A `/change-request` parancs: a változtatási igény hatásvizsgálata, spec v2.0 és design javaslat, új feladatfájlok, kód nélkül, a `v1.0` címkéből indulva. | Még nem használtuk: a változtatási igény a v1.0 után érkezik (`docs/progress.md`, „Változtatási igény”). |
| [`.claude/agents/task-reviewer.md`](../../.claude/agents/task-reviewer.md) | Független ellenőr subagent: csak olvas és ellenőrzést futtat; a feladatfájllal, a design-nal és a szabályokkal veti össze a változást. | Érdemi találatok: a `SqliteErrorCode 19` minden constraint-hibát 409-re fordított, ezért `2067`-re szűkült (napló 02); a mappalista BINARY rendezése miatt a fejlesztő `hu-HU` rendezésre váltott (napló 04, V22); a leállt backend üres 500-ja miatt nem jelent meg a „szerver nem érhető el” üzenet (napló 06); a már nyitott jegyzetre kattintva elveszett a „nem mentett” jelzés (napló 07); elérhetetlen saját jegyzetnél aktív maradt a Törlés (napló 08). Gyakori adminisztratív blokkolója: a `progress.md` hiányzó pipája (napló 01, 02, 07, 07b, 08, 10). |
| [`docs/tasks/`](../tasks/) | Feladatfájlok azonos szerkezetben: cél, előfeltétel, „Olvasd el először”, SZEREPEL / NEM szerepel, tesztek, „Kész, ha”, commit üzenet. A tervezési fázisban készültek (00). | A „NEM szerepel” lista betartása: pl. a 06-ban és a 07-ben nem készült frontend-teszt, mert nem szerepelt a feladatban (napló 06, 07). A 07b-t a fejlesztő a 07 kézi próbájában talált két hiba miatt vette fel (napló 07, „Ellenőrzés (fejlesztő)”). |
| **Külső plugin: `frontend-design`** | Forrás: `frontend-design@anthropic-plugin-directory`, projekt hatókörrel telepítve (`.claude/settings.json`, `enabledPlugins`). Cél: egységes, átgondolt vizuális irány a három frontend-feladathoz. A projekt szabályai (`frontend.md`: Tailwind, komponenskönyvtár nélkül) az erősebbek. | Napló 06 és 07 („Kontextus”: `frontend-design` skill). Az első javasolt irányt („vonalas füzet”) a fejlesztő elvetette, és saját irányt adott („aktadosszié”, Courier Prime). A plugin bevonásának indoka: napló 06, fejlesztői döntés (V33). Ezen kívül más külső skill vagy plugin nincs a projektben. |

A beállítások fejlesztőnek szóló leírása: [`docs/claude-code-utmutato.md`](../claude-code-utmutato.md).

## MI-használat fázisonként

### Követelményelemzés és tervezés
A claude.ai felületen az MI a specifikációból tervet javasolt (architektúra, adatmodell, jogosultsági mátrix, REST API,
validáció, tesztelési terv), és összegyűjtötte a spec nyitott kérdéseit. Ezekből lettek a V1–V13 értelmezési döntések.
Ugyanitt készült el az agent munkakörnyezete (`CLAUDE.md`, `.claude/`, feladatfájlok).
A fejlesztő módosításai, például: a kódszintű első kidolgozást elvetette, és tervszintűt kért helyette; a jelszószabályt
kis- és nagybetű-, valamint számjegy-követelménnyel egészítette ki (V5); `localStorage` helyett `sessionStorage`-ot
választott (V13).
Bizonyíték: [`00-tervezes-osszefoglalo.md`](00-tervezes-osszefoglalo.md), [`docs/design.md`](../design.md),
`AuthTests` (jelszószabály), `docs/testing/manual/06.md` 13–14. lépés (token a lap bezárása után).

### Megvalósítás (01–08)
A Claude Code a feladatonkénti terv jóváhagyása után írta a backendet (01–05) és a frontendet (06–08). A fejlesztő
minden feladatnál döntött a nyitott kérdésekben; ezek V14–V52 néven bekerültek a design-ba. Példák, ahol a fejlesztő
eltért a javaslattól: a lista szűrése a `NoteAccessService`-be került (napló 03); kis- és nagybetűtől független szerep
(napló 05, V28); a mappaváltás a Mentéshez kötött (napló 07, V39).
Bizonyíték: a napló 01–08 bejegyzései; a tesztszám növekedése 8 → 55 → 121 → 162 → 218 (napló „Ellenőrzés” sorai);
`Notes.Tests/`, `notes-web/src/**/*.test.tsx`.

### Tesztelés (07b, 09, 10)
- **07b:** a fejlesztő kézi próbája két hibát talált a 07-ben (napló 07, „Ellenőrzés (fejlesztő)”). Az agent
  diagnosztizálta az okot fájl:sor szinten, a javítás előtt megírta a tesztet, és elmentette a hibás és a javított
  futást (`docs/testing/runs/07b-*`).
- **09:** az agent a spec AC1–AC8 feltételeit a meglévő `// ACn` tesztekhez rendelte, két hiányt talált, és pótolta őket
  (AC2: két kliens; regressziós teszt). Elkészült az [`acceptance.md`](../testing/acceptance.md) (`dotnet test`
  220/220, `npm test` 23/23, `docs/testing/runs/09-*`).
- **10:** a valódi hibát a mai kódban is újra előidéztük, és elmentettük a futásokat és a visszaállító patch-et:
  [`hibafeltaras.md`](../testing/hibafeltaras.md), `docs/testing/runs/10-*`.

### Dokumentáció (11)
Az agent a README-t, az [API-leírást](../api.md) (a megvalósított kódból) és ezt a dokumentumot írta. Az indítási
lépéseket a HEAD tiszta klónján ellenőrizte (napló 11). A végső próbát tiszta klónból a fejlesztő végzi.

### Változtatási igény
Később, a `/change-request` paranccsal (3. mérföldkő).

## A módszer korlátai

- **Az utasítás nem kényszerít.** A `CLAUDE.md` és a szabályok csak kontextust adnak. Ami nem sérülhet, azt
  jogosultsági szabály vagy hook védi (00, „Korlátai”).
- **A Bash-hook heurisztika, és túl szigorú.** Nyolcszor blokkolt ártalmatlan olvasó vagy naplóíró parancsot, mert a
  szövegében szerepelt a design neve. A fejlesztő ezt szándékosan így hagyta („inkább legyen szigorú, mint engedékeny”,
  napló 07), és a `CLAUDE.md`-be felvette a dedikált eszközök használatát (napló 08).
- **Az agent nem futtatta az alkalmazást.** A backend indítását és a `curl`-t a jogosultság tiltja, ezért böngészős
  vagy élő API-próbát az agent nem végzett. A felületi hibákat (07: 25. és 31. lépés) a fejlesztő kézi próbája találta,
  nem az agent és nem a reviewer.
- **A le nem futtatott, MI által írt lépések hibásak lehetnek.** Az agent PowerShell-parancsai kétszer hibásan
  működtek Windows PowerShell 5.1 alatt (kódolás, a hibatörzs kiolvasása; napló 06, 08). Ez csak a kézi végrehajtáskor
  derült ki.
- **Pontatlan hivatkozás.** Az agent egy diagnózisban több fájl összefűzött listájának sorszámát vette át (`main.tsx:176`);
  a reviewer találta meg (napló 07b). A sorhivatkozásokat ellenőrizni kell.
- **A reviewer is MI.** Ugyanazzal a modellel fut, és gyakran adminisztratív hiányt (pipa, napló) jelez blokkolóként.
  Érdemi hibát is talált (lásd a táblázatot), de nem helyettesíti a fejlesztő saját futtatását és kézi próbáját.
- **Visszafordíthatatlan műveletek.** A tervezéskor az MI munkaterülete újraindult, és egy fájl rossz helyre került.
  A javasolt törlést a fejlesztő csak magyarázat után engedte (00). Az `npm audit fix --force` törő változást jelzett
  előre, ezért a fejlesztő a futtatás után ellenőrizte a verziókat (napló 07b, „Függőségek biztonsága”, V49).

## Döntési esetek

A három döntési eset: [`dontesi-esetek.md`](dontesi-esetek.md). A végleges hármat a fejlesztő választja ki és írja meg.
Jelöltek fázisonként (a naplóból és a 0. pontból):

| Fázis | Jelölt | Miért érdemi | Bizonyíték |
|---|---|---|---|
| Tervezés | A token tárolása: `sessionStorage` (V13) | Az MI `localStorage`-ot javasolt; a fejlesztő a következmények elemzése után módosította. | 00, 8. pont; `notes-web/src/api.ts`; `manual/06.md` 13–14. lépés |
| Megvalósítás | A mappalista rendezése (V22) | A fejlesztő először az egyszerű adatbázisbeli rendezést választotta, majd a reviewer észrevétele után magyar ábécérendre módosította. | Napló 04; `FoldersTests.ListFolders_MixedCaseAndAccentedNames_HungarianAlphabeticalOrder` |
| Tesztelés | Sikertelen mentésnél elveszett a beírt szöveg (V47) | A hibát a fejlesztő kézi próbája találta, nem az MI; a teszt a hibás változaton elbukik, a javítotton átmegy. | Napló 07, 07b, 10; [`hibafeltaras.md`](../testing/hibafeltaras.md); `runs/07b-*`, `runs/10-*` |

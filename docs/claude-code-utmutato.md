# Claude Code használati útmutató (a fejlesztőnek)

Ez a fájl neked szól, nem az agentnek. Leírja, mit csinálnak a beállítások, és hogyan dolgozz velük.

## A fájlok szerepe

| Fájl | Mire való | Mikor töltődik be |
|---|---|---|
| `CLAUDE.md` | Projektleírás, parancsok, alapszabályok, munkamódszer | Minden munkamenet elején |
| `.claude/rules/backend.md` | Backend-konvenciók, migrációk | Ha Claude a `Notes.Api/` alatti fájlt olvas |
| `.claude/rules/testing.md` | Tesztelési szabályok | Ha Claude a `Notes.Tests/` alatti fájlt olvas |
| `.claude/rules/frontend.md` | Frontend-konvenciók | Ha Claude a `notes-web/` alatti fájlt olvas |
| `.claude/settings.json` | Jogosultságok (allow/ask/deny), plan alapmód, hookok | Indításkor |
| `.claude/hooks/*.mjs` | Védelmek, amelyek Claude döntésétől függetlenül hatnak; a betöltött szabályok naplózása | A megfelelő eseménynél |
| `.claude/skills/task/SKILL.md` | A `/task NN` parancs: a feladat-munkafolyamat | Ha beírod: `/task 01` |
| `.claude/skills/change-request/SKILL.md` | A `/change-request` parancs: hatásvizsgálat, spec v2.0 javaslat, új feladatok | Ha beírod, a változtatási igény után |
| `.claude/agents/task-reviewer.md` | Független ellenőr a feladat végén | A `/task` munkafolyamat hívja |
| `docs/ai/` | MI-napló, döntési esetek, a tervezési fázis összefoglalója | Az agent írja a naplót, te a döntéseket |
| `docs/testing/` | Tesztjelentés, hibafeltárás, mentett futások | 09–10. feladat |
| `CLAUDE.local.md.example` | Minta a személyes beállításokhoz | Átnevezve `CLAUDE.local.md`-re töltődik be |

A `CLAUDE.md` és a szabályfájlok **kontextust** adnak, nem kényszerítenek. Ami nem sérülhet, azt a
jogosultsági szabályok és a hookok védik.

## Mit véd a beállítás, és hogyan

| Védett dolog | Jogosultsági szabály | Hook |
|---|---|---|
| `docs/spec.md`, `docs/design.md` | `Edit` tiltva | `protect-files.mjs` (szerkesztés), `guard-bash.mjs` (shell-paranccsal írás) |
| Meglévő migrációk, `ModelSnapshot` | – | `protect-files.mjs`, `guard-bash.mjs` |
| `.env`, `secrets.json` | `Read` tiltva | `protect-files.mjs` |
| Új csomag, user-secrets, migráció törlése | `ask` (rákérdez) | – |
| `git add` / `commit` | `ask` (rákérdez) | – |
| `git push`, `rm -rf`, `curl`, `wget` | tiltva | – |

A `guard-bash.mjs` heurisztika: blokkol minden shell-parancsot, amely a védett fájlt említi és írásra
utal (`>`, `sed -i`, `rm`, `mv`, `cp`, `Set-Content`…). Emiatt egy ártalmatlan `cp docs/spec.md máshova`
is blokkolódik; ez szándékos, a hibaüzenet elmondja Claude-nak, mi a teendő.

A munkamenet **plan módban** indul (`"defaultMode": "plan"`): Claude olvashat és tervezhet, de a terv
jóváhagyásáig nem módosít fájlt. Ha ez egy kisebb kérésnél zavar, a Shift+Tab billentyűvel válthatsz módot.

## Első beállítás

1. Másold a csomag tartalmát a repó gyökerébe, és commitold (`docs/`, `.claude/`, `CLAUDE.md`, `.gitignore`).
2. Előfeltételek: .NET SDK, Node.js (a hookok Node-ban futnak), Git. Windows-on a Claude Code a Git for
   Windows bash-ét használja a parancsokhoz és a hookokhoz.
3. Indítsd a Claude Code-ot a repó gyökerében: `claude`.
4. Ellenőrizd:
   - `/permissions` – látszanak-e az allow/ask/deny szabályok
   - `/hooks` – látszik-e a SessionStart és a két PreToolUse hook
   - a `task-reviewer` subagent: kérdezd meg Claude-ot („Milyen subagentek érhetők el ebben a projektben?”),
     vagy nézd meg a `/doctor` jelentését (a `/agents` varázsló megszűnt; a subagent maga a
     `.claude/agents/task-reviewer.md` fájl)
   - a `/` menüben látszik-e a `task` parancs
   - `/doctor` – jelez-e hibás vagy figyelmen kívül hagyott szabályt
5. A munkamenet elején Claude-nak tudnia kell a haladásról (ezt a SessionStart hook adja). Kérdezd meg
   tőle: „Melyik a következő feladat?” – ha a 01-et mondja, a hook működik.
6. A szabályfájlok csak akkor töltődnek be, amikor Claude a `Read`, `Write` vagy `Edit` eszközzel egy
   illeszkedő fájlhoz nyúl (pl. a `Notes.Api/` alatt). Hogy betöltődtek-e, azt a
   `.claude/logs/instructions.log` mutatja: az `InstructionsLoaded` hook minden betöltést egy sorba ír
   (`path_glob_match  .claude/rules/backend.md  kiváltotta: Notes.Api/Program.cs`). A `/context` a
   munkamenet indulásakor betöltött fájlokat mutatja a **Memory files** alatt.
   A `/memory` mást mutat: a szerkeszthető memóriafájlok helyét és az **auto memory** be- és kikapcsolását.
   Az auto memory Claude saját jegyzetei; ha még nem jegyzett fel semmit, üresnek látszik, és ez rendben van.
   Ha a naplóban a `backend.md` nem jelenik meg egy `Notes.Api/` fájl beolvasása után, próbáld ki az
   idézőjel nélküli írásmódot (`paths: Notes.Api/**`); a Claude Code issue-k között van erre bejelentés.

## Egy feladat menete

1. Új munkamenet: `/clear` (vagy új `claude` indítás). Hosszabb munkánál ez kötelező: a régi kontextus
   rontja a következő feladat minőségét.
2. `/task 01`
3. Claude beolvassa a feladatot és a hivatkozott szakaszokat, majd tervet ad. **Olvasd el figyelmesen**:
   itt a legolcsóbb kijavítani a félreértést. Ha rendben van, hagyd jóvá.
4. Claude megvalósítja, lefuttatja a „Kész, ha” pontokat, és elindítja a `task-reviewer`-t.
5. A beszámoló után: nézd át a diffet (`git diff`), futtasd le magad is a `dotnet test`-et, és ha minden
   rendben, commitolj (vagy kérd meg Claude-ot, a commit rákérdez).
6. Ha a következő feladatnál ugyanazt a hibát látod, mint korábban, vedd fel a szabályt a `CLAUDE.md`-be
   vagy a megfelelő `.claude/rules/` fájlba.

## A te részed minden feladat után (5 perc, de pontot ér)

A követelmények szerint a „használat puszta felsorolása nem elegendő”: a döntéseidet és az ellenőrzéseidet
kell dokumentálni, és a védésen MI nélkül kell elmagyaráznod a kódot. Ezért minden feladat után:

1. Töltsd ki a napló bejegyzésének két sorát (`docs/ai/ai-naplo.md`): **Fejlesztői döntés** (mit fogadtál
   el, mit módosítottál a terven, és miért) és **Ellenőrzés (fejlesztő)** (mit néztél át, mit futtattál).
2. Ha a feladatban volt érdemi döntés (pl. két megoldás közül választottál), jelöld döntési eset jelöltnek.
3. Olvasd el a beszámoló „Felkészülés a védésre” pontjait, és nézd meg a hivatkozott kódot magad.
   Próbáld meg a kérdést MI nélkül megválaszolni.

## Mérföldkövek és a repó

- A repó a 2. mérföldkőnél legyen **publikus** (a követelmény ezt kéri). A `.gitignore` miatt a
  `CLAUDE.local.md`, a `.claude/settings.local.json` és a titkok nem kerülnek fel; a JWT-kulcs user-secrets-ben van.
- A 11. feladat után, a commit és push után címkézd az első verziót:
  `git tag -a v1.0 -m "Első működő verzió"` és `git push origin v1.0`. Ez a „rögzített első verzió”,
  ehhez képest mutatod be a változásokat.

## Változtatási igény

1. Másold az oktató szövegét a `docs/change-request/igeny.md` fájlba.
2. Új munkamenet, majd `/change-request`. Az agent plan módban hatásvizsgálatot ad: érintett feltételek
   és kódrészek, adatok és migráció, legalább két megoldás, regressziós terv. Ezt nézd át alaposan,
   mert a pontozás külön díjazza az indoklást és a regressziós ellenőrzést.
3. A jóváhagyás után az agent javaslatot ír a spec v2.0-ra (a VÁLTOZÁSNAPLÓ-val, legfeljebb 4000
   karakterben) és a design módosítására a `docs/change-request/` alá. A `docs/spec.md` és a
   `docs/design.md` védett, ezeket **te** vezeted át (a hook az agentnek nem engedi).
4. A létrejött 12+ feladatokat ugyanúgy, `/task NN` paranccsal futtatod.

## Mások által készített skillek és pluginok

Két forma van:

- **Plugin marketplace-ről** (ajánlott): `/plugin` (Discover fül) vagy `/plugin install <név>@<marketplace>`.
  Az Anthropic hivatalos marketplace-e (`claude-plugins-official`) alapból elérhető. Telepítéskor válassz
  hatókört: a **Project** a repó `.claude/settings.json`-jába írja, így a beadott kódbázisban is látszik.
  VS Code-ban a bővítmény „Manage plugins” ablakából telepíthetsz. A plugin skilljei névtérrel érhetők el:
  `/<plugin>:<skill>`.
- **Önálló skill-mappa** (pl. egy GitHub-repóból): másold a mappát a `.claude/skills/<név>/` alá (benne
  a `SKILL.md`), és a `/<név>` paranccsal hívható.

Mielőtt telepítesz:

1. **Nézd át a tartalmát.** A pluginok hookokat, scripteket és MCP-szervereket is hozhatnak, amelyek a te
   jogosultságaiddal futnak. Telepítés előtt a részletek nézet megmutatja, mit ad hozzá.
2. **Ütközés a projekt szabályaival.** Egy külső skill a saját ízlése szerint javasolhat könyvtárat vagy
   megoldást. A projektben a `.claude/rules/frontend.md` az erősebb (Tailwind, komponenskönyvtár nélkül, V33).
   Ha ettől el akarsz térni, módosítsd tudatosan a `design.md` 7. szakaszát és a frontend-szabályt, és rögzítsd
   döntésként a naplóban.
3. **Dokumentáld.** A követelmények az MI-t támogató elemeket külön pontozzák; a telepített plugin vagy
   skill kerüljön be a `docs/ai/mi-hasznalat.md`-be (honnan van, mire használtad, hol látszik az eredménye).
4. **Ne terheld feleslegesen a kontextust.** Csak azt telepítsd, amit tényleg használsz; a nem használt
   pluginokat a `/plugin` Installed fülén letilthatod.

### A `frontend-design` telepítése (a 06. feladat előtt)

1. Claude Code-ban: `/plugin install frontend-design@claude-plugins-official`, vagy VS Code-ban a „Manage plugins”
   ablakban keresd meg. Hatókörnek a **Project**-et válaszd, így a `.claude/settings.json`-ba kerül, és a repóban
   is látszik, hogy a projekt használja.
2. Telepítés után indíts új munkamenetet, és ellenőrizd: Claude-ot kérdezd meg, milyen skillek érhetők el.
3. Commitold a `.claude/settings.json` változását külön commitban, a V33-mal együtt.
4. A skill a frontend munkánál magától betöltődik; a 06. feladat tervében az agent leírja a vizuális irányt,
   amit jóváhagysz.

Ezen felül hasznos lehet a `csharp-lsp` és a `typescript-lsp` (kódintelligencia: Claude szerkesztés után
azonnal látja a fordítási hibákat; a `csharp-ls`, illetve a `typescript-language-server` programot külön
kell telepíteni).

## Ha valami félremegy

- **Rossz irányba indult:** `Esc` megszakítja; `/rewind` (vagy dupla `Esc`) visszalép egy korábbi pontra.
  Ez jobb, mint egymásra halmozni a javító utasításokat.
- **Elkészült, de rossz:** `git restore .` és `git clean -fd` (ezeket te futtasd), majd új munkamenet,
  pontosított feladatleírással.
- **A hook téved:** a blokkoló üzenet megjelenik a beszélgetésben. Ha jogos a művelet, csináld meg kézzel,
  vagy módosítsd a hook szabályát.
- **A `/task` nem indul:** ha Claude azt mondja, hogy a skillt nem hívhatja meg, töröld a
  `disable-model-invocation: true` sort a `SKILL.md`-ből (egyes verziókban ismert hiba). Ugyanez
  vonatkozik a `/change-request`-re.

## Személyes beállítások

Ami csak rád vonatkozik (pl. PowerShell-szintaxis, tömörebb válaszok), az a `CLAUDE.local.md`-be menjen,
a saját jogosultsági kivételeid pedig a `.claude/settings.local.json`-ba. Mindkettő `.gitignore`-ban van.

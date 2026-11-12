# MI-napló

Feladatonként egy bejegyzés. Az agent a `/task` munkafolyamat végén tölti ki a vázat; a
**Fejlesztői döntés** és az **Ellenőrzés (fejlesztő)** sort a fejlesztő írja vagy pontosítja.
Ez a napló a nyersanyaga a README MI-fejezetének és a három döntési esetnek
(`docs/ai/dontesi-esetek.md`).

Fázisok: követelményelemzés/tervezés · megvalósítás · tesztelés · változtatási igény · dokumentáció

---

## 00 – Tervezés és agent-munkakörnyezet
- **Fázis:** követelményelemzés/tervezés
- **Eszköz:** Claude (claude.ai), Claude Opus 5.5
- **Részletek:** `docs/ai/00-tervezes-osszefoglalo.md`

<!-- Új bejegyzés sablonja (az agent másolja a fájl végére):

## NN – <feladat címe> (ÉÉÉÉ-HH-NN)
- **Fázis:** megvalósítás | tesztelés | változtatási igény | dokumentáció
- **Eszköz:** Claude Code (<felület: VS Code bővítmény / CLI>), <modell>
- **Kontextus:** `docs/tasks/NN-*.md`, a design hivatkozott szakaszai, CLAUDE.md, `.claude/rules/…`
- **Terv és módosításai:** mit javasolt az agent; mit változtatott rajta a fejlesztő a jóváhagyás előtt
- **Eltérés a tervtől megvalósítás közben:** – / mi és miért
- **Blokkolt műveletek (hook/jogosultság):** – / mi volt, és mi lett helyette
- **Elbukó tesztek valódi hiba miatt:** – / teszt neve, a hiba oka, a mentett kimenet útja
- **Reviewer megállapításai:** és mi történt velük
- **Ellenőrzés:** futtatott parancsok és eredményük (pl. dotnet test: 42/42)
- **Fejlesztői döntés:** (a fejlesztő tölti ki) elfogadás / módosítás / elvetés és indok
- **Ellenőrzés (fejlesztő):** (a fejlesztő tölti ki) mit nézett át, mit futtatott le saját maga
- **Döntési eset jelölt?** igen/nem – ha igen, egy mondatban miért
-->

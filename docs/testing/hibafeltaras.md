# Hibafeltárás – sikertelen mentésnél elveszett a beírt szöveg (V47)

**Valódi fejlesztési hiba, nem szándékosan beültetett.** A hibát a fejlesztő kézi próbája találta a 07. feladat után
(`docs/testing/manual/07.md` 31. lépés). A 07b. feladat írt rá automatizált tesztet és javította. A tesztet a 07b-ben a
hibás kódon, a 10. feladatban pedig a mai kódban visszaállított hibás feltételen is lefuttattuk: mindkét esetben elbukik,
a javított kódon átmegy.

## 1. A hiba

**Tünet (07.md, 31. lépés):** a felhasználó megnyit egy mentett jegyzetet, a szövegét módosítja, és a backend leállítása
után a „Mentés” gombot nyomja. Megjelenik „A szerver nem érhető el.” üzenet, de egy pillanattal később a szerkesztő
helyén csak a hibaüzenet és egy „Bezárás” gomb áll, a beírt szöveg elvész. A backend újraindítása után a jegyzet a
szerveren tárolt, régi szöveggel nyílik meg.

**Elvárt működés:** design 7. szakasz, „Szerkesztő” blokk:
> Sikertelen mentés (validációs, hálózati vagy szerverhiba): a hibaüzenet a szerkesztőn belül jelenik meg, a szerkesztő és
> a beírt szöveg megmarad, és a Mentés újra megnyomható (V47).

Ugyanezt rögzíti a `.claude/rules/frontend.md` is („sikertelen mentésnél a beírt szöveg megmarad”). A spec elfogadási
feltételei (AC1–AC8) ezt az esetet közvetlenül nem írják le; a szabály a design felületi döntése (V47). A legközelebbi
rokona az AC4, amely szerint sikertelen mentésnél a tárolt tartalom változatlan marad. Ez a hibás változatban is
teljesült, a felhasználó helyi, még nem mentett szövege veszett el.

**Az ok (a frontend, nem a backend):**
1. A sikertelen `PUT` után a `NoteEditor` `catch` ága beállítja a hibaüzenetet, a helyi `draft` állapot még megvan
   (`2a1bee2:notes-web/src/notes/NoteEditor.tsx:151-154`).
2. A `finally` ág hibánál is meghívja a `refreshLists()`-t (`NoteEditor.tsx:155-157`), amely a teljes `['notes']`
   előtagú query-családot érvényteleníti (`NoteEditor.tsx:112-115`).
3. Ebbe beletartozik a megnyitott jegyzet részletes lekérdezése is (`['notes', 'detail', id]`,
   `2a1bee2:notes-web/src/notes/views.ts:83`; a mai kódban `views.ts:95-96`), ezért az is újratöltődik, és a leállított backend miatt elbukik. A TanStack Query
   a lekérdezést `error` állapotba teszi, bár a korábban betöltött adat (`detail.data`) megvan.
4. A `NotesPage` csak a hibaállapotot nézte, az adat meglétét nem
   (`2a1bee2:notes-web/src/pages/NotesPage.tsx:97-98`):
   ```tsx
   if (detail.isPending) return <p role="status">Betöltés…</p>
   if (detail.isError) {
     return ( /* hibaüzenet + Bezárás gomb */ )
   }
   return <NoteEditor key={detail.data.id} … />
   ```
   A `NoteEditor` helyére így a hibablokk került, a komponens lecsatolódott, és vele a helyi `draft` állapot is elveszett.

**Hibás változat:** a `2a1bee2` commit (a 07b munkájának kezdetén a HEAD). A frontendkódot utoljára a `4a51135`
módosította (`git diff 4a51135 2a1bee2 -- notes-web` üres), a hibás logika a 07. feladat commitjából (`9f9e4cd`) való.

## 2. A javítás

Commit: `a93041c` („Fix editor draft loss on failed save and folder view after move”). A V47-es javítás a
`notes-web/src/pages/NotesPage.tsx`-ben van (a commit másik része a V46-os nézetváltás, ez a hibához nem tartozik):

```diff
-    if (detail.isPending) return <p role="status">Betöltés…</p>
-    if (detail.isError) {
+    // Once the note is loaded, the editor stays mounted even if a later reload fails (e.g. the lists are
+    // refreshed after a failed save while the backend is down): unmounting it would lose the typed text (V47).
+    // The error block is only for a note that could not be loaded at all (e.g. 404).
+    if (!detail.data && detail.isError) {
       return ( /* hibaüzenet + Bezárás gomb */ )
     }
+    if (!detail.data) return <p role="status">Betöltés…</p>
     return <NoteEditor key={detail.data.id} … />
```

A hibablokk most csak akkor jelenik meg, ha a jegyzet egyáltalán nem tölthető be (például 404 megnyitáskor). Ha az adat
már megvan, egy későbbi sikertelen újratöltés nem csatolja le a szerkesztőt; a hibaüzenet a szerkesztőn belül marad. A
mai kódban: `notes-web/src/pages/NotesPage.tsx:115-128`. A 08. feladat erre építve kezeli a menet közben elérhetetlenné
vált jegyzetet (404/403 újratöltéskor: a szerkesztő nyitva marad, felül üzenet; V52).

## 3. A teszt

`notes-web/src/pages/NotesPage.test.tsx`, `describe('NotesPage – failed save (V47)')`, két eset (`it.each`):
- `keeps the editor and the typed text after a network error, and saves it when the backend is back`: a `fetch` kivételt
  dob (leállított backend);
- `keeps the editor and the typed text after a 5xx without a body, and saves it when the backend is back`: törzs nélküli
  502 (a Vite proxy válasza leállított backendnél).

A teszt a valódi `NotesPage`-et rendereli (valódi `QueryClientProvider`, `MemoryRouter`, hamis `AuthContext`, mockolt
`fetch`). Lépései:
1. megnyitja a jegyzetet;
2. a szövegbe új sort ír;
3. „leállítja a backendet”, és megnyomja a „Mentés”-t;
4. megvárja, hogy a részletes lekérdezés újratöltődjön, és minden lekérés befejeződjön.

Ezután elvárja, hogy:
- a „Szöveg” mező még látható legyen;
- a mezőben a beírt szöveg legyen;
- a szerkesztőn belül „A szerver nem érhető el. Ellenőrizd, hogy fut-e a backend.” üzenet jelenjen meg;
- a backend „visszaállítása” után az újabb „Mentés” a beírt szöveget küldje el `PUT`-tal.

A teszt `QueryClient`-je `retry: false`. Az éles kliens a hálózati hibát egyszer újrapróbálja (`notes-web/src/main.tsx:18-19`),
ez a hiba lényegén nem változtat: az újrapróbálás után is hibával zárul a lekérdezés.

**Miért bukik el a hibás változaton:** a hibaüzenet így szól:
`AssertionError: a szerkesztő „Szöveg” mezője látható marad: expected null not to be null`. A `contentField()` a
„Szöveg” címkéjű `textarea`-t keresi; `null`-t ad, mert a `NoteEditor` lecsatolódott, és helyén a hibablokk van. A teszt
tehát pontosan a tünetnél bukik el (a szerkesztő eltűnt), nem fordítási vagy környezeti hibán. Mindkét eset így bukik el.

## 4. A futások

| Futás | Kód | Eredmény | Kimenet |
|---|---|---|---|
| 07b – hibás | `2a1bee2` + a 07b-ben megírt teszt (javítás előtt) | 2/2 elbukik, az állításon (`NotesPage.test.tsx:121`) | [`runs/07b-failed-save-keeps-draft-elbukik.txt`](runs/07b-failed-save-keeps-draft-elbukik.txt) |
| 07b – javított | a `a93041c` állapota | 2/2 átmegy | [`runs/07b-failed-save-keeps-draft-atmegy.txt`](runs/07b-failed-save-keeps-draft-atmegy.txt) |
| 10 – hibás (újra előidézve) | a mai kód (`de6d59c`) a hibás feltétellel (a lenti patch) | 2/2 elbukik, ugyanazon az állításon (`NotesPage.test.tsx:123`) | [`runs/10-failed-save-keeps-draft-elbukik.txt`](runs/10-failed-save-keeps-draft-elbukik.txt) |
| 10 – javított | a mai kód (`de6d59c`, változatlan) | 2/2 átmegy | [`runs/10-failed-save-keeps-draft-atmegy.txt`](runs/10-failed-save-keeps-draft-atmegy.txt) |

A 10. feladat futásai azt igazolják, hogy a teszt a 08. feladat változásai után is őrzi a hibát. A 08-ban a teszt egy
`/shares` mockot kapott, ezért az állítás sora 121-ről 123-ra került. A 10-es „hibás” futás nem új hiba: ugyanaz a
valódi hiba, a mai kódba visszaállítva. A mai kódban ugyanis a 08. feladat óta más is változott (például a `detailGone` és
az `unavailableMessage`), ezért a `2a1bee2` fájlja nem tehető vissza egy az egyben.

## 5. Reprodukálás

A repó gyökerében, tiszta munkakönyvtárral:

```powershell
git apply docs/testing/10-valodi-hiba-visszaallitas.patch   # a hibás feltétel visszaállítása
cd notes-web
npm test -- src/pages/NotesPage.test.tsx                    # 2 failed
cd ..
git checkout -- notes-web/src/pages/NotesPage.tsx           # a javított kód vissza
cd notes-web
npm test -- src/pages/NotesPage.test.tsx                    # 2 passed
```

Az eredeti hibás változaton: `git switch --detach 2a1bee2`, majd a tesztfájl
(`git show a93041c:notes-web/src/pages/NotesPage.test.tsx`) és a tesztkörnyezet (a `a93041c` `package.json`-ja és
`vite.config.ts`-e, `npm install`) átvétele kell, mert a `2a1bee2`-ben még nincs Vitest. A 07b-ben a teszt és a
tesztkörnyezet a javítás előtt került a `2a1bee2` munkakönyvtárába, így készült az elbukó futás. A fenti patch az
egyszerűbb út.

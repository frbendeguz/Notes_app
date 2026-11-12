---
name: change-request
description: Az oktató által kiadott változtatási igény feldolgozása - hatásvizsgálat, spec v2.0 és design javaslat, új feladatfájlok. Kódot nem ír. Csak akkor, ha a fejlesztő a /change-request paranccsal kéri.
argument-hint: <a változtatási igény szövege, vagy üresen, ha a docs/change-request/igeny.md-ben van>
disable-model-invocation: true
---

# Változtatási igény feldolgozása

A változtatási igény szövege: $ARGUMENTS
Ha ez üres, olvasd el a `docs/change-request/igeny.md` fájlt. Ha az sincs, kérd el a fejlesztőtől.

A házi feladat követelményei szerint a módosítást a **rögzített első verzióból** (`v1.0` címke) kell
elvégezni; be kell mutatni, mely követelményeket és kódrészeket érinti, miért ez a megoldás, és milyen
regressziós ellenőrzések igazolják a korábbi működés és a tárolt adatok megőrzését.
Ebben a munkamenetben **kódot nem írsz**: elemzel, javaslatot teszel, és feladatfájlokat készítesz.

## 0. Ellenőrzés
- `git tag --list v1.0` és `git log --oneline -1 v1.0`: létezik-e a címke? Ha nem, állj meg, és szólj.
- `git status`: tiszta-e a munkakönyvtár? A HEAD a `v1.0`-ból indul-e (`git merge-base --is-ancestor v1.0 HEAD`)?
- Ha az igény szövege nem egyértelmű, a kérdéseket a terv elején tedd fel.

## 1. Hatásvizsgálat (plan módban, jóváhagyásra)
Írd meg a `docs/change-request/hatasvizsgalat.md` fájl tartalmát (a jóváhagyás után mentsd el):
1. **Az igény a saját szavaiddal**, és az értelmezési kérdések (ha vannak).
2. **Érintett elfogadási feltételek:** melyik AC változik, melyik új, melyik marad változatlan.
3. **Érintett kód:** fájlok, osztályok, végpontok, frontend-komponensek (keresd meg őket a kódban,
   fájl:sor hivatkozással), és a változás jellege mindegyiknél.
4. **Adatok és séma:** kell-e migráció; mi történik a meglévő adatokkal (mit kell megőrizni, mit
   kell átalakítani, mi az alapérték a régi sorokra). Ha nincs sémaváltozás, mondd ki.
5. **Megoldási lehetőségek:** legalább kettő, előnyökkel és hátrányokkal, és a javasolt megoldás indoka.
6. **Regressziós terv:** mely meglévő tesztek védik a változatlan működést (név szerint), milyen új
   tesztek kellenek; ha van migráció, **adatmegőrzési teszt**: v1.0 sémájú adatbázis feltöltése,
   az új migráció futtatása, az adatok ellenőrzése.
7. **Kockázatok.**

## 2. Javaslatok a védett dokumentumokra
A `docs/spec.md` és a `docs/design.md` védett; ezeket a fejlesztő módosítja. Készíts javaslatot:
- `docs/change-request/spec-v2-javaslat.md`: a spec **teljes** új szövege, benne a módosult
  feltételekkel, a bizonytalanságok frissítésével és a **VÁLTOZÁSNAPLÓ (v2.0)** kitöltésével
  (mi változott, és milyen értelmezési döntést hoztunk). **A spec szövege legfeljebb 4000 karakter**
  (a fejlécmegjegyzés nélkül); számold meg, és írd a fájl végére az eredményt. Ha nem fér bele,
  tömörítsd, a részleteket pedig a `hatasvizsgalat.md`-re hivatkozva hagyd ott.
- `docs/change-request/design-javaslat.md`: a `docs/design.md` módosítandó szakaszai, előtte/utána formában.

## 3. Feladatfájlok
Az elfogadott megoldás alapján írd meg az új feladatokat a `docs/tasks/` alá, a meglévő
feladatfájlokkal azonos szerkezetben (12-től számozva), például:
- backend-módosítás és migráció, adatmegőrzési teszttel,
- frontend-módosítás,
- regressziós ellenőrzés és tesztjelentés frissítése (`docs/testing/acceptance.md`: új/módosult AC-k,
  a „Kötelező esetek” regressziós sora a változtatási igényhez kötve),
- dokumentáció: `docs/change-request/valtozasok-osszefoglalasa.md` (mi változott v1.0 → végleges,
  a commitok/diff hivatkozásával), README és `docs/ai/mi-hasznalat.md` frissítése.
Vedd fel őket a `docs/progress.md` „Változtatási igény” szakaszába.

## 4. Lezárás
- MI-napló bejegyzés (fázis: változtatási igény) a `docs/ai/ai-naplo.md` végére.
- Beszámoló: a javasolt megoldás egy bekezdésben, a fejlesztő teendői (a spec és a design javaslat
  átvezetése, a hatásvizsgálat átnézése), az új feladatok listája. Ne commitolj, amíg a fejlesztő nem kéri.

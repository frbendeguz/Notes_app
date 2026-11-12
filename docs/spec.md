# SPECIFIKÁCIÓ – Szinkronizált jegyzetkezelő

> Az eredeti specifikáció. Az agent NEM módosíthatja.

## CÉL
Webalkalmazás, amelyben a felhasználók szöveges jegyzeteket készítenek, mappákba rendezik és más
felhasználókkal megosztják. A jegyzetek a szerveren tárolódnak, így bármely eszközről,
bejelentkezés után elérhetők.

## ARCHITEKTÚRA ÉS TECHNOLÓGIA
Külön backend és frontend.
- Backend: ASP.NET Core Web API (C#), EF Core, SQLite, REST API.
- Frontend: React + TypeScript (Vite), webes kliens.
- Automatizált tesztek: xUnit.

## SZEREPKÖRÖK (jegyzetenként)
- Tulajdonos: létrehozza, szerkeszti, törli, mappába helyezi és megosztja a jegyzetet.
- Szerkesztő: a megosztott jegyzet címét és szövegét módosíthatja; nem törölhet, nem oszthat meg.
- Olvasó: a megosztott jegyzetet csak olvashatja.

## FŐ FORGATÓKÖNYVEK
Regisztráció és belépés; jegyzet létrehozása, szerkesztése, törlése; mappa létrehozása és jegyzet
mappába helyezése; jegyzet megosztása felhasználónév alapján; megosztás visszavonása; ugyanazon
fiókba belépés másik eszközről.

## ELFOGADÁSI FELTÉTELEK
1. Helyes adatokkal belépve a felhasználó csak a saját és a vele megosztott jegyzeteket látja.
   Hibás jelszóval a belépés elutasítva, hibaüzenet jelenik meg, munkamenet nem jön létre.
2. 1–100 karakteres címmel és legfeljebb 10 000 karakteres szöveggel mentett jegyzet megjelenik a
   listában, és újbóli belépés után, illetve másik eszközről is ugyanúgy látszik.
3. Határeset: pontosan 100 karakteres cím és pontosan 10 000 karakteres szöveg elfogadva; 101
   karakteres cím vagy 10 001 karakteres szöveg esetén a mentés elutasítva, a jegyzet nem jön létre.
4. Hibás bemenet: üres vagy csak szóközökből álló cím esetén a mentés elutasítva (400-as hiba és
   hibaüzenet), meglévő jegyzet szerkesztésekor a korábbi tartalom változatlan marad.
5. A tulajdonos megoszt egy jegyzetet egy létező felhasználóval olvasó vagy szerkesztő joggal, a
   címzett a „Velem megosztott" listában látja. Nem létező felhasználóval, önmagával vagy ismételt
   megosztás esetén hibaüzenet jelenik meg, duplikáció nem keletkezik.
6. Jogosultság: az olvasó szerkesztési vagy törlési kísérlete 403-as hibát kap és a jegyzet
   változatlan; a szerkesztő módosíthat, de törlési és megosztási kísérlete 403; a jegyzethez nem
   tartozó idegen felhasználó 403/404-et kap.
7. Megosztás visszavonása után a címzett a következő lekérésnél már nem látja és nem éri el a
   jegyzetet; a jegyzet a tulajdonosnál megmarad.
8. Mappák: egy jegyzet legfeljebb egy mappában lehet; a mappanév 1–50 karakter, felhasználónként
   egyedi (kis- és nagybetűtől függetlenül); duplikált név elutasítva. Nem üres mappa törlésekor a
   benne lévő jegyzetek megmaradnak, mappa nélküli állapotba kerülnek.

## LÉNYEGES BIZONYTALANSÁGOK ÉS A VÁLASZTOTT ÉRTELMEZÉS
- Szinkronizálás: nincs valós idejű szinkron; a szerveren tárolt adatot a kliens betöltéskor vagy
  frissítéskor kéri le.
- Egyidejű szerkesztés: „utolsó mentés érvényes" (last write wins), ez ismert, vállalt korlát.
- Kategória és mappa: csak mappák vannak, címkék/kategóriák nincsenek.
- Mappák: csak a tulajdonos használja őket; a címzett a megosztott jegyzeteket külön listában látja.
- Megosztás címzettje: felhasználónév alapján azonosítva; csoportos megosztás nincs.

## VÁLTOZÁSNAPLÓ
A változtatási igény után a módosult feltételek és az értelmezési döntések itt kerülnek
rögzítésre (v2.0).

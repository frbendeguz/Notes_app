---
paths:
  - "Notes.Tests/**"
---

# Tesztelési szabályok (Notes.Tests)

- Integrációs tesztek: `TestApiFactory` (`WebApplicationFactory<Program>`, in-memory SQLite, egyetlen
  életben tartott kapcsolat). Hitelesített klienshez az `AuthHelper`-t használd (a 02. feladattól).
- A fixture adatbázisa közös: minden teszt egyedi felhasználónévvel dolgozzon
  (pl. `"u" + Guid.NewGuid().ToString("N")[..12]`), és ne feltételezze, hogy az adatbázis üres.
  Rövid, véletlen nevet ne generálj (kevés változat → ütközés → időnként elbukó teszt). Rövid nevet csak a
  hossz-határteszt használjon, és ott is olyat, amelyet más teszt nem használ.
- Határértékek `[Theory]` + `[InlineData]`, a korlátok a `Limits` osztályból (`Limits.TitleMax + 1`).
- Tesztnév: `Metódus_Helyzet_ElvártEredmény`, pl. `CreateNote_TitleTooLong_Returns400AndNoNoteCreated`.
- Hibás esetben a teszt ellenőrizze a mellékhatás hiányát is (nem jött létre / változatlan maradt).
- Ahol a teszt egy elfogadási feltételt fed le, kommentben jelöld: `// AC3`.
- Elbukó tesztet ne „javíts” a teszt gyengítésével, kihagyásával vagy törlésével. Ha a teszt szerint
  a termékkód hibás, javítsd a kódot; ha a teszt elvárása tűnik hibásnak, kérdezz.

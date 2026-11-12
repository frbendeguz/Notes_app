---
paths:
  - "notes-web/**"
---

# Frontend szabályok (notes-web)

- React függvénykomponensek, TypeScript `strict`. `any` tilos; ismeretlen adatra `unknown` + szűkítés.
- Minden API-hívás a `src/api.ts` wrapperen keresztül (Bearer token, JSON, ProblemDetails `detail`
  kinyerése, 401 → kijelentkeztetés). Komponensben közvetlen `fetch` nincs.
- A JWT csak `sessionStorage`-ben tárolható (V13); `localStorage`-ba, cookie-ba vagy URL-be nem kerülhet.
- Adatlekérés: TanStack Query; mentés/törlés után query invalidálás. Útvonalak: React Router.
- Stílus: sima CSS, UI-könyvtár nélkül. A felület nyelve magyar.
- A szerep szerinti tiltás/rejtés csak kényelmi réteg: a szerver 403/404 válaszát mindig kezelni kell.
- A szerver hibaüzenetét (`detail`, `errors`) jelenítsd meg; sikertelen mentésnél a beírt szöveg megmarad.
- A backend URL-jét ne égesd be: fejlesztéskor a Vite proxy továbbítja az `/api` hívásokat.
- Feladat végén: `npm run build` és `npm run lint` hibamentes.

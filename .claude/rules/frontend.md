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
- Stílus: Tailwind CSS v4 (`@tailwindcss/vite`), utility osztályokkal a komponensekben. Nincs `tailwind.config.js`
  és PostCSS-konfiguráció: a v4 a `src/index.css`-ben `@import "tailwindcss";` sorral indul, a saját színek és
  betűtípusok a `@theme` blokkba kerülnek. Komponenskönyvtár (MUI, shadcn stb.) és inline `style` nincs.
- Vizuális tervezés: a `frontend-design` skill irányelvei szerint, de ezek a szabályok az erősebbek (Tailwind,
  komponenskönyvtár nélkül; új npm csomag csak engedéllyel, külső betűtípus vagy CDN csak engedéllyel).
- Hozzáférhetőség: minden űrlapmezőnek legyen címkéje, a gombok billentyűzettel is elérhetők, látható fókusz.
- Vizuális irány (V35): „aktadosszié” (régi FBI-akták), Courier Prime betű (`@fontsource/courier-prime`, helyben).
  A színek és a betű csak a `src/index.css` `@theme` tokenjeiből jöhetnek; új szín vagy betű a tokenek között legyen.
- Statikus ellenőrzés: `npm run lint` (oxlint, V34). Ne cseréld ESLintre engedély nélkül.
- A felület nyelve magyar.
- A szerep szerinti tiltás/rejtés csak kényelmi réteg: a szerver 403/404 válaszát mindig kezelni kell.
- A szerver hibaüzenetét (`detail`, `errors`) jelenítsd meg; sikertelen mentésnél a beírt szöveg megmarad.
- A backend URL-jét ne égesd be: fejlesztéskor a Vite proxy továbbítja az `/api` hívásokat.
- Feladat végén: `npm run build` és `npm run lint` hibamentes.

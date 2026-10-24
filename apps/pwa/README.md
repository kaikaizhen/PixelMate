# PixelMate PWA

Vue 3 + TypeScript + Vite installable web app (served under `/web/`, API on the same origin under `/api/`).
This stage: register, login, logout, forgot / reset password, e-mail confirmation, adopt a pet (pick a look + name), the pet's home
(level, skills, tap to pet), offline banner, install + update prompts.

Design target: **no page scrolls** on an iPhone 16 (393×852) or a 1440×900 desktop (checked by `e2e/pwa.spec.ts`); wide screens
use two columns, phones drop the subtitle and shrink the pet.

## Run

```bash
# terminal 1: API (see apps/api/README.md for the database secret)
cd apps/api && dotnet run --project src/PixelMate.Api          # http://localhost:5200
# terminal 2: PWA
cd apps/pwa && npm install && npm run dev                      # http://localhost:5173/web/
```

`/api` is proxied to the API in dev and preview, so the session cookie is first-party. In development the API prints the
confirmation / reset e-mail (with its link) to its console until Gmail is configured.

## Scripts

| Command | What it does |
|---|---|
| `npm run dev` | dev server on 5173 |
| `npm run build` | type-check + production build + service worker (`dist/`) |
| `npm run preview` | serve `dist/` on 4174 (use this to test install / offline) |
| `npm test` | unit + component tests (Vitest) |
| `npm run e2e` | Playwright on desktop (1440) and mobile (360); the API is mocked |

## Notes

- PWA: manifest scope `/web/`, `registerType: prompt` (a new version waits for the player to tap 更新), precache of static files only,
  `/api/*` is never cached or used as a navigation fallback.
- Every state-changing request sends `X-Requested-With: pixelmate` (the API's CSRF check).
- Icons are generated from the star pet: `python tools/pixel-art/pwa_icons.py`.

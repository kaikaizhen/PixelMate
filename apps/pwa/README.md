# PixelMate PWA

Vue 3 + TypeScript + Vite installable web app (served under `/web/`, API on the same origin under `/api/`).
This stage: register, login, logout, forgot / reset password, e-mail confirmation, adopt a pet (pick a look + name), the pet's home
(level, skills, tap to pet), offline banner, install + update prompts.

The home page is **full screen**: the house art is scaled to the short side of the screen and its wall, baseboard and floor are
continued with seamless tiles (`python tools/pixel-art/room_extend.py`), so the room runs edge to edge. Functions (pet, account;
backpack and mail are marked "coming soon") open as floating cards on top of the room — no dim mask, the room stays tappable.

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

## Game art

The home page is **your house** (the website's room art) filled with the game assets: the window shows the weather scene for the
time of day (`world.weather.*`), the furniture and the pet come from `assets/manifest.json` **by assetKey** (no hard-coded paths).
`npm run dev` / `build` first run `scripts/sync-game-assets.mjs`, which copies the repo-root `assets/` (manifest, sprites, backgrounds)
into `public/game/` (git-ignored). Edit the starter room in `src/constants/defaultRoom.ts`.

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
- Bag (背包): `useInventoryStore` lists owned items and wears/removes them through `POST /pets/me/equipment`
  (sends the pet `revision`; a conflict re-reads the pet). Worn items are drawn on the pet by `GamePet` using the manifest `slot`/`z`;
  a costume hides the hat and draws its tail behind the body. Room items are listed only; placing them comes later.
- Life: the API decides what the pet is doing (`pet.activity`, 15-minute slots) and what it finds; the PWA only shows it (label, walking pace, notes
  while humming) and re-reads the pet when its slot ends, on focus and every minute. New finds (`GET /pets/me/events`) pop up once per device.

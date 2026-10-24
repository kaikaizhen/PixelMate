# PixelMate API

ASP.NET Core (.NET 10 LTS) Web API for pets, inventory and mail. Database: MariaDB 10.11 via EF Core 9 + Pomelo
(Pomelo has no EF Core 10 release yet; the runtime is .NET 10).

## Run

```bash
cd apps/api
dotnet user-secrets set "ConnectionStrings:Default" "Server=<host>;Port=3306;Database=<schema>;Uid=<user>;Pwd=<password>" --project src/PixelMate.Api
dotnet run --project src/PixelMate.Api          # http://localhost:5200
dotnet test
```

Settings are read from, in order of priority: real environment variables, `apps/api/.env.local`, `apps/api/.env`, user-secrets,
`appsettings*.json`. `.env` and `.env.local` are git-ignored (copy `.env.example`). Typical split:

- `.env` — the values for the real site (database, Gmail, `Auth__WebBaseUrl=https://…`).
- `.env.local` — overrides for your own machine (`Auth__WebBaseUrl=http://localhost:5173/web`, `Auth__SecureCookie=false`, `Health__ExposeDb=true`).

Use single quotes around values that contain `#` or `$`, e.g. `ConnectionStrings__Default='Server=…;Pwd=a#b$c'`.

## Endpoints

| Method | Path | Notes |
|---|---|---|
| GET | `/api/v1/health` | liveness, no dependencies |
| GET | `/api/v1/health/db` | internal diagnostic; 404 unless `Health:ExposeDb=true` |
| POST | `/api/v1/auth/register` | `{email, password, displayName}` → 201; 409 `EMAIL_ALREADY_REGISTERED`; 422 with per-field `errors` |
| POST | `/api/v1/auth/login` | sets the `.pixelmate.auth` cookie (HttpOnly, SameSite=Lax, Secure); 5 failures lock 15 min |
| POST | `/api/v1/auth/logout` | 204 |
| GET | `/api/v1/auth/me` | current user, 401 when signed out |
| POST | `/api/v1/auth/forgot-password` | `{email}` → always 202 (never reveals whether the account exists); mails a 1-hour link |
| POST | `/api/v1/auth/reset-password` | `{email, token, newPassword}` → 204; single use; signs out other sessions |
| POST | `/api/v1/pets` | `{name, bodyVariant}` (`star_01_cream` / `star_01_mint`) → 201 snapshot; 409 `PET_ALREADY_EXISTS`; name 1–12 characters |
| GET | `/api/v1/pets/me` | pet snapshot (docs/API契約 §2); 404 when the player has not adopted yet |
| POST | `/api/v1/pets/me/skills/allocate` | `{skillKey, pointsToAdd 1-5, expectedRevision}` → `{revision, skills, remainingSkillPoints}`; 422 `INSUFFICIENT_POINTS` (no points / over the 5 cap), 409 `REVISION_CONFLICT` |
| POST | `/api/v1/pets/me/skills/reset` | `{expectedRevision}` → all skills back to 0 (free in the MVP) |
| GET | `/api/v1/inventory` | the player's items with catalogue details (`name, category, rarity, slot, assetKey, quantity, equipped`) |
| POST | `/api/v1/pets/me/equipment` | `{slot, itemId \| null, expectedRevision}` wear / take off; slots `head front back feet hand costume`; costume and hat exclude each other |
| POST | `/api/v1/dev/inventory/grant` | **Development only**: `{itemId, quantity}` puts items in the bag |
| POST | `/api/v1/dev/pets/me/level-up` | **Development only** (not mapped elsewhere): +1 level so skill points can be tested |
| POST | `/api/v1/auth/confirm-email` / `resend-confirmation` | e-mail verification; enforced when `Auth:RequireConfirmedEmail=true` |

Every **state-changing** request must send `X-Requested-With: pixelmate` (CSRF defence for cookie sessions), otherwise 403 `CSRF_REJECTED`.
Passwords: 10+ characters with a letter and a digit. Auth endpoints have their own rate limit (`RateLimit:AuthPerMinute`).

## Mail (Gmail)

Set `Mail__Username`, `Mail__Password` (a Google *app password*), `Mail__FromAddress` in the environment (see `.env.example`).
Without them the API runs fine: in Development the e-mail text (with the link) is written to the log, in Production nothing is sent and a warning is logged.

## Database

```bash
export DOTNET_ROOT=$HOME/.dotnet DOTNET_ROLL_FORWARD=Major      # only needed when dotnet-ef runs on a newer runtime
dotnet ef database update --project src/PixelMate.Api           # creates/updates all tables
dotnet ef migrations add <Name> --project src/PixelMate.Api --output-dir Data/Migrations
```

Every error is `{ "code", "message", "requestId" }`; codes are defined once in `Infrastructure/ApiError.cs`.

## Item catalogue

`item_definitions` is seeded at start-up from `assets/manifest.json` (copied to `Seed/manifest.json`), so the art, the API and the PWA /
ESP32 share the same assetKeys (`itemId` = assetKey). Items: wearables, furniture, decor, collectibles and stationery (458 today); weather
scenes, effects and costume tails are not items. Rarity follows the art batch: B1/B2 common, B3/B4 rare, B5 and costumes precious.

## Life (autonomous behaviour)

- `Life/LifeScheduler.cs` – pure rules: the activity of each 15-minute slot (idle/walk/read/hum/letter, weighted by skill points) and whether a
  finished walking slot ends with a find. Same answer on every device and process.
- `Life/LifeService.cs` – makes up finished slots (max `Life:CatchUpDays`, default 7): `pet_events` (unique per pet+slot) + the items go into the bag in one
  transaction; `pet_life.cursor_slot` is a concurrency token so the API and the worker can run at once. Max `Life:MaxFindsPerDay` (3) a UTC day.
- `Life/LifeWorker.cs` – `BackgroundService` every `Life:WorkerIntervalSeconds` (300; 0 = off). `GET /pets/me` also catches up lazily.
- `GET /pets/me` has `activity {key, startedAt, endsAt}`; `GET /pets/me/events` lists recent finds. Development: `POST /dev/pets/me/life/rewind?hours=N`.

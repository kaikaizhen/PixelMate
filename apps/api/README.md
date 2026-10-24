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

Secrets live in user-secrets (dev) or environment variables (`ConnectionStrings__Default`), never in the repo. See `.env.example`.

## Endpoints

| Method | Path | Notes |
|---|---|---|
| GET | `/api/v1/health` | liveness, no dependencies |
| GET | `/api/v1/health/db` | internal diagnostic; 404 unless `Health:ExposeDb=true` |

Every error is `{ "code", "message", "requestId" }`; codes are defined once in `Infrastructure/ApiError.cs`.

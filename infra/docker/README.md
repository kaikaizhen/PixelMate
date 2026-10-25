# Docker images

Built on the developer machine only (the NAS never builds), from the **repository root**:

```bash
TAG=$(date +%Y%m%d-%H%M%S)-$(git rev-parse --short HEAD)
docker build --platform linux/amd64 -f infra/docker/api.Dockerfile -t pixelmate-api:$TAG .
docker build --platform linux/amd64 -f infra/docker/pwa.Dockerfile --build-arg "VITE_ENV_LABEL=測試服（會刪檔）" -t pixelmate-pwa:$TAG .
docker build --platform linux/amd64 --build-arg VITE_SITE_URL=https://example.com -t pixelmate:$TAG apps/web
```

| Image | Listens on | Notes |
|---|---|---|
| `pixelmate` (website, `apps/web`) | 5174 | |
| `pixelmate-pwa` | 5175 | served under `/web/`; `VITE_ENV_LABEL` puts a "test server" tag on every screen (empty = nothing) |
| `pixelmate-api` | 5176 | settings from the `.env` that lives only on the host; `Database__MigrateOnStart=true` applies EF migrations at start; Alpine + tzdata, read-only root fs, no capabilities |

Signed-in players stay signed in across deployments because the cookie keys live in the database (`PersistKeysToDbContext`) under the fixed application name `PixelMate`
(`DeploymentTests`). Running an amd64 image on an Apple-silicon Docker (QEMU) can crash .NET reflection stubs; test the image natively or on the target.

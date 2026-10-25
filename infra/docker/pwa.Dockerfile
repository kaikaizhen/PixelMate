# PixelMate PWA (served under /web/). Built on the developer machine only: docker build -f infra/docker/pwa.Dockerfile --build-arg VITE_ENV_LABEL=... .
FROM --platform=$BUILDPLATFORM node:22-alpine AS build
WORKDIR /repo
COPY apps/pwa/package.json apps/pwa/package-lock.json apps/pwa/
RUN cd apps/pwa && npm ci
COPY apps/pwa apps/pwa
# the game art is read by assetKey through the manifest; scripts/sync-game-assets.mjs copies these into public/game
COPY assets/manifest.json assets/manifest.json
COPY assets/sprites assets/sprites
COPY assets/backgrounds assets/backgrounds
# non-empty = this is a test server and the PWA says so (e.g. "測試服（會刪檔）")
ARG VITE_ENV_LABEL=
ENV VITE_ENV_LABEL=$VITE_ENV_LABEL
RUN cd apps/pwa && node scripts/sync-game-assets.mjs && npm run build

FROM nginx:1.27-alpine
ENV PORT=5175
COPY infra/docker/pwa.nginx.template /etc/nginx/templates/default.conf.template
COPY --from=build /repo/apps/pwa/dist /usr/share/nginx/html/web
HEALTHCHECK --interval=30s --timeout=3s --retries=3 CMD wget -q -O /dev/null http://127.0.0.1:${PORT}/health || exit 1

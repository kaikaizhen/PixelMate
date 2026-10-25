# PixelMate API. Built on the developer machine only (the NAS never builds): docker build --platform linux/amd64 -f infra/docker/api.Dockerfile .
# The publish output is framework-dependent (CPU independent), so the SDK stage runs natively and only the small runtime stage is for the NAS CPU.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# the item catalogue is seeded from the game manifest (csproj links ../../../../assets/manifest.json)
COPY assets/manifest.json assets/manifest.json
COPY apps/api/src/PixelMate.Api/PixelMate.Api.csproj apps/api/src/PixelMate.Api/
RUN dotnet restore apps/api/src/PixelMate.Api/PixelMate.Api.csproj
COPY apps/api/src/PixelMate.Api apps/api/src/PixelMate.Api
RUN dotnet publish apps/api/src/PixelMate.Api/PixelMate.Api.csproj -c Release -o /out --no-restore -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
# tzdata: players' time zones (Asia/Taipei ...) are resolved by name
RUN apk add --no-cache tzdata
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:8082 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0
USER app
EXPOSE 8082
HEALTHCHECK --interval=30s --timeout=4s --start-period=40s --retries=3 CMD wget -q -O /dev/null http://127.0.0.1:8082/api/v1/health || exit 1
ENTRYPOINT ["dotnet", "PixelMate.Api.dll"]

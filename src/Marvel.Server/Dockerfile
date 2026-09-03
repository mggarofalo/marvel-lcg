# Multi-platform manifest digests pin the exact Microsoft inputs. The output
# remains a framework-dependent net8.0 application even though the repository
# builds with its exact .NET 10 SDK.
FROM mcr.microsoft.com/dotnet/sdk:10.0.400@sha256:e1ffd2a92ae84c1291bc1b6887501f8af98e6331e7af6d4c8d37168c5e87a64c AS build
WORKDIR /source
COPY global.json Directory.Build.props Directory.Build.targets Directory.Packages.props ./
COPY src/ src/

ARG MARVEL_PRODUCT_VERSION=0.1.0-dev.0
ARG MARVEL_COMMIT=local
ARG MARVEL_VERSION_MAJOR=0
ARG MARVEL_VERSION_MINOR=1
ARG MARVEL_VERSION_PATCH=0
RUN dotnet publish src/Marvel.Server/Marvel.Server.csproj \
    --configuration Release \
    --output /out \
    --no-self-contained \
    -p:MarvelProductVersion="$MARVEL_PRODUCT_VERSION" \
    -p:MarvelCommit="$MARVEL_COMMIT" \
    -p:MarvelVersionMajor="$MARVEL_VERSION_MAJOR" \
    -p:MarvelVersionMinor="$MARVEL_VERSION_MINOR" \
    -p:MarvelVersionPatch="$MARVEL_VERSION_PATCH"

FROM mcr.microsoft.com/dotnet/runtime:8.0.30-bookworm-slim@sha256:9d94ecf60a21c6e7a784cf0761fbd4a8391646617a0ff2f39621443d580cc2c3
ARG MARVEL_PRODUCT_VERSION=0.1.0-dev.0
ARG MARVEL_COMMIT=local
LABEL org.opencontainers.image.title="Marvel Champions server" \
      org.opencontainers.image.description="Server-authoritative Marvel Champions Core Set engine" \
      org.opencontainers.image.version="$MARVEL_PRODUCT_VERSION" \
      org.opencontainers.image.revision="$MARVEL_COMMIT" \
      org.opencontainers.image.source="https://github.com/mggarofalo/marvel-lcg"

WORKDIR /app
COPY --from=build /out/ ./
COPY datasets/cards/cards.json datasets/cards/cards.json
COPY datasets/setup/setup.json datasets/setup/setup.json
COPY datasets/abilities/abilities.json datasets/abilities/abilities.json

RUN mkdir -p /var/lib/marvel/sessions && chown "$APP_UID:$APP_UID" /var/lib/marvel/sessions
VOLUME ["/var/lib/marvel/sessions"]
EXPOSE 41923
STOPSIGNAL SIGTERM
HEALTHCHECK --interval=10s --timeout=5s --start-period=10s --retries=3 \
    CMD ["dotnet", "Marvel.Server.dll", "--health-check"]
USER $APP_UID
ENTRYPOINT ["dotnet", "Marvel.Server.dll", "--listen", "0.0.0.0", "--port", "41923", "--data-root", "/app", "--save-root", "/var/lib/marvel/sessions"]

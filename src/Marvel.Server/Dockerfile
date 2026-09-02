# global.json is the build authority; the output still targets and runs on net8.0.
FROM mcr.microsoft.com/dotnet/sdk:10.0.302 AS build
WORKDIR /source
COPY . .
RUN dotnet publish src/Marvel.Server/Marvel.Server.csproj \
    --configuration Release \
    --output /out \
    --no-self-contained

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /out/ ./
COPY datasets/cards/cards.json datasets/cards/cards.json
COPY datasets/setup/setup.json datasets/setup/setup.json
COPY datasets/abilities/abilities.json datasets/abilities/abilities.json
VOLUME ["/var/lib/marvel/sessions"]
EXPOSE 41923
ENTRYPOINT ["dotnet", "Marvel.Server.dll", "--listen", "0.0.0.0", "--port", "41923", "--data-root", "/app", "--save-root", "/var/lib/marvel/sessions"]

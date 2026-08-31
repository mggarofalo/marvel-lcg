FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
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
EXPOSE 41923
ENTRYPOINT ["dotnet", "Marvel.Server.dll", "--listen", "0.0.0.0", "--port", "41923", "--data-root", "/app"]

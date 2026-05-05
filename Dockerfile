# ── Build stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/Estoria.Domain/Estoria.Domain.csproj            src/Estoria.Domain/
COPY src/Estoria.Application/Estoria.Application.csproj  src/Estoria.Application/
COPY src/Estoria.Infrastructure/Estoria.Infrastructure.csproj src/Estoria.Infrastructure/
COPY src/Estoria.Api/Estoria.Api.csproj                  src/Estoria.Api/

RUN dotnet restore src/Estoria.Api/Estoria.Api.csproj

COPY . .
RUN dotnet publish src/Estoria.Api/Estoria.Api.csproj \
    -c Release -o /app/publish --no-restore

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Estoria.Api.dll"]

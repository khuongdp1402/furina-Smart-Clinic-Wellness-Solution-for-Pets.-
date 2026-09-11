# syntax=docker/dockerfile:1

# --- build stage: restore + publish Release, never ships in the final image ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files first so `dotnet restore` is cached across builds that
# only change source code, not dependencies.
COPY Furina.sln .
COPY src/Furina.Api/Furina.Api.csproj src/Furina.Api/
COPY src/Furina.Domain/Furina.Domain.csproj src/Furina.Domain/
COPY src/Furina.Infrastructure/Furina.Infrastructure.csproj src/Furina.Infrastructure/
RUN dotnet restore src/Furina.Api/Furina.Api.csproj

COPY src/ src/
RUN dotnet publish src/Furina.Api/Furina.Api.csproj -c Release -o /app/publish --no-restore

# --- runtime stage: just the published output, run as non-root ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN groupadd --system furina && useradd --system --gid furina --home-dir /app --shell /usr/sbin/nologin furina \
    && chown -R furina:furina /app
USER furina

COPY --from=build --chown=furina:furina /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Furina.Api.dll"]

# syntax=docker/dockerfile:1

# ---- Stage 1: build the Angular SPA ----
FROM node:20-alpine AS spa
WORKDIR /spa
COPY Frontend/ClientApp/package.json Frontend/ClientApp/package-lock.json ./
RUN npm ci
COPY Frontend/ClientApp/ ./
RUN npm run build

# ---- Stage 2: publish the .NET API (SPA build skipped; copied from stage 1) ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS api
WORKDIR /src
COPY Backend/ ./Backend/
RUN dotnet restore Backend/ProjectK.3PDB.Standalone.API/ProjectK.3PDB.Standalone.API.csproj
RUN dotnet publish Backend/ProjectK.3PDB.Standalone.API/ProjectK.3PDB.Standalone.API.csproj \
    -c Release -o /app/publish -p:SkipSpaBuild=true
# Serve the pre-built Angular app as static web assets.
COPY --from=spa /spa/dist/browser /app/publish/wwwroot

# ---- Stage 3: runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=api /app/publish ./

# Headless container mode: serve SPA + API, no browser launch, no Velopack.
ENV PROJECTK_CONTAINER=true
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5220
ENTRYPOINT ["dotnet", "ProjectK.3PDB.Standalone.API.dll"]

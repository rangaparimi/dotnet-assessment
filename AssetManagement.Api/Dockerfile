# Build context: repository root  (docker build -f AssetManagement.Api/Dockerfile .)

# ---- Build stage: full SDK, compiles and publishes the app ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first, in its own layer, so packages are cached until the .csproj changes
COPY AssetManagement.Api/AssetManagement.Api.csproj AssetManagement.Api/
RUN dotnet restore AssetManagement.Api/AssetManagement.Api.csproj

COPY AssetManagement.Api/ AssetManagement.Api/
RUN dotnet publish AssetManagement.Api/AssetManagement.Api.csproj -c Release -o /app/publish --no-restore

# ---- Runtime stage: small ASP.NET image with only the published output ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# .NET 8 images listen on 8080 and run as the non-root "app" user by default
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "AssetManagement.Api.dll"]

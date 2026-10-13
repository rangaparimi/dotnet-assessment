# Build context: repository root  (docker build -f AssetManagement.UI/Dockerfile .)

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY AssetManagement.UI/AssetManagement.UI.csproj AssetManagement.UI/
RUN dotnet restore AssetManagement.UI/AssetManagement.UI.csproj

COPY AssetManagement.UI/ AssetManagement.UI/
RUN dotnet publish AssetManagement.UI/AssetManagement.UI.csproj -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "AssetManagement.UI.dll"]

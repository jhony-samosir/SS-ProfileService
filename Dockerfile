# =========================
# Build Stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

WORKDIR /src

# Copy csproj terlebih dahulu untuk cache restore
COPY src/SS.ProfileService.API/*.csproj src/SS.ProfileService.API/

RUN dotnet restore src/SS.ProfileService.API/SS.ProfileService.API.csproj

# Copy seluruh source
COPY . .

# Publish langsung (tidak perlu build terpisah)
RUN dotnet publish \
    src/SS.ProfileService.API/SS.ProfileService.API.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# =========================
# Runtime Stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "SS.ProfileService.API.dll"]
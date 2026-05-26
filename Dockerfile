FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Salin csproj dan restore
COPY ["src/SS.ProfileService.API/SS.ProfileService.API.csproj", "src/SS.ProfileService.API/"]
RUN dotnet restore "src/SS.ProfileService.API/SS.ProfileService.API.csproj"

# Salin semua source code dan build
COPY . .
RUN dotnet build "src/SS.ProfileService.API/SS.ProfileService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/SS.ProfileService.API/SS.ProfileService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image menggunakan .NET 10 ASP.NET Core
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SS.ProfileService.API.dll"]

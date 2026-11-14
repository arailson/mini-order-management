# syntax=docker/dockerfile:1

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia apenas os .csproj para aproveitar o cache de restore.
COPY src/Domain/Domain.csproj Domain/
COPY src/Application/Application.csproj Application/
COPY src/Infrastructure/Infrastructure.csproj Infrastructure/
COPY src/API/API.csproj API/

RUN dotnet restore API/API.csproj

COPY src/Domain/ Domain/
COPY src/Application/ Application/
COPY src/Infrastructure/ Infrastructure/
COPY src/API/ API/

RUN dotnet publish API/API.csproj -c Release -o /app/publish --no-restore -p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

USER app
ENTRYPOINT ["dotnet", "API.dll"]

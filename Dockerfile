FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/InventoryApp.sln backend/
COPY backend/src/API/InventoryApp.API.csproj backend/src/API/
COPY backend/src/Application/InventoryApp.Application.csproj backend/src/Application/
COPY backend/src/Domain/InventoryApp.Domain.csproj backend/src/Domain/
COPY backend/src/Infrastructure/InventoryApp.Infrastructure.csproj backend/src/Infrastructure/
COPY backend/tests/InventoryApp.Tests/InventoryApp.Tests.csproj backend/tests/InventoryApp.Tests/

RUN dotnet restore backend/InventoryApp.sln

COPY backend/ backend/
RUN dotnet publish backend/src/API/InventoryApp.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "InventoryApp.API.dll"]

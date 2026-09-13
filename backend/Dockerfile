FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/ ./backend/
RUN dotnet restore backend/InventoryApp.sln
RUN dotnet publish backend/src/API/InventoryApp.API.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://0.0.0.0:10000
ENV DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish .

USER $APP_UID

EXPOSE 10000
ENTRYPOINT ["dotnet", "InventoryApp.API.dll"]

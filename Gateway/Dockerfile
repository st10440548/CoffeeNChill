FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Gateway/CoffeeNChill.Gateway.csproj Gateway/
RUN dotnet restore Gateway/CoffeeNChill.Gateway.csproj
COPY Gateway/ Gateway/
RUN dotnet publish Gateway/CoffeeNChill.Gateway.csproj -c Release -o /app/publish --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "CoffeeNChill.Gateway.dll"]

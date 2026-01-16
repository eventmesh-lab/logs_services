ARG APP_PORT=7188
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore logs_services.api/logs_services.api.csproj
RUN dotnet publish logs_services.api/logs_services.api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
ARG APP_PORT=7188
ENV ASPNETCORE_URLS=http://*:${APP_PORT}
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE ${APP_PORT}
ENTRYPOINT ["dotnet", "logs_services.api.dll"]
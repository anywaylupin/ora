# Build from the repository root so the shared .editorconfig reaches the analyzers:
#   docker build -f api/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY .editorconfig ./
COPY api/global.json api/Directory.Build.props api/Directory.Packages.props api/
COPY api/Ora.Api/Ora.Api.csproj api/Ora.Api/
RUN dotnet restore api/Ora.Api/Ora.Api.csproj
COPY api/Ora.Api/ api/Ora.Api/
RUN dotnet publish api/Ora.Api/Ora.Api.csproj --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Ora.Api.dll"]

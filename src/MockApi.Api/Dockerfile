# Multi-stage build for MockApi.Api — the mock API used as a real, containerizable
# artifact by the AI-200 study program's 01-containers labs (ACR, ACR Tasks,
# App Service, Container Apps, AKS). Build context must be the repo root
# (see build command below).
#
#   docker build -f src/MockApi.Api/Dockerfile -t mock-api-acr:local .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/MockApi.Api/MockApi.Api.csproj src/MockApi.Api/
RUN dotnet restore src/MockApi.Api/MockApi.Api.csproj

COPY src/MockApi.Api/ src/MockApi.Api/
RUN dotnet publish src/MockApi.Api/MockApi.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "MockApi.Api.dll"]

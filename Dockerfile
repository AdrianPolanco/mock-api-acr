FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY src/MockApi.Api/MockApi.Api.csproj src/MockApi.Api/
RUN dotnet restore src/MockApi.Api/MockApi.Api.csproj

COPY src/MockApi.Api/ src/MockApi.Api/

WORKDIR /src/src/MockApi.Api
RUN dotnet publish -c Release --no-restore -o /app/publish


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "MockApi.Api.dll"]

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY . .

WORKDIR /src/src/MockApi.Api

RUN dotnet restore
RUN dotnet publish -c Release --no-restore -o /app/publish


FROM dhi.io/dotnet:10

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:80

ENTRYPOINT ["dotnet", "MockApi.Api.dll"]
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /bot

COPY *.csproj ./

RUN dotnet restore

COPY . ./

RUN dotnet publish -c Release -o /bot/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine

WORKDIR /bot

COPY --from=build /bot/publish .
COPY .env /bot/.env

ENTRYPOINT [ "dotnet", "tg_bot.dll" ]
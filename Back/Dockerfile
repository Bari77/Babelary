FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

ARG APP_ENV=Production
ENV ASPNETCORE_ENVIRONMENT=${APP_ENV}

COPY Back/ /src/Back/

RUN dotnet restore /src/Back/Api/Api.csproj
RUN dotnet publish /src/Back/Api/Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ARG APP_ENV=Production
ARG VERSION
ARG RUN_NUMBER
LABEL version="${VERSION}"
LABEL run_number="${RUN_NUMBER}"

ENV ASPNETCORE_ENVIRONMENT=${APP_ENV}
ENV ASPNETCORE_URLS=http://+:80

COPY --from=build /app/publish .

EXPOSE 80
ENTRYPOINT ["dotnet", "Api.dll"]

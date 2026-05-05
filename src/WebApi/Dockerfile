FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . ./
WORKDIR /src/src/WebApi
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish
# If BUILD_INFO.json was created in the project (by CI), copy it into the publish folder
RUN /bin/sh -c 'if [ -f /src/src/WebApi/BUILD_INFO.json ]; then cp /src/src/WebApi/BUILD_INFO.json /app/publish/; fi'

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "WebApi.dll"]

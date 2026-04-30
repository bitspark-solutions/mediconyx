FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY mediconyx-api/ ./
RUN dotnet restore mediconyx-api.csproj
RUN dotnet restore Tests/mediconyx-api.Tests.csproj

# Run unit tests — build fails if tests fail
RUN dotnet test Tests/mediconyx-api.Tests.csproj --no-restore --verbosity normal

RUN dotnet publish mediconyx-api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet","mediconyx-api.dll"]

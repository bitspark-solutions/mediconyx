FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY mediconyx-api/ ./
RUN dotnet restore mediconyx-api.csproj
RUN dotnet restore Tests/mediconyx-api.Tests.csproj

# Run unit tests — build fails if tests fail
RUN dotnet test Tests/mediconyx-api.Tests.csproj --no-restore --verbosity normal

RUN dotnet publish mediconyx-api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# Install Kerberos GSSAPI library. Npgsql eagerly probes for it at startup
# even when authenticating with username/password — missing it only logs a
# noisy warning, but installing it future-proofs the image for SSPI/Kerberos
# auth and silences the warning cleanly. The .NET 10 base image is Ubuntu
# 24.04 (Debian-based), so apt-get is available. Cleaned lists keep the
# layer small.
#
# Note: the GSSAPI shared object Npgsql looks for (libgssapi_krb5.so.2)
# lives in the `libgssapi-krb5-2` package, not `libkrb5-3`. Installing
# libgssapi-krb5-2 transitively pulls in libkrb5-3 + libkrb5support0
# which is everything we need.
RUN apt-get update \
 && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
 && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet","mediconyx-api.dll"]

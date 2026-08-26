FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY NileTechno.sln ./
COPY src/NileTechno.Domain/NileTechno.Domain.csproj src/NileTechno.Domain/
COPY src/NileTechno.Application/NileTechno.Application.csproj src/NileTechno.Application/
COPY src/NileTechno.Infrastructure/NileTechno.Infrastructure.csproj src/NileTechno.Infrastructure/
COPY src/NileTechno.API/NileTechno.API.csproj src/NileTechno.API/

RUN dotnet restore src/NileTechno.API/NileTechno.API.csproj

COPY . .
RUN dotnet publish src/NileTechno.API/NileTechno.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
USER root
# SqlClient still does a TLS pre-login on Linux even with Encrypt=False.
# OpenSSL 3 (Debian 12 / .NET 9) defaults to SECLEVEL=2 and rejects the
# ciphers/TLS versions this SQL Server uses — same handshake EOF Nile Chat
# never hits because Node/tedious is not on OpenSSL 3 SECLEVEL=2.
RUN awk '\
  /\[system_default_sect\]/ { found=1 } \
  found && /CipherString/ { sub(/CipherString = .*/, "CipherString = DEFAULT:@SECLEVEL=0") } \
  found && /MinProtocol/ { sub(/MinProtocol = .*/, "MinProtocol = TLSv1") } \
  { print } \
  END { \
    if (!found) { \
      print ""; \
      print "[system_default_sect]"; \
      print "MinProtocol = TLSv1"; \
      print "CipherString = DEFAULT:@SECLEVEL=0"; \
    } \
  }' /etc/ssl/openssl.cnf > /tmp/openssl.cnf \
  && mv /tmp/openssl.cnf /etc/ssl/openssl.cnf

WORKDIR /app
COPY --from=build /app/publish .
ENV DOTNET_HOSTBUILDER_RELOADCONFIGONCHANGE=false
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "NileTechno.API.dll"]

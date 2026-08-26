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
WORKDIR /app
COPY --from=build /app/publish .
ENV DOTNET_HOSTBUILDER_RELOADCONFIGONCHANGE=false
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
EXPOSE 8080
ENTRYPOINT ["dotnet", "NileTechno.API.dll"]

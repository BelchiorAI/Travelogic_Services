# Build context: the repository root (see docker-compose.yml).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, in its own layer, so it is cached until a project file changes.
COPY Directory.Build.props ./
COPY src/api/Suppliers.Domain/Suppliers.Domain.csproj src/api/Suppliers.Domain/
COPY src/api/Suppliers.Application/Suppliers.Application.csproj src/api/Suppliers.Application/
COPY src/api/Suppliers.Infrastructure/Suppliers.Infrastructure.csproj src/api/Suppliers.Infrastructure/
COPY src/api/Suppliers.Api/Suppliers.Api.csproj src/api/Suppliers.Api/
RUN dotnet restore src/api/Suppliers.Api/Suppliers.Api.csproj

COPY src/api/ src/api/
RUN dotnet publish src/api/Suppliers.Api/Suppliers.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Uploaded photos and videos (a volume in docker-compose.yml), owned by the non-root user that writes them.
RUN mkdir -p /app/media && chown $APP_UID /app/media

# The aspnet image ships a non-root "app" user; port 8080 needs no root privileges.
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Suppliers.Api.dll"]

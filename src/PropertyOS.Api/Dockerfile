# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS restore
WORKDIR /src

COPY ["src/PropertyOS.Domain/PropertyOS.Domain.csproj", "src/PropertyOS.Domain/"]
COPY ["src/PropertyOS.Application/PropertyOS.Application.csproj", "src/PropertyOS.Application/"]
COPY ["src/PropertyOS.Infrastructure/PropertyOS.Infrastructure.csproj", "src/PropertyOS.Infrastructure/"]
COPY ["src/PropertyOS.Api/PropertyOS.Api.csproj", "src/PropertyOS.Api/"]
RUN dotnet restore "src/PropertyOS.Api/PropertyOS.Api.csproj"

FROM restore AS build
COPY src/ src/
RUN dotnet publish "src/PropertyOS.Api/PropertyOS.Api.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM build AS migration-build
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet tool install --global dotnet-ef --version 9.0.0
RUN dotnet-ef migrations bundle \
    --project "src/PropertyOS.Infrastructure/PropertyOS.Infrastructure.csproj" \
    --startup-project "src/PropertyOS.Api/PropertyOS.Api.csproj" \
    --configuration Release \
    --self-contained \
    --target-runtime linux-x64 \
    --output /app/migrate

FROM mcr.microsoft.com/dotnet/runtime-deps:9.0 AS migrations
WORKDIR /app
COPY --from=migration-build --chown=$APP_UID:$APP_UID /app/migrate .
USER $APP_UID
ENTRYPOINT ["/bin/sh", "-c", "exec /app/migrate --connection \"$PROPERTYOS_DB\""]

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/storage \
    && chown -R "$APP_UID:$APP_UID" /app

COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .

USER $APP_UID
EXPOSE 8080
HEALTHCHECK --interval=15s --timeout=5s --start-period=30s --retries=5 \
    CMD curl --silent --show-error --output /dev/null http://127.0.0.1:8080/ || exit 1

ENTRYPOINT ["dotnet", "PropertyOS.Api.dll"]

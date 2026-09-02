FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/SweetSecrets.Api/SweetSecrets.Api.csproj src/SweetSecrets.Api/
COPY src/SweetSecrets.Application/SweetSecrets.Application.csproj src/SweetSecrets.Application/
COPY src/SweetSecrets.Contracts/SweetSecrets.Contracts.csproj src/SweetSecrets.Contracts/
COPY src/SweetSecrets.Domain/SweetSecrets.Domain.csproj src/SweetSecrets.Domain/
COPY src/SweetSecrets.Infrastructure/SweetSecrets.Infrastructure.csproj src/SweetSecrets.Infrastructure/
RUN dotnet restore src/SweetSecrets.Api/SweetSecrets.Api.csproj

COPY src/ src/
RUN dotnet publish src/SweetSecrets.Api/SweetSecrets.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys \
    && chown -R "$APP_UID:$APP_UID" /home/app/.aspnet
COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080
ENTRYPOINT ["dotnet", "SweetSecrets.Api.dll"]

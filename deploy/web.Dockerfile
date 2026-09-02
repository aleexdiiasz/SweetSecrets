FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/SweetSecrets.Web/SweetSecrets.Web.csproj src/SweetSecrets.Web/
COPY src/SweetSecrets.Contracts/SweetSecrets.Contracts.csproj src/SweetSecrets.Contracts/
RUN dotnet restore src/SweetSecrets.Web/SweetSecrets.Web.csproj

COPY src/ src/
RUN dotnet publish src/SweetSecrets.Web/SweetSecrets.Web.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM nginx:1.29-alpine AS runtime
COPY deploy/nginx.conf /etc/nginx/nginx.conf
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
USER nginx
EXPOSE 8080

# Docker Production Deployment Baseline

## Alcance

TEN-031 agrega una baseline reproducible para ejecutar SweetSecrets con Docker Compose:

```text
Internet -> terminador TLS -> Web/Nginx :8080
                              |-- archivos Blazor WASM
                              |-- /api/* y /health/* -> API :8080
                                                        -> PostgreSQL :5432
```

Solo Web publica un puerto del host. API y PostgreSQL permanecen en la red privada `backend`. El terminador TLS, DNS, certificados, firewall, backups y observabilidad pertenecen a la infraestructura del ambiente y no se incluyen en Compose.

## Artefactos

- `deploy/web.Dockerfile`: publica Blazor y lo sirve con Nginx no root.
- `deploy/api.Dockerfile`: publica la API .NET y la ejecuta con el usuario no root de la imagen.
- `deploy/nginx.conf`: fallback SPA y reverse proxy same-origin.
- `deploy/compose.production.yml`: Web, API, migrador de MASTER y PostgreSQL 18.6.
- `deploy/.env.example`: nombres de variables y valores de ejemplo no secretos.

## Configuración inicial

Copiar `deploy/.env.example` a un archivo local no versionado y reemplazar todos los placeholders. No reutilizar las credenciales de ejemplo.

Variables requeridas:

- `POSTGRES_USER`, `POSTGRES_PASSWORD` y `MASTER_CONNECTION_STRING`;
- `PUBLIC_BASE_URL`, con URL HTTPS pública de Web;
- `SMTP_HOST`, `SMTP_PORT`, `SMTP_USE_SSL`, remitente y credenciales SMTP;
- `BOOTSTRAP_ADMIN_EMAIL`, `BOOTSTRAP_ADMIN_PASSWORD` y `BOOTSTRAP_ADMIN_FULL_NAME`;
- opcionalmente `WEB_HTTP_PORT` para el puerto local recibido por el terminador TLS.

`POSTGRES_USER` necesita permiso para crear bases porque el provisioning database-per-tenant crea una base PostgreSQL por tenant. En producción administrada puede sustituirse por un rol con el privilegio mínimo equivalente.

La API valida al arrancar que Production tenga conexión MASTER, URLs HTTPS, SMTP, bootstrap, Data Protection y proxies confiables. Falla de forma temprana si falta una configuración obligatoria.

## Arranque y migraciones

Desde la raíz del repositorio:

```powershell
docker compose --env-file deploy/.env -f deploy/compose.production.yml config --quiet
docker compose --env-file deploy/.env -f deploy/compose.production.yml build
docker compose --env-file deploy/.env -f deploy/compose.production.yml up -d --wait
```

El servicio one-shot `migrate-master` ejecuta `SweetSecrets.Api.dll --migrate-master` antes de iniciar la API. La API normal no aplica migraciones MASTER en cada arranque. Si la migración falla, el servicio API no debe iniciar.

El alta de un tenant conserva el comportamiento existente: crea su base y aplica las migraciones tenant durante el provisioning. La actualización masiva y coordinada de bases tenant existentes queda pendiente de una herramienta operacional posterior; no se recorren automáticamente al arrancar.

## Web, same-origin y reverse proxy

En Production, Web usa su propio origen para consumir `/api`; no requiere `ApiBaseUrl` en el cliente. Nginx sirve rutas SPA mediante `index.html` y reenvía `/api/*` y `/health/*` sin publicar la API.

Nginx transmite `Host`, `X-Forwarded-Host`, `X-Forwarded-Proto` y agrega la IP TCP a `X-Forwarded-For`. La API procesa estos encabezados antes de HTTPS, health, rate limiting y autenticación. Solo confía en la red Docker configurada (`172.30.0.0/24`) y limita el salto reenviado a uno. No se limpian las listas de confianza ni se aceptan proxies arbitrarios.

El puerto Web debe quedar accesible únicamente desde el terminador TLS o la red controlada. Si cambia la topología o el CIDR, actualizar `ForwardedHeaders__KnownNetworks__0` y `ForwardedHeaders__ForwardLimit`; no ampliar la confianza a Internet.

## Persistencia

- `postgres_data` conserva MASTER y las bases tenant.
- `dataprotection_keys` conserva las claves que protegen cookies y tokens ASP.NET entre reinicios/recreaciones de API.

Las claves de Data Protection son material sensible: restringir acceso y respaldarlas junto con los datos. `docker compose down` conserva volúmenes; `docker compose down -v` los elimina y no debe usarse en producción para una detención normal.

La política de backup/restore, rotación y almacenamiento externo de secretos queda fuera de esta baseline y debe definirse antes del go-live.

## Bootstrap y email

El administrador inicial se configura solo mediante variables externas y se crea confirmado por el inicializador existente. No hay contraseña predeterminada en la imagen o Compose. Tras el primer arranque debe rotarse la credencial conforme a la operación del ambiente.

El transporte SMTP de TEN-029 recibe su configuración externamente. No se incluyen API keys ni credenciales. Para un despliegue final se recomienda inyección mediante el gestor de secretos del orquestador en lugar de conservar secretos en archivos locales o variables inspeccionables.

## Operación y verificación

Los logs de Nginx van a stdout/stderr; API y PostgreSQL usan sus salidas estándar. Los endpoints públicos a través de Web son:

- `GET /health/live`: proceso API vivo, independiente de PostgreSQL.
- `GET /health/ready`: conectividad con MASTER.

Validación técnica ejecutada en una pila aislada:

- construcción de las tres imágenes;
- migración MASTER one-shot y arranque ordenado;
- `/`, `/cuenta`, `/health/live` y `/health/ready` a través de Nginx;
- `/api/auth/me` no autenticado devuelve `401`;
- solo Web aparece publicado en el host;
- reinicio de API y PostgreSQL conserva datos, migraciones y claves de Data Protection;
- rate limiting de login devuelve `429` después del límite y usa la IP normalizada por el proxy, no un valor `X-Forwarded-For` arbitrario del cliente.

## Pendientes para producción final

- terminación TLS, DNS, firewall y restricción del puerto Web al proxy frontal;
- gestor de secretos y rotación de credenciales;
- backups y pruebas de restauración de PostgreSQL/Data Protection;
- automatización para migrar bases tenant existentes;
- métricas, alertas y centralización de logs;
- dimensionamiento, alta disponibilidad y estrategia de rollback;
- validación SMTP y smoke completo en el ambiente Production real.

# Production Readiness and Health Checks

## Issue

TEN-023 - Production Readiness & Health Checks

## Endpoints operacionales

La API publica dos endpoints pequeños, anónimos y aptos para infraestructura:

- `GET /health/live`: confirma que el proceso ASP.NET Core responde. No ejecuta health checks ni depende de PostgreSQL.
- `GET /health/ready`: ejecuta la verificación etiquetada `ready` y confirma conectividad liviana con MASTER mediante `Database.CanConnectAsync()`.

Durante la validación manual inicial se detectó que ambos endpoints estaban mapeados después de `UseAuthentication`. Una petición que incluía una cookie podía activar la validación Identity/SecurityStamp contra MASTER antes de llegar al endpoint; por ello liveness fallaba al detener PostgreSQL aunque `UserActivityMiddleware` excluyera el path.

La corrección registra los health checks como middleware terminal inmediatamente después de HTTPS y antes de CORS, autenticación, actividad de usuario y autorización. Así `/health/live` no entra en ningún middleware dependiente de Identity o MASTER. `/health/ready` también evita autenticación, pero ejecuta deliberadamente solo el health check `master_database`.

La respuesta contiene únicamente el estado global:

```json
{"status":"Healthy"}
```

Readiness devuelve el código HTTP estándar de health checks (`200` saludable, `503` no saludable). No incluye nombres de servidor, base, usuario, connection strings, excepciones ni stack traces.

## Database-per-tenant

Readiness no enumera tenants ni abre sus bases. Hacerlo aumentaría el costo del sondeo, acoplaría la disponibilidad global a todos los tenants y podría agotar conexiones. La base tenant se valida operacionalmente al resolver un usuario autenticado y crear su `TenantDbContext`.

Una verificación tenant específica puede diseñarse en el futuro con contexto autenticado o monitoreo fuera de banda, pero no forma parte del health global.

## Configuración Production

Al iniciar en `Production`, la API exige:

- `ConnectionStrings__MasterDatabase` no vacío;
- al menos un `Cors__AllowedOrigins__0` HTTPS;
- `PasswordRecovery__ResetPageBaseUrl` como URL HTTPS absoluta;
- `EmailConfirmation__ConfirmationPageBaseUrl` como URL HTTPS absoluta.
- configuración `Email__Smtp__*` válida para host, puerto, remitente y credenciales emparejadas.
- ruta/nombre de aplicación para claves persistentes de Data Protection;
- bootstrap admin completo;
- al menos un proxy o una red confiable y `ForwardLimit` positivo.

Los valores deben suministrarse mediante variables de entorno o un almacén seguro. No existen valores Production ni secretos hardcodeados. Development conserva sus URLs localhost en `appsettings.Development.json` y no queda bloqueado por la validación Production.

También deben configurarse fuera del repositorio:

- URL pública HTTPS de Web; el baseline Docker usa API same-origin y no publica `ApiBaseUrl` en el cliente;
- credenciales PostgreSQL;
- terminación HTTPS y red/CIDR del proxy confiable;
- proveedor y credenciales de email transaccional.

## Email Production

TEN-029 registra un transporte SMTP estándar con MailKit en ambientes no Development y exige configuración `Email:Smtp` completa en Production. Development conserva la bandeja local. Credenciales y remitente se suministran externamente; detalles en `docs/technical/PRODUCTION_EMAIL_RATE_LIMITING.md`.

## Cookies, HTTPS y CORS

La cookie `SweetSecrets.Auth` permanece `HttpOnly`, `Secure`, `SameSite=Lax`, con expiración de ocho horas. La API conserva `UseHttpsRedirection`. CORS ahora obtiene orígenes desde `Cors:AllowedOrigins`, permite credenciales y no usa comodines.

TEN-031 procesa `X-Forwarded-For`, `X-Forwarded-Proto` y `X-Forwarded-Host` antes de redirección, health y seguridad. La confianza se restringe a proxies/redes configurados y a un número explícito de saltos. Las claves de Data Protection se guardan en un volumen persistente para que cookies y tokens sobrevivan recreaciones de API.

## Manejo de errores y logging

Fuera de Development se habilita `UseExceptionHandler` con Problem Details para producir respuestas genéricas sin detalles internos. Swagger/OpenAPI continúa limitado a Development.

Un fallo de readiness registra únicamente que MASTER no está disponible; no adjunta la excepción para evitar filtrar datos de conexión. Los flujos tenant y email existentes tampoco deben registrar contraseñas, tokens, connection strings ni secretos.

## Pruebas

Las pruebas automatizadas cubren estado saludable/no saludable de MASTER, supresión de detalles sensibles, cuerpo mínimo, rutas anónimas y validación Production sin afectar Development.

Una prueba de pipeline ejecuta liveness y readiness delante de un middleware posterior que falla si es alcanzado: liveness permanece `200 Healthy` sin invocar el probe MASTER, mientras readiness invoca el probe y devuelve `503 Unhealthy` cuando falla.

## Limitaciones y pendientes

- validación SMTP Production-like con una cuenta de prueba;
- ajuste operacional de límites de autenticación;
- terminador TLS y certificados externos al baseline Docker;
- observabilidad externa y monitoreo de bases tenant fuera de banda;
- prueba funcional final en el ambiente Production real.

La topología, comandos, migraciones explícitas y smoke Production-like de TEN-031 están documentados en `docs/technical/DOCKER_PRODUCTION_DEPLOYMENT.md`.

TEN-032 agrega verificación recuperable: readiness por sí sola no demuestra que MASTER, tenants y key ring puedan restaurarse. La preparación operacional exige backups completos verificados y ejercicios periódicos según `docs/technical/BACKUP_RECOVERY.md`.

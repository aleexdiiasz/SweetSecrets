# Autenticación y sesiones

## Tecnología

SweetSecrets utiliza ASP.NET Core Identity.

## Base

Los usuarios están almacenados en MASTER.

Tabla:

platform_users

## Identificador

Los usuarios utilizan Guid como clave primaria.

## Roles

Roles actuales:

- PLATFORM_ADMIN
- TENANT_OWNER
- TENANT_USER

## PLATFORM_ADMIN

El administrador de plataforma utiliza:

TenantId = null

TEN-024 mantiene esta regla en `/admin`: el shell no resuelve tenant ni consume modulos operacionales. La ruta requiere `PLATFORM_ADMIN`; las rutas tenant continúan requiriendo `TENANT_OWNER`.

TEN-025 agrega listado, detalle, suspensión y reactivación de tenants exclusivamente para `PLATFORM_ADMIN`. Suspender cambia el estado en MASTER: un `TENANT_OWNER` suspendido es rechazado después de validar correctamente sus credenciales y políticas Identity, pero antes de crear sesión o cookie. Las credenciales inválidas conservan la respuesta genérica y no revelan el estado tenant. `PLATFORM_ADMIN` no queda afectado.

Las sesiones tenant existentes siguen protegidas por `CurrentTenantResolver`, que solo admite tenants `Active`. `/api/auth/me` también consulta la política MASTER y finaliza la sesión si el tenant dejó de estar activo, evitando un shell autenticado sin datos. Reactivar el tenant permite nuevamente login y operación sin modificar el usuario ni su contraseña.

## Cookies

La autenticación utiliza cookies Identity.

Nombre:

SweetSecrets.Auth

Características:

- HttpOnly;
- Secure;
- SameSite Lax;
- sliding expiration;
- duración configurada de 8 horas.

## Login

Endpoint:

POST /api/auth/login

Proceso:

1. validar correo;
2. buscar usuario;
3. verificar IsActive;
4. verificar IsBlocked;
5. validar contraseña con Identity;
6. aplicar control de intentos fallidos;
7. exigir correo confirmado cuando corresponde;
8. obtener roles Identity;
9. para `TENANT_OWNER`, exigir `TenantStatus.Active` desde MASTER;
10. crear user_session;
11. actualizar LastLoginAt;
12. actualizar LastActivityAt;
13. generar cookie;
14. agregar session_id como claim;
15. agregar tenant_id cuando corresponda;
16. registrar LOGIN_SUCCESS.

La respuesta exitosa incluye los roles obtenidos por Identity para elegir el destino inicial de UI: `PLATFORM_ADMIN` va a `/admin` y `TENANT_OWNER` a `/`. Esta selección no reemplaza la autorización de páginas y endpoints.

## Login fallido

Se registra:

LOGIN_FAILED

Si Identity bloquea temporalmente por intentos:

LOGIN_LOCKED_OUT

## Logout

Endpoint:

POST /api/auth/logout

Proceso:

1. identificar UserId;
2. obtener session_id;
3. cerrar user_session;
4. borrar cookie Identity;
5. registrar LOGOUT.

## Usuario actual

Endpoint:

GET /api/auth/me

Devuelve:

- UserId;
- TenantId;
- SessionId;
- Email;
- Roles.

## Sesiones

Tabla:

user_sessions

Campos funcionales:

- UserId;
- StartedAt;
- LastActivityAt;
- EndedAt;
- IpAddress;
- UserAgent;
- IsActive;
- EndReason.

## Actividad

UserActivityMiddleware actualiza actividad de usuarios autenticados.

Actualiza:

user_sessions.LastActivityAt

platform_users.LastActivityAt

## Usuario online

Actualmente se considera online cuando:

- tiene sesión activa;
- LastActivityAt está dentro de la ventana configurada.

La consulta administrativa utiliza una ventana de 5 minutos.

## Bloqueo administrativo

Cuando PLATFORM_ADMIN bloquea:

1. IsBlocked = true;
2. SecurityStamp se actualiza;
3. sesiones activas terminan;
4. se registra USER_BLOCKED.

## Desbloqueo

Cuando PLATFORM_ADMIN desbloquea:

1. IsBlocked = false;
2. SecurityStamp se actualiza;
3. se registra USER_UNBLOCKED.

## Endpoints administrativos

GET /api/admin/users

POST /api/admin/users/{userId}/block

POST /api/admin/users/{userId}/unblock

Requieren:

PLATFORM_ADMIN

## Seguridad de contraseñas

Configuración actual:

- mínimo 10 caracteres;
- mayúscula obligatoria;
- minúscula obligatoria;
- número obligatorio;
- símbolo obligatorio.

## Lockout

Configuración inicial:

- 5 intentos fallidos;
- bloqueo temporal de 15 minutos.

## Secretos

Nunca almacenar:

- password de usuario;
- password PostgreSQL;
- secretos SMTP;
- API keys

en código o Git.

## Swagger

Swagger se utiliza para pruebas manuales durante desarrollo.

No sustituye la UI de autenticación Blazor.

## Endpoints operacionales anónimos

`GET /health/live` y `GET /health/ready` permiten sondeo de infraestructura sin cookie. Su respuesta se limita al estado global y no expone identidad, tenant, base de datos, connection strings ni excepciones. No son endpoints funcionales de usuario.

En Production, las excepciones no controladas utilizan el manejador global con respuestas genéricas Problem Details; las herramientas Swagger/OpenAPI permanecen exclusivas de Development.

## Registro público Web

TEN-018 agrega la ruta pública `/register`. Consume `POST /api/auth/register` y, después de un resultado correcto, dirige a `/login` porque el endpoint no crea automáticamente una sesión ni una cookie de autenticación.

## Recuperación de contraseña

TEN-019 agrega:

- `POST /api/auth/forgot-password`;
- `POST /api/auth/reset-password`;
- `/forgot-password`;
- `/reset-password`.

Los tokens son generados y validados por ASP.NET Core Identity, tienen vigencia de una hora y la solicitud inicial utiliza una respuesta genérica para evitar enumeración de usuarios.

## Mi cuenta y cambio de contraseña

TEN-020 agrega `/cuenta`, `GET /api/auth/account` y `POST /api/auth/change-password` para `TENANT_OWNER`. El usuario objetivo se resuelve desde la identidad autenticada y la contraseña se cambia con ASP.NET Core Identity sobre MASTER.

Después de un cambio correcto se reemite la cookie conservando sus propiedades y los claims de tenant y sesión. La sesión actual continúa activa con el `SecurityStamp` actualizado.

## Confirmación de correo

TEN-021 agrega `POST /api/auth/resend-confirmation`, `POST /api/auth/confirm-email` y `/confirm-email`. Los tokens se generan y validan con ASP.NET Core Identity en MASTER.

Las cuentas creadas desde `2026-09-01T00:00:00Z` deben confirmar su correo antes del login. Las cuentas legacy anteriores no se bloquean ni se alteran. Login valida primero la contraseña y después aplica la restricción, evitando revelar una cuenta pendiente ante credenciales incorrectas.

El reenvío usa una respuesta genérica para correos inexistentes, confirmados, inactivos, bloqueados o fallos de entrega.

## Pendiente

- expiración avanzada de sesiones;
- invalidación periódica basada en SecurityStamp;
- rate limiting de login;
- pruebas automatizadas de autenticación.

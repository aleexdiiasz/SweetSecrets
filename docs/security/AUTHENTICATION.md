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
7. crear user_session;
8. actualizar LastLoginAt;
9. actualizar LastActivityAt;
10. generar cookie;
11. agregar session_id como claim;
12. agregar tenant_id cuando corresponda;
13. registrar LOGIN_SUCCESS.

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

## Pendiente

- confirmación de correo;
- expiración avanzada de sesiones;
- invalidación periódica basada en SecurityStamp;
- rate limiting de login;
- pruebas automatizadas de autenticación.

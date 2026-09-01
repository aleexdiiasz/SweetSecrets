# Password Recovery & Reset

## Issue y objetivo

TEN-019 implementa recuperación y restablecimiento seguro de contraseña para usuarios almacenados en MASTER mediante ASP.NET Core Identity.

## Endpoints y contratos

Endpoints públicos:

- `POST /api/auth/forgot-password`
- `POST /api/auth/reset-password`

Contratos:

- `ForgotPasswordRequest`: `Email`.
- `ForgotPasswordResponse`: mensaje genérico.
- `ResetPasswordRequest`: `Email`, `Token`, `NewPassword`.
- `ResetPasswordResponse`: confirmación pública.

No se reciben `TenantId`, `DatabaseName` ni `ConnectionString`.

## Flujo

```text
Login
→ /forgot-password
→ POST /api/auth/forgot-password
→ UserManager.GeneratePasswordResetTokenAsync
→ enlace /reset-password?email=...&token=...
→ POST /api/auth/reset-password
→ UserManager.ResetPasswordAsync
→ Login con nueva contraseña
```

Los tokens utilizan los proveedores predeterminados de ASP.NET Core Identity y Data Protection. Antes de incluirse en la URL se codifican con Base64 URL-safe. La vigencia configurada es de una hora.

## Seguridad contra enumeración

Una solicitud con correo existente, inexistente, inactivo o bloqueado devuelve el mismo mensaje público:

```text
Si existe una cuenta asociada a este correo, recibirás instrucciones para restablecer tu contraseña.
```

Los fallos de entrega tampoco cambian la respuesta pública. Se registran internamente sin escribir el token ni el correo en el log.

El endpoint no devuelve password hash, token, identificadores tenant ni detalles de conexión. La operación usa `UserManager<ApplicationUser>` contra MASTER y no consulta bases tenant.

## Estrategia de entrega

La entrega está desacoplada mediante `IPasswordResetNotificationService`.

En Development, `DevelopmentPasswordResetNotificationService` escribe las instrucciones en una bandeja local:

```text
%TEMP%/SweetSecrets/password-recovery
```

El nombre del archivo es aleatorio y el log solo muestra su ruta. El archivo contiene el correo y enlace necesarios para pruebas locales; debe tratarse como información sensible y eliminarse después de probar. El token deja de ser válido después de una hora o al utilizarse correctamente.

En ambientes que no son Development se registra `UnconfiguredPasswordResetNotificationService`. No se eligió proveedor comercial, SMTP ni credenciales. Hasta configurar una implementación productiva, la solicitud mantiene la respuesta genérica pero no puede entregar el enlace.

## UI

- Login enlaza a `/forgot-password`.
- Forgot Password muestra envío, error y mensaje genérico de éxito.
- Reset Password valida presencia del enlace, confirmación de contraseña, envío, token inválido/expirado, contraseña rechazada y éxito.
- Ambas páginas son públicas, usan `AuthLayout`, cultura `es-MX` y CSS aislado responsive aproximadamente a 390 px.

El backend conserva las reglas definitivas de contraseña configuradas en Identity.

Los errores de la política de contraseña se localizan de forma centralizada mediante `SpanishIdentityErrorDescriber`. Reset, Register y cualquier otro flujo que utilice el mismo `UserManager` reciben mensajes consistentes en español para longitud mínima, carácter especial, dígito, letra minúscula, letra mayúscula y caracteres únicos. La UI no replica ni sustituye estas reglas; Identity continúa siendo la autoridad.

## Pruebas

- Build aislado: correcto, 0 advertencias y 0 errores.
- Tests automatizados cubren la entrega Development y la localización de errores de contraseña.
- Se agregó una prueba que verifica la entrega en la bandeja local Development y elimina el archivo generado.
- `git diff --check`: correcto.

El build normal quedó inicialmente bloqueado por una instancia de `SweetSecrets.Api` abierta por el usuario; se utilizó `--artifacts-path` para validar sin interrumpirla.

## Pruebas manuales pendientes

PENDIENTE PRUEBA FUNCIONAL EN NAVEGADOR.

Debe comprobarse correo existente e inexistente con respuesta equivalente, lectura del enlace en la bandeja Development, token válido, token alterado, token expirado, contraseña inválida, cambio correcto, login con contraseña nueva y responsive aproximadamente a 390 px.

## Limitaciones y decisiones pendientes

- Falta elegir e implementar proveedor de correo para Production.
- No se implementó confirmación de correo.
- No hay rate limiting específico para los endpoints de recuperación.
- No se implementa `TENANT_USER` ni UI administrativa.

# Email Infrastructure and Email Confirmation

## Issue y alcance

TEN-021 incorpora una infraestructura común de email transaccional y confirmación de correo para cuentas nuevas mediante ASP.NET Core Identity en MASTER.

## Infraestructura común

Application define `ITransactionalEmailSender` y `TransactionalEmailMessage`. Infrastructure aporta:

- `DevelopmentTransactionalEmailSender`, bandeja local de desarrollo;
- `UnconfiguredTransactionalEmailSender`, adapter explícito para ambientes sin proveedor.

Password Recovery y Email Confirmation reutilizan la misma abstracción. Application no conoce SMTP ni proveedores comerciales. No se almacenan credenciales, API keys ni secretos.

En Development los mensajes se escriben en:

```text
%TEMP%/SweetSecrets/email
```

Cada archivo tiene nombre aleatorio, destinatario, asunto y cuerpo. El log solo registra la ruta. Los enlaces son sensibles y deben eliminarse después de las pruebas.

Production conserva el adapter no configurado. Falta seleccionar e implementar un proveedor transaccional y configurar URLs públicas; las respuestas anti-enumeración no cambian si falla la entrega.

## Endpoints y contratos

- `POST /api/auth/resend-confirmation`: `ResendEmailConfirmationRequest` y respuesta genérica.
- `POST /api/auth/confirm-email`: `ConfirmEmailRequest` (`Email`, `Token`) y `ConfirmEmailResponse`.

Ambos endpoints son públicos porque operan antes del login. No aceptan `UserId`, `TenantId`, base de datos ni connection string.

## Flujo

```text
registro
→ provisioning tenant sin cambios
→ creación TENANT_OWNER con EmailConfirmed=false
→ GenerateEmailConfirmationTokenAsync
→ token Base64 URL-safe
→ email /confirm-email?email=...&token=...
→ ConfirmEmailAsync
→ EmailConfirmed=true
→ login permitido
```

La vigencia deriva del token provider de Identity/Data Protection, configurado actualmente en una hora junto con los demás tokens Identity.

## Registro y UI

Después del registro, `/register` dirige a `/confirm-email?email=...&registered=true`. La página pública permite:

- informar que la cuenta fue creada;
- confirmar automáticamente un enlace;
- mostrar confirmación exitosa;
- manejar token inválido o expirado;
- reenviar el mensaje evitando doble submit;
- volver a Login.

La UI utiliza `AuthLayout`, cultura `es-MX` y CSS aislado responsive aproximadamente a 390 px.

## Login y compatibilidad

Las cuentas creadas desde `2026-09-01T00:00:00Z` requieren `EmailConfirmed=true`. La política se configura mediante `EmailConfirmation:EnforceForAccountsCreatedAfterUtc`.

Las cuentas anteriores con `EmailConfirmed=false` se consideran legacy y no se bloquean retroactivamente. No se modificó información existente ni se agregó migración. El administrador bootstrap ya se crea confirmado.

Login valida primero la contraseña. Solo después de credenciales correctas devuelve `EMAIL_NOT_CONFIRMED`; así una contraseña incorrecta no permite enumerar cuentas pendientes. Después de confirmar, el login normal crea cookie y `user_session` sin cambios.

## Seguridad

El reenvío responde siempre:

```text
Si existe una cuenta pendiente asociada a este correo, recibirás instrucciones para confirmarla.
```

La misma respuesta aplica a correo inexistente, ya confirmado, inactivo, bloqueado o fallo de entrega. Los tokens no se devuelven en la respuesta ni se escriben en logs. La confirmación opera únicamente con `UserManager<ApplicationUser>` en MASTER.

## Pruebas

Las pruebas automatizadas cubren generación y enlace, token válido, token inválido, confirmación exitosa, reenvío sin enumeración, política de login para cuenta nueva/confirmada/legacy, endpoints públicos y bandeja Development.

PENDIENTE PRUEBA FUNCIONAL EN NAVEGADOR del flujo completo y responsive aproximadamente a 390 px.

## Pendientes

- seleccionar e implementar proveedor Production;
- configurar URLs públicas de Production;
- rate limiting específico para reenvío y recuperación;
- definir una campaña opcional de confirmación para cuentas legacy.

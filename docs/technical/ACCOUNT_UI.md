# Tenant Account UI

## Issue y alcance

TEN-020 implementa la sección autenticada `/cuenta` para `TENANT_OWNER`. Permite consultar nombre y correo propios y cambiar la contraseña de la cuenta activa. No administra otros usuarios ni modifica correo, tenant o perfil.

## Endpoints y contratos

- `GET /api/auth/account` devuelve `AccountResponse`: `FullName` y `Email`.
- `POST /api/auth/change-password` recibe `ChangePasswordRequest`: `CurrentPassword` y `NewPassword`; devuelve `ChangePasswordResponse` con confirmación pública.

Ambos endpoints requieren `[Authorize(Roles = TENANT_OWNER)]`. El `UserId` se obtiene del `NameIdentifier` autenticado y no forma parte del request. No se aceptan ni devuelven `TenantId`, base de datos, connection string, password hash o identificadores técnicos de sesión.

## Cambio de contraseña

`AccountService` obtiene el usuario de MASTER y ejecuta `UserManager.ChangePasswordAsync`. ASP.NET Core Identity valida la contraseña actual y aplica la política vigente a la contraseña nueva:

- mínimo 10 caracteres;
- dígito;
- letra minúscula;
- letra mayúscula;
- carácter especial.

Los errores se traducen centralmente por código mediante `IdentityErrorLocalizer`, reutilizado también por Password Reset y Self-Registration. No se envían códigos ni excepciones al cliente. Blazor solo valida campos obligatorios y coincidencia de confirmación; el backend conserva la autoridad.

## Continuidad de sesión

`ChangePasswordAsync` actualiza el `SecurityStamp`. Después del cambio correcto, `IdentityAccountSessionRefresher` reemite la cookie Identity con sus propiedades actuales y conserva los claims `session_id` y `tenant_id`. La sesión activa continúa válida y el registro `user_sessions` no se reemplaza ni finaliza.

No se cierra sesión arbitrariamente. Las demás cookies anteriores del usuario quedan sujetas a la validación normal del `SecurityStamp` configurada por Identity.

## UI

La ruta `/cuenta` está protegida para `TENANT_OWNER` y se accede desde el header principal. Incluye:

- carga y reintento de información;
- nombre y correo en modo de solo lectura;
- contraseña actual, nueva y confirmación;
- prevención de doble envío;
- estados de guardado, éxito y error;
- CSS aislado responsive aproximadamente a 390 px.

## Pruebas

Las pruebas automatizadas cubren:

- contraseña actual incorrecta;
- contraseña nueva inválida con mensajes en español;
- cambio correcto y refresco de sesión;
- exigencia de rol `TENANT_OWNER` en consulta y cambio, que produce 401 para anónimos y 403 para otros roles mediante el middleware de autorización.

PENDIENTE PRUEBA FUNCIONAL EN NAVEGADOR del flujo completo y responsive aproximadamente a 390 px.

## Límites

No incluye `TENANT_USER`, administración de usuarios, cambio de correo, eliminación de cuenta, cambio de tenant ni interfaz `PLATFORM_ADMIN`.

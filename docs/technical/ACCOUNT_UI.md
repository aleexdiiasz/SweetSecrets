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

La ruta `/cuenta` está protegida para `TENANT_OWNER` y forma parte de la navegación principal desktop, tablet y móvil. Funciona como centro de administración personal e incluye:

- carga y reintento de información;
- nombre y correo en modo de solo lectura;
- card navegable Configuración, con acceso a `/configuracion`;
- acción Seguridad que abre el cambio de contraseña en un modal;
- contraseña actual, nueva y confirmación únicamente mientras el modal está abierto;
- prevención de doble envío;
- estados de guardado, éxito y error;
- cierre de sesión mediante el mismo `AuthApiClient.LogoutAsync` existente;
- CSS aislado responsive y bottom sheet en móvil.

El modal usa `role=dialog`, `aria-modal`, labels y foco inicial en la contraseña actual. En escritorio y tablet aparece centrado; en móvil se presenta como bottom sheet por encima de la navegación inferior. Cancelar o completar correctamente el cambio cierra el modal y reemplaza inmediatamente el modelo para no conservar contraseñas en el estado. El éxito se muestra de forma visible en Cuenta y la sesión continúa activa.

Cerrar sesión se encuentra exclusivamente en Cuenta para TENANT_OWNER. Se eliminó del header y de la navegación global, sin cambiar el endpoint ni el comportamiento de autenticación.

## Pruebas

Las pruebas automatizadas cubren:

- contraseña actual incorrecta;
- contraseña nueva inválida con mensajes en español;
- cambio correcto y refresco de sesión;
- exigencia de rol `TENANT_OWNER` en consulta y cambio, que produce 401 para anónimos y 403 para otros roles mediante el middleware de autorización.

PENDIENTE PRUEBA FUNCIONAL EN NAVEGADOR del flujo completo y responsive aproximadamente a 390 px.

## Límites

No incluye `TENANT_USER`, administración de usuarios, cambio de correo, eliminación de cuenta, cambio de tenant ni interfaz `PLATFORM_ADMIN`.

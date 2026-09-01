# Tenant Self-Registration UI

## Issue y objetivo

TEN-018 implementa una experiencia pública de autoregistro en Blazor WebAssembly mediante la ruta `/register`.

## Contratos y endpoint

La UI reutiliza los contratos existentes:

- `RegisterRequest`: `BusinessName`, `FullName`, `Email`, `Password`.
- `RegisterResponse`: respuesta pública del registro.

Endpoint utilizado: `POST /api/auth/register`.

No se agregaron endpoints ni se modificó el backend.

## Flujo

```text
/register
→ AuthApiClient.RegisterAsync
→ POST /api/auth/register
→ SelfRegistrationService
→ TenantProvisioningService
→ PostgreSQL tenant + migraciones + seed
→ TenantUserProvisioningService
→ TENANT_OWNER con EmailConfirmed=false
→ email transaccional de confirmación
→ /confirm-email?registered=true
```

El endpoint no autentica automáticamente. Después de un registro correcto, la UI dirige a la confirmación de correo. Las cuentas nuevas deben confirmar el enlace antes de iniciar sesión.

## Interfaz

El formulario contiene exactamente los datos requeridos por `RegisterRequest`: nombre del negocio, nombre del propietario, correo electrónico y contraseña.

Incluye validaciones básicas, prevención de doble envío, estado de procesamiento y mensajes de error. Login enlaza a “Crear cuenta” y Registro enlaza a “Iniciar sesión”.

## Seguridad y multi-tenancy

La página es pública y no acepta `TenantId`, `TenantCode`, `DatabaseName`, `ConnectionString` ni rol. Tampoco muestra esos valores después del registro.

El backend continúa siendo autoridad para generar el tenant, crear su base PostgreSQL independiente, aplicar migraciones, ejecutar el seed y crear el primer `TENANT_OWNER`.

No se almacenan contraseña ni tokens en el navegador. La autenticación posterior continúa utilizando la cookie HttpOnly existente.

## Manejo de errores

`AuthApiClient` conserva los mensajes públicos devueltos por 400 y 409. Para errores sin un mensaje JSON válido, incluido 500, presenta un mensaje genérico sin exponer stack traces o detalles internos. Los errores de conexión también tienen un mensaje específico.

## Responsive y cultura

La página usa `AuthLayout`, CSS aislado y se adapta a una columna aproximadamente a 390 px. Conserva la cultura `es-MX` configurada globalmente. No agrega frameworks de UI.

## Pruebas técnicas

- `dotnet build .\SweetSecrets.slnx`: correcto.
- `dotnet test .\SweetSecrets.slnx --no-build`: correcto.
- `git diff --check`: correcto.

## Prueba manual pendiente

PENDIENTE PRUEBA FUNCIONAL EN NAVEGADOR.

Debe validarse `/register`, los enlaces entre Login y Registro, validaciones, error por correo duplicado, estado prolongado de provisioning, redirección con confirmación, login de la cuenta creada y responsive aproximadamente a 390 px.

La prueba debe utilizar datos nuevos controlados y no modificar ni eliminar tenants existentes, especialmente el tenant fallido `000002`.

## Limitaciones

- No hay recuperación ni cambio de contraseña.
- El registro no inicia sesión automáticamente.
- No implementa `TENANT_USER`, invitaciones ni administración de usuarios.
- La UI no muestra detalles internos del provisioning.

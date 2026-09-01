# PLATFORM_ADMIN Tenant Management

TEN-025 implementa administración de tenants sobre MASTER para `PLATFORM_ADMIN`.

## API

- `GET /api/admin/tenants`: búsqueda server-side por nombre, código, owner o email; filtro por estado; página limitada a 50.
- `GET /api/admin/tenants/{id}`: detalle, owner principal y diez movimientos de auditoría recientes.
- `POST /api/admin/tenants/{id}/suspend`.
- `POST /api/admin/tenants/{id}/activate`.

Todos requieren `PLATFORM_ADMIN`. Los contratos no exponen `DatabaseName`, connection strings ni credenciales.

## Estados

Solo se permiten `Active -> Suspended` y `Suspended -> Active`. `Provisioning`, `Disabled` y `Failed` son de solo lectura. La actualización usa condición por estado para detectar cambios concurrentes sin agregar RowVersion ni migraciones.

Suspender conserva usuarios, datos y base tenant. Un `TENANT_OWNER` solo puede iniciar una nueva sesión cuando su tenant está `Active`: después de validar credenciales y políticas Identity, el login consulta el estado en MASTER y rechaza `Suspended` antes de emitir sesión o cookie. `PLATFORM_ADMIN` no depende de esta comprobación tenant.

`CurrentTenantResolver` continúa exigiendo estado `Active`, por lo que una sesión ya abierta tampoco puede obtener contexto operacional después de la suspensión. Además, `/api/auth/me` detecta ese cambio, finaliza la sesión y devuelve una respuesta controlada para que Web vuelva al login en vez de mostrar un shell vacío. `Suspended -> Active` restaura login y resolución operacional sin cambiar contraseña, usuario ni base tenant.

## Auditoría

Las acciones registran `TENANT_SUSPENDED` o `TENANT_ACTIVATED`, actor derivado de `NameIdentifier`, tenant objetivo, transición, IP, User-Agent y timestamp. El frontend no envía actor.

## UI

`/admin/tenants` ofrece búsqueda, filtro, loading, error, reintento, vacío y listado responsive. `/admin/tenants/{id}` muestra datos MASTER, owner, actividad, auditoría reciente y confirmación antes de suspender/reactivar. No usa mocks ni consulta tenant DB.

## Pruebas y pendientes

Las pruebas cubren política de transición, actualización/auditoría, 404, propagación server-side de filtros, ausencia de infraestructura sensible y política de login `Active`/`Suspended`, incluida reactivación, excepción de `PLATFORM_ADMIN` e invalidación de una sesión existente mediante `/api/auth/me`. PENDIENTE PRUEBA FUNCIONAL EN NAVEGADOR con PostgreSQL MASTER real, ambos cambios de estado, bloqueo/restauración de login y operación, y responsive aproximadamente a 390 px.

Fuera de alcance: creación/eliminación, edición de infraestructura, usuarios/sesiones completos y dashboard global.

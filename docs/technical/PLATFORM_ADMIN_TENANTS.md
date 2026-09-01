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

Suspender conserva usuarios, datos y base tenant. `CurrentTenantResolver` ya exige estado `Active`, por lo que un tenant suspendido no puede obtener contexto operacional. Reactivar restaura esa resolución sin modificar la base tenant.

## Auditoría

Las acciones registran `TENANT_SUSPENDED` o `TENANT_ACTIVATED`, actor derivado de `NameIdentifier`, tenant objetivo, transición, IP, User-Agent y timestamp. El frontend no envía actor.

## UI

`/admin/tenants` ofrece búsqueda, filtro, loading, error, reintento, vacío y listado responsive. `/admin/tenants/{id}` muestra datos MASTER, owner, actividad, auditoría reciente y confirmación antes de suspender/reactivar. No usa mocks ni consulta tenant DB.

## Pruebas y pendientes

Las pruebas cubren política de transición, actualización/auditoría, 404, propagación server-side de filtros y ausencia de infraestructura sensible en contratos. PENDIENTE PRUEBA FUNCIONAL EN NAVEGADOR con PostgreSQL MASTER real, ambos cambios de estado, bloqueo/restauración operacional y responsive aproximadamente a 390 px.

Fuera de alcance: creación/eliminación, edición de infraestructura, usuarios/sesiones completos y dashboard global.

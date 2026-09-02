# PLATFORM_ADMIN Audit Explorer

## Alcance y origen

TEN-028 habilita un explorador global, histórico y de solo lectura sobre `platform_audit_logs` en PostgreSQL MASTER. Reutiliza `IPlatformAuditService` y `PlatformAuditLog`; no crea una segunda infraestructura de escritura ni modifica el esquema.

## Endpoints

- `GET /api/admin/audit`: listado paginado.
- `GET /api/admin/audit/{id}`: detalle seguro; devuelve 404 cuando el evento no existe.

Ambos exigen `PLATFORM_ADMIN`. `TENANT_OWNER` recibe 403 por la autorización backend, independientemente de la navegación Web.

## Listado, búsqueda y filtros

El listado acepta `search`, `action`, `from`, `to`, `tenant`, `actor`, `targetUser`, `page` y `pageSize`. La búsqueda cubre acción, descripción, actor, correo, tenant y usuario objetivo cuando las relaciones siguen disponibles. Acción es coincidencia exacta normalizada a mayúsculas; actor, tenant y usuario usan coincidencia parcial sin distinguir mayúsculas.

`from` y `to` representan días UTC. Desde es inclusivo y hasta incluye el día completo mediante un límite SQL exclusivo al inicio del día siguiente. Un rango invertido devuelve 400.

La paginación es server-side, inicia en 1 y limita `pageSize` a 50. El resultado incluye total, página, tamaño e items. El orden predeterminado es `CreatedAt DESC`, seguido de `Id DESC` para estabilidad.

## Campos visibles y datos faltantes

El listado muestra acción, descripción, fecha, actor, tenant e IP disponible. El detalle agrega entidad, usuario objetivo, User-Agent e ID del evento para soporte. Los actores, tenants o usuarios eliminados se resuelven con `LEFT JOIN`: el evento permanece visible y la UI muestra “No disponible”.

## Exclusiones de seguridad

Los DTO son proyecciones explícitas. No devuelven `PasswordHash`, passwords, stamps, cookies, tokens, connection strings, `DatabaseName`, secretos, credenciales ni headers completos. `OldValues` y `NewValues` se excluyen porque el modelo histórico los guarda como texto arbitrario y no existe todavía un esquema allowlist que garantice su seguridad.

## Eficiencia y multi-tenancy

`PlatformAuditQueryService` depende exclusivamente de `MasterDbContext`. No usa `TenantDbContext`, `ITenantDbContextFactory` ni bases `sweetsecrets_tenant_*`.

La consulta usa `AsNoTracking`, filtros y joins SQL, `CountAsync`, `OrderByDescending`, `Skip`, `Take` y `Select`. No utiliza `AsEnumerable`, filtrado cliente, carga completa ni N+1. Una prueba con Npgsql y `ToQueryString` verifica traducción de búsqueda, filtros, orden y paginación.

## UI

`/admin/audit` integra búsqueda, acción, tenant, actor, usuario objetivo, rango de fechas, estados loading/error/reintento/vacío, badges y paginación. `/admin/audit/{id}` presenta el detalle seguro. `AdminNavMenu` enlaza el módulo funcional.

Los filtros y resultados pasan a una columna en pantallas cercanas a 390 px, el texto largo hace wrap y no se requiere scroll horizontal.

## Pruebas y pendientes

Las pruebas cubren autorización, forwarding de búsqueda/filtros/fechas, máximo pageSize, rango inválido, vacío, detalle, 404, relaciones faltantes, contrato seguro, dependencia MASTER-only y traducción Npgsql.

PENDIENTE PRUEBA FUNCIONAL EN NAVEGADOR con MASTER real, eventos históricos, filtros combinados, paginación y responsive aproximadamente a 390 px.

La política de retención/purga, exportación y un esquema seguro tipado para metadata quedan fuera de TEN-028.

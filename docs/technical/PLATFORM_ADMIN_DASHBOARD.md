# PLATFORM_ADMIN Platform Dashboard

## Alcance y endpoint

TEN-027 convierte `/admin` en el resumen operacional global de SweetSecrets. `GET /api/admin/dashboard` exige `PLATFORM_ADMIN` y devuelve un DTO agregado; no devuelve entidades EF ni acepta tenant, rol o actor desde Web.

## Métricas y origen

Todas las cifras se calculan en PostgreSQL MASTER con `AsNoTracking`, `CountAsync`, `GroupBy`, filtros, `Distinct`, orden y límite:

- tenants: total y distribución `Active`, `Suspended`, `Provisioning`, `Failed` y `Disabled`, desde `tenants.Status`;
- usuarios: total, `TENANT_OWNER`, `PLATFORM_ADMIN`, bloqueados y correos no confirmados, desde Identity MASTER y sus roles;
- sesiones activas: registros `user_sessions.IsActive`;
- usuarios online: usuarios distintos con sesión activa y `LastActivityAt` dentro de los últimos cinco minutos;
- recientes: cinco tenants, cinco usuarios y cinco eventos de auditoría, ordenados por fecha descendente.

La presencia es derivada, no tiempo real, y no usa SignalR. Varias sesiones activas del mismo usuario cuentan una vez en online.

## Actividad reciente

Tenants y usuarios recientes enlazan a sus detalles administrativos. La auditoría muestra sólo acción, descripción y fecha. El dashboard no expone IP, User-Agent, valores anteriores/nuevos completos ni un explorador global; este último puede ser un issue independiente.

## Eficiencia y traducción

No se cargan tablas completas para contar y no existe N+1. Las listas usan `OrderBy`, `ThenBy`, `Take` y proyección final. Pruebas con el provider Npgsql generan SQL mediante `ToQueryString` para verificar `DISTINCT`, filtro temporal, orden y límite, evitando el patrón de proyección intermedia que falló durante TEN-026.

## MASTER-only y seguridad

`PlatformDashboardQueryService` depende sólo de `MasterDbContext`. No usa `ITenantDbContextFactory`, no enumera bases tenant y no consulta productos, recetas, settings, costos o ventas. El contrato no expone `DatabaseName`, connection strings, Identity stamps, hashes, cookies, tokens ni secretos.

Los health checks existentes conservan sus endpoints `/health/live` y `/health/ready`. El dashboard no los duplica, no hace HTTP loopback y no presenta un estado health ambiguo.

## UI

`/admin` incluye loading, error, reintento, valores cero y estados vacíos. Presenta cards de tenants, usuarios, online y sesiones; barras CSS por estado; resumen de usuarios; tres listas recientes y accesos a Tenants, Usuarios y Sesiones. No agrega librerías de gráficas.

La cuadrícula se reduce progresivamente y pasa a una columna aproximadamente a 390 px; textos largos usan wrap y las fechas se apilan.

## Pruebas y pendientes

Las pruebas cubren rol requerido, conteos/estados mapeados, MASTER vacío, contratos seguros y traducción Npgsql de online y recientes. Queda pendiente la validación funcional en navegador con MASTER real, datos vacíos/reales y responsive aproximadamente a 390 px.

Se descartaron métricas de productos, recetas, ventas, ingresos, utilidad y costos porque pertenecen a bases tenant y no representan salud operacional de plataforma. También quedan fuera health embebido, auditoría completa, notificaciones y presencia en tiempo real.

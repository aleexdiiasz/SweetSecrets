# PLATFORM_ADMIN Users & Sessions

## Alcance

TEN-026 habilita administración de identidades y sesiones existentes exclusivamente sobre MASTER. No crea `TENANT_USER`, no cambia roles y no abre ninguna base tenant.

## API

- `GET /api/admin/users`: búsqueda server-side por nombre, correo, tenant o código; filtros por rol, bloqueo y estado online; paginación limitada a 50.
- `GET /api/admin/users/{id}`: cuenta, tenant, actividad, diez sesiones y diez movimientos de auditoría recientes.
- `POST /api/admin/users/{id}/block` y `/unblock`.
- `GET /api/admin/users/sessions`: sesiones globales con búsqueda, filtro activa/cerrada y paginación.
- `POST /api/admin/users/sessions/{id}/revoke`: revocación real de sesiones tenant.

Todos requieren `PLATFORM_ADMIN`. El actor, su sesión, IP y User-Agent se derivan del request autenticado.

## Datos visibles y ocultos

Se muestran nombre, correo, rol, tenant/código, activo, bloqueado, correo confirmado, creación, último login, última actividad, online, inicio/actividad/fin de sesión, IP y User-Agent. No se exponen `PasswordHash`, stamps, tokens, cookies, `DatabaseName`, connection strings ni secretos.

## Bloqueo y desbloqueo

Bloquear establece `IsBlocked`, actualiza el `SecurityStamp`, termina todas las sesiones activas y registra `USER_BLOCKED`. Es una acción sobre la identidad: no cambia `TenantStatus` ni la base tenant. Desbloquear registra `USER_UNBLOCKED` y sólo restaura login cuando la cuenta está activa, el correo cumple la política y el tenant permanece `Active`; nunca reactiva un tenant suspendido.

Las cuentas `PLATFORM_ADMIN` son de solo lectura en TEN-026. No pueden bloquearse/desbloquearse ni pueden revocarse sus sesiones desde esta UI; esto incluye impedir autobloqueo y la revocación accidental de la sesión administrativa actual.

## Sesiones y revocación real

`UserActivityMiddleware` valida que `session_id` corresponda al usuario y continúe activo en MASTER antes de actualizar actividad. Si fue cerrado, ejecuta sign-out, reemplaza el principal por uno anónimo y la autorización devuelve `401`. Por tanto, `SESSION_REVOKED` y `USER_BLOCKED` invalidan el acceso de la cookie en la siguiente petición; no se limita a cambiar un registro visual.

Una revocación correcta establece fin y razón `SESSION_REVOKED` mediante el servicio existente y registra auditoría con actor y usuario objetivo. No se muestran cookies ni tokens.

## Estado online

Se conserva la semántica existente: online significa al menos una sesión activa con `LastActivityAt` dentro de los últimos cinco minutos. Es presencia derivada, no tiempo real; no usa SignalR.

## Web y responsive

`/admin/users`, `/admin/users/{id}` y `/admin/sessions` usan `AdminLayout`, estados de carga/error/vacío, reintento, confirmación y prevención de doble envío. Los listados cambian a tarjetas/columnas reducidas aproximadamente a 390 px y evitan anchos fijos.

## Seguridad y pruebas

La búsqueda y filtros se ejecutan en PostgreSQL MASTER. Los contratos y servicios no dependen de `ITenantDbContextFactory`. Las pruebas cubren autorización, propagación server-side, 404, ausencia de campos sensibles, acciones rechazadas, sesiones y revocación. Queda pendiente la prueba funcional en navegador con MASTER real y responsive a 390 px.

Durante la validación manual se detectó que el listado ordenaba sobre una proyección intermedia `UserRow` que Npgsql no podía traducir. La consulta corregida conserva los joins y aplica búsqueda, filtros, `OrderBy`, `Skip` y `Take` sobre las columnas originales antes de proyectar `PlatformUserSummary`. Pruebas de traducción con el provider relacional Npgsql generan el SQL real mediante `ToQueryString`; no se usa `AsEnumerable`, materialización previa ni evaluación client-side.

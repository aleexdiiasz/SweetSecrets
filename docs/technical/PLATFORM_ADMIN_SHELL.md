# PLATFORM_ADMIN Administration Shell

## Issue y alcance

TEN-024 crea la base visual y de navegación exclusiva para `PLATFORM_ADMIN`. No implementa administración funcional, métricas globales ni datos simulados.

## Separación de áreas

- `TENANT_OWNER` usa `/`, `MainLayout` y Productos, Recetas, Configuración y Cuenta.
- `PLATFORM_ADMIN` usa `/admin` y `AdminLayout`.

`/admin` requiere `[Authorize(Roles = "PLATFORM_ADMIN")]`. Las páginas tenant permanecen protegidas con `TENANT_OWNER`. La navegación visual no reemplaza la autorización backend.

## Login y sesión

El login devuelve los roles obtenidos por ASP.NET Core Identity. `AuthenticatedAreaNavigation` selecciona `PLATFORM_ADMIN -> /admin`, `TENANT_OWNER -> /` y, sin rol válido, `/login`.

`ApiAuthenticationStateProvider` y `/api/auth/me` reconstruyen la sesión al recargar. Cuando una ruta rechaza acceso, `RedirectToLogin` dirige al autenticado hacia el home permitido para su rol y al anónimo hacia Login. No se guarda el rol en localStorage ni se recibe desde formularios.

## AdminLayout

Incluye sidebar diferenciado, encabezado de plataforma, correo autenticado y logout. No ofrece “Mi cuenta”: TEN-020 y sus endpoints continúan limitados a `TENANT_OWNER`.

Inicio, Tenants, Usuarios y Sesiones son rutas administrativas activas. TEN-025 implementa `/admin/tenants`; TEN-026 implementa `/admin/users`, detalle y `/admin/sessions`. Auditoría conserva por ahora sus vistas recientes dentro de los detalles y el módulo global continúa pendiente. Detalles: `docs/technical/PLATFORM_ADMIN_TENANTS.md` y `docs/technical/PLATFORM_ADMIN_USERS_SESSIONS.md`.

## Seguridad y multi-tenancy

`PLATFORM_ADMIN` tiene `TenantId = null`. El shell no consume Dashboard tenant, Products, Recipes, Settings ni `ITenantDbContextFactory`. La landing es estática y no resuelve tenant. Las APIs administrativas existentes operan sobre MASTER y requieren `PLATFORM_ADMIN`.

La revisión de separación detectó que `ProductsController` conservaba autorización autenticada genérica; TEN-024 lo alinea con Dashboard, Recipes y Settings mediante autorización explícita `TENANT_OWNER`, evitando que un administrador global dispare resolución tenant accidental.

No se envían `TenantId`, `DatabaseName`, connection strings, roles objetivo ni identificadores de usuario para resolver la sesión.

## Responsive

El layout cambia a estructura vertical en pantallas pequeñas. Header, identidad y logout se reajustan, las tarjetas pasan a una columna y los módulos pendientes se ocultan del menú compacto. Está preparado para revisión aproximadamente a 390 px.

## Pruebas

Las pruebas cubren destinos post-login, autorización `PLATFORM_ADMIN`, permanencia de `TENANT_OWNER` en APIs tenant y los flujos administrativos de usuarios/sesiones.

PENDIENTE PRUEBA FUNCIONAL EN NAVEGADOR: login con ambos roles, rechazo cruzado, recarga directa, logout administrativo y responsive aproximadamente a 390 px.

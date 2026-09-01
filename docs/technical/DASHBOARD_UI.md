# Tenant Owner Dashboard

## Issue

TEN-022 - Tenant Owner Dashboard Improvements

## Objetivo

La ruta protegida `/` presenta un resumen operacional real del tenant autenticado. Sustituye las tarjetas informativas de TEN-014 y consume un único endpoint agregado.

## API y arquitectura

`GET /api/dashboard` requiere el rol `TENANT_OWNER`. Web no envía `TenantId`, nombre de base ni connection string. `DashboardQueryService` obtiene `TenantDbContext` mediante `ITenantDbContextFactory`, por lo que todas las consultas se ejecutan exclusivamente sobre la base resuelta desde la identidad autenticada.

El endpoint devuelve productos y recetas totales/activos, costo promedio de las recetas activas y los cinco productos y recetas modificados o creados más recientemente. Las consultas usan `AsNoTracking`, agregaciones, proyecciones, `OrderBy` y `Take(5)`. No cargan catálogos completos ni consultan por elemento.

El costo promedio describe el costo de elaboración registrado por receta activa. No representa ingresos, ventas ni utilidad. No se muestran métricas financieras inventadas porque esas entidades todavía no existen.

## Interfaz

La página incluye carga, error con reintento y vacíos independientes. Presenta tarjetas de métricas, actividad reciente y accesos rápidos a Productos, Recetas y Configuración. El diseño cambia a una columna en pantallas pequeñas y está preparado para revisión aproximadamente a 390 px.

## Seguridad

- autorización explícita `TENANT_OWNER` en controller y página;
- resolución tenant únicamente desde la sesión autenticada;
- sin acceso a MASTER ni parámetros capaces de seleccionar otro tenant;
- sin soporte `TENANT_USER` en este alcance.

## Pruebas

Las pruebas unitarias verifican autorización, mapeo de métricas/actividad y respuesta vacía. La separación entre tenants usa el mismo `ITenantDbContextFactory` de los módulos operacionales.

## Validación manual pendiente

- datos reales y actualización tras modificar productos/recetas;
- tenant vacío, error y reintento;
- aislamiento visual entre dos tenants;
- enlaces rápidos y responsive aproximadamente a 390 px.

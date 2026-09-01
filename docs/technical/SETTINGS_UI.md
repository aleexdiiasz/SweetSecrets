# Tenant Settings UI

## Issue y objetivo

TEN-017 convierte `/configuracion` en la interfaz operacional de configuración para `TENANT_OWNER`, conectada al backend multi-tenant existente y alineada visualmente con TEN-014, TEN-015 y TEN-016.

## Arquitectura y seguridad

La Web consume exclusivamente la API. No envía `TenantId`, `DatabaseName` ni `ConnectionString`. El contexto conserva el flujo: usuario autenticado -> MASTER -> tenant `Active` -> `ITenantDbContextFactory` -> PostgreSQL del tenant.

La página y `SettingsController` están restringidos a `TENANT_OWNER` mediante la constante `PlatformRoles.TenantOwner`. `TENANT_USER` permanece pausado y no forma parte del alcance funcional.

## Endpoints utilizados

- `GET /api/settings`
- `PUT /api/settings/{key}`

El cliente también implementa `GET /api/settings/{key}` para consultas puntuales, aunque la página carga el listado completo.

No se agregaron endpoints, contratos ni migraciones.

## SettingsApiClient

Se agregó `SettingsApiClient` como servicio scoped. Reutiliza los contratos existentes:

- `SettingListItemResponse`
- `SettingDetailResponse`
- `UpdateSettingRequest`
- `UpdateSettingResponse`

Centraliza el manejo de respuestas 400, 401, 403, 404 y 409, preservando el mensaje del backend cuando está disponible.

## Comportamiento funcional

- carga configuraciones reales del tenant autenticado;
- muestra clave, valor, descripción y fecha de actualización;
- permite editar los valores devueltos por la API;
- presenta estados de carga, vacío, error, guardado y confirmación;
- recarga el listado después de una actualización correcta;
- conserva la cultura `es-MX` de SweetSecrets.Web.

## MULTIPLIER

`MULTIPLIER` inicia actualmente en `3`. La UI lo trata como decimal, permite valores como `4.5` y exige un valor mayor que cero como validación básica. Antes de enviarlo se serializa mediante `InvariantCulture`, como exige el backend. El backend conserva la validación definitiva.

La configuración `settings.MULTIPLIER` y `Recipe.Multiplier` son datos distintos:

```text
settings.MULTIPLIER
→ multiplicador predeterminado para nuevas recetas

Recipe.Multiplier
→ multiplicador propio y persistente de cada receta
```

Al abrir “Nueva receta”, la Web consulta el valor vigente mediante `SettingsApiClient`. Al guardar, ese valor viaja en `CreateRecipeRequest.Multiplier` y queda persistido como `Recipe.Multiplier`. El usuario puede ajustarlo antes de guardar o editarlo posteriormente.

Cambiar la configuración general no modifica ni recalcula recetas existentes.

## Responsive

El CSS aislado adapta las tarjetas, metadatos, mensajes, formulario y botones a una columna por debajo de 768 px, incluyendo aproximadamente 390 px. No se agregó ningún framework de UI.

## Pruebas técnicas

- `dotnet build .\SweetSecrets.slnx`: correcto, 0 advertencias y 0 errores.
- `dotnet test .\SweetSecrets.slnx --no-build`: correcto.
- `git diff --check`: correcto.

## Pendiente

PENDIENTE PRUEBA FUNCIONAL EN NAVEGADOR.

Debe validarse visualmente la carga real, actualización de `MULTIPLIER`, confirmación, errores, responsive aproximadamente a 390 px y el escenario de integración con recetas descrito en la regla oficial.

## Limitaciones

- La integración ocurre en la Web reutilizando `SettingsApiClient`; el backend de recetas continúa recibiendo el multiplicador explícito mediante su contrato existente.
- No modifica ni recalcula recetas existentes.
- No implementa creación o eliminación de claves de configuración.
- No implementa `TENANT_USER` ni administración global de plataforma.

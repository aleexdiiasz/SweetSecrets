# Tenant Recipes UI

## Issue y objetivo

TEN-016 convierte `/recetas` en la interfaz operacional de recetas para `TENANT_OWNER`, conectada al backend multi-tenant existente y siguiendo los patrones visuales de TEN-014 y TEN-015.

## Seguridad multi-tenant

La Web no envía `TenantId`, `DatabaseName` ni `ConnectionString`. Todas las operaciones conservan el flujo: usuario autenticado -> MASTER -> tenant `Active` -> `ITenantDbContextFactory` -> PostgreSQL del tenant. `RecipesController` quedó restringido a `TENANT_OWNER` y la página mantiene la misma protección.

## Endpoints utilizados

- `GET /api/recipes`
- `GET /api/recipes/{id}`
- `POST /api/recipes`
- `PUT /api/recipes/{id}`
- `POST /api/recipes/{id}/items`
- `PUT /api/recipes/{recipeId}/items/{itemId}`
- `DELETE /api/recipes/{recipeId}/items/{itemId}`
- `GET /api/recipes/{id}/cost-history`
- `PATCH /api/recipes/{id}/active`
- `GET /api/products`
- `GET /api/units`
- `GET /api/settings/MULTIPLIER`

## Cliente Web

Se agregó `RecipesApiClient` como servicio scoped. Centraliza las operaciones HTTP y transforma respuestas 400, 401, 403, 404 y 409 en mensajes útiles, preservando el mensaje del backend cuando existe. Se reutilizan `ProductsApiClient` y `UnitsApiClient`.

## Funcionalidad

- listado real y búsqueda local por nombre;
- alta y edición de nombre, descripción y multiplicador;
- detalle de receta e ingredientes;
- alta de ingrediente con producto activo, cantidad y unidad compatible;
- edición de cantidad y eliminación de ingredientes;
- costos y precios recibidos del backend, sin recalcularlos en la Web;
- historial de costos con motivos traducidos para presentación;
- desactivación y reactivación, incluida la sincronización de costos del backend al reactivar.

## Multiplicador predeterminado

La creación de recetas reutiliza `SettingsApiClient` para consultar `GET /api/settings/MULTIPLIER`. El valor vigente de `settings.MULTIPLIER` aparece como valor inicial al abrir “Nueva receta”; ya no existe un `3m` hardcodeado.

La consulta se repite al abrir el formulario para evitar usar un valor obsoleto. Si el setting no existe, no es un decimal mayor que cero o falla la API, la UI muestra el problema y no abre el alta con un valor inventado.

Al guardar, el valor se envía mediante `CreateRecipeRequest.Multiplier` y queda persistido como `Recipe.Multiplier`. Después de creada, la receta conserva ese valor propio y puede modificarse individualmente desde su edición.

Cambiar `settings.MULTIPLIER` no modifica ni recalcula recetas existentes.

## Conversiones

La UI compara `MeasurementType` entre la unidad del producto y las unidades activas. Esto permite `GR <-> KG`, `ML <-> L` y mantiene `PZA` dentro de `Count`. El backend sigue siendo autoridad sobre compatibilidad, conversión y costos.

## Estados y responsive

La página muestra estados de carga, vacío, búsqueda sin resultados, error y guardado. El CSS aislado conserva tablas con desplazamiento horizontal y adapta encabezados, formularios y resúmenes a una columna aproximadamente a 390 px.

## Pruebas realizadas

- Se cargó correctamente la ruta real `/recetas` y se mostró el listado existente.
- Se validaron el alta y la edición de una receta.
- La receta inició con multiplicador `3` y se actualizó correctamente a `4`.
- Se validaron agregar ingrediente, editar su cantidad y eliminarlo.
- El backend recalculó correctamente los costos y el historial mostró los cambios correspondientes.
- Se validó el ciclo completo de desactivar y reactivar la receta.
- Se comprobó la conversión `GR -> KG` con un producto cuyo costo era `$0.300000/g`: una cantidad de `0.1 kg` produjo un costo de `$30.00`.
- El resultado final validado fue `Recipe.TotalCost = $30.00`, `Multiplier = 4` y `SuggestedPrice = $120.00`.
- La interfaz responsive se probó aproximadamente a 390 px. Los formularios, botones y el desplazamiento horizontal funcionaron correctamente.
- No se observaron errores visuales ni funcionales durante las pruebas en navegador.
- `dotnet build .\SweetSecrets.slnx`: correcto, 0 advertencias y 0 errores.
- `dotnet test .\SweetSecrets.slnx --no-build`: correcto.
- `git diff --check`: correcto.

## Limitaciones

El contrato actual `UpdateRecipeItemRequest` solo permite editar la cantidad. Para cambiar producto o unidad se elimina y agrega nuevamente el ingrediente. No se modificó el esquema PostgreSQL ni se agregó una migración.

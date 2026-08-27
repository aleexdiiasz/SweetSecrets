# Recipes

## 1. Objetivo

Documentar el módulo operacional de recetas de SweetSecrets.

Las recetas pertenecen exclusivamente a la base PostgreSQL del tenant autenticado.

Toda operación utiliza:

```text
ITenantDbContextFactory
```

El frontend no selecciona:

```text
TenantId
DatabaseName
ConnectionString
```

---

## 2. Flujo de acceso

```text
Usuario autenticado
        ↓
CurrentTenantResolver
        ↓
MASTER
        ↓
Tenant Active
        ↓
CurrentTenantDbContextFactory
        ↓
TenantDbContext
        ↓
recipes / recipe_items
```

---

## 3. Entidad Recipe

Campos principales:

```text
Id
Name
Description
Multiplier
TotalCost
SuggestedPrice
IsActive
CreatedAt
CreatedBy
UpdatedAt
UpdatedBy
Items
```

La receta mantiene una colección:

```text
ICollection<RecipeItem>
```

---

## 4. Entidad RecipeItem

Relaciona:

```text
Recipe
Product
Unit
```

Campos:

```text
Id
RecipeId
ProductId
Quantity
UnitId
UnitCost
TotalCost
```

Cada ingrediente conserva el costo unitario utilizado para calcular su costo dentro de la receta.

---

## 5. Servicios

Consultas:

```text
IRecipeQueryService
RecipeQueryService
```

Comandos:

```text
IRecipeCommandService
RecipeCommandService
```

Ambos trabajan mediante:

```text
ITenantDbContextFactory
```

---

## 6. Endpoints actuales

```text
GET    /api/recipes
GET    /api/recipes/{id}
POST   /api/recipes
PUT    /api/recipes/{id}

POST   /api/recipes/{id}/items
PUT    /api/recipes/{recipeId}/items/{itemId}
DELETE /api/recipes/{recipeId}/items/{itemId}

GET    /api/recipes/{id}/cost-history

PATCH  /api/recipes/{id}/active
```

Todos requieren autenticación.

---

## 7. Listado de recetas

Endpoint:

```text
GET /api/recipes
```

Devuelve:

```text
Id
Name
Description
Multiplier
TotalCost
SuggestedPrice
IsActive
```

Las recetas se ordenan por nombre.

La consulta utiliza:

```text
AsNoTracking
```

---

## 8. Detalle de receta

Endpoint:

```text
GET /api/recipes/{id}
```

Devuelve:

```text
Id
Name
Description
Multiplier
TotalCost
SuggestedPrice
IsActive
CreatedAt
CreatedBy
UpdatedAt
UpdatedBy
Items
```

Si la receta no existe dentro del tenant actual:

```text
404 Not Found
```

---

## 9. Detalle de ingredientes

Cada elemento de:

```text
Items
```

incluye:

```text
Id
ProductId
ProductName
Quantity
UnitId
UnitCode
UnitName
UnitSymbol
UnitCost
TotalCost
```

Esto permite que la PWA pueda reconstruir completamente una receta sin consultar directamente PostgreSQL.

---

## 10. Crear receta

Endpoint:

```text
POST /api/recipes
```

Datos:

```text
Name
Description
Multiplier
```

Validaciones:

```text
Name obligatorio
Name <= 200 caracteres
Multiplier > 0
Nombre no duplicado dentro del tenant
```

Al crear una receta todavía sin ingredientes:

```text
TotalCost = 0
SuggestedPrice = 0
IsActive = true
```

Auditoría:

```text
CreatedAt = UTC
CreatedBy = usuario autenticado
```

---

## 11. Multiplicador

El multiplicador se utiliza para calcular el precio sugerido.

Regla:

```text
SuggestedPrice = TotalCost × Multiplier
```

Ejemplo validado:

```text
TotalCost = 45
Multiplier = 4
SuggestedPrice = 180
```

El precio sugerido se redondea a:

```text
2 decimales
```

utilizando:

```text
MidpointRounding.AwayFromZero
```

---

## 12. Agregar ingrediente

Endpoint:

```text
POST /api/recipes/{id}/items
```

Datos:

```text
ProductId
Quantity
UnitId
```

Validaciones:

```text
RecipeId válido
ProductId válido
UnitId válido
Quantity > 0
receta existente
receta activa
producto existente
producto activo
unidad existente
unidad activa
producto no duplicado dentro de la receta
```

---

## 13. Regla actual de unidades

Actualmente:

```text
RecipeItem.UnitId
```

debe coincidir con:

```text
Product.UnitId
```

No se realizan conversiones automáticas entre:

```text
KG ↔ GR
L  ↔ ML
```

porque todavía no existe una capa formal de conversiones de unidades.

No se deben introducir conversiones implícitas sin una regla de dominio explícita.

---

## 14. Costo del ingrediente

Regla:

```text
RecipeItem.TotalCost
=
RecipeItem.Quantity × RecipeItem.UnitCost
```

El `UnitCost` proviene del producto.

Ejemplo validado:

```text
Quantity = 250
UnitCost = 0.18
TotalCost = 45
```

El costo del ingrediente se conserva con precisión de hasta:

```text
6 decimales
```

---

## 15. Costo total de receta

Después de agregar, editar o eliminar ingredientes:

```text
Recipe.TotalCost
=
SUM(RecipeItem.TotalCost)
```

Ejemplo:

```text
Ingrediente 1 = 45
Ingrediente 2 = 20
Ingrediente 3 = 10
```

Resultado:

```text
Recipe.TotalCost = 75
```

---

## 16. Precio sugerido

Después de recalcular el costo total:

```text
Recipe.SuggestedPrice
=
Recipe.TotalCost × Recipe.Multiplier
```

Ejemplo validado:

```text
TotalCost = 54
Multiplier = 4
SuggestedPrice = 216
```

---

## 17. Editar receta

Endpoint:

```text
PUT /api/recipes/{id}
```

Permite modificar:

```text
Name
Description
Multiplier
```

Al cambiar el multiplicador:

```text
TotalCost
```

permanece igual.

Se recalcula:

```text
SuggestedPrice
```

También actualiza:

```text
UpdatedAt
UpdatedBy
```

---

## 18. Editar cantidad de ingrediente

Endpoint:

```text
PUT /api/recipes/{recipeId}/items/{itemId}
```

Permite modificar:

```text
Quantity
```

Flujo:

```text
Quantity
↓
RecipeItem.TotalCost
↓
Recipe.TotalCost
↓
Recipe.SuggestedPrice
```

Ejemplo validado:

```text
Quantity = 300
UnitCost = 0.18
```

Resultado:

```text
RecipeItem.TotalCost = 54
Recipe.TotalCost = 54
Multiplier = 4
Recipe.SuggestedPrice = 216
```

---

## 19. Eliminar ingrediente

Endpoint:

```text
DELETE /api/recipes/{recipeId}/items/{itemId}
```

El ingrediente se elimina físicamente de:

```text
recipe_items
```

Después se recalculan:

```text
Recipe.TotalCost
Recipe.SuggestedPrice
UpdatedAt
UpdatedBy
```

Si se elimina el único ingrediente:

```text
Items = []
TotalCost = 0
SuggestedPrice = 0
```

---

## 20. Historial de costos

Entidad:

```text
RecipeCostHistory
```

Tabla:

```text
recipe_cost_history
```

Campos:

```text
Id
RecipeId
PreviousCost
NewCost
Reason
CreatedAt
```

---

## 21. Eventos de historial

Actualmente se generan automáticamente:

```text
RECIPE_ITEM_ADDED
RECIPE_ITEM_UPDATED
RECIPE_ITEM_REMOVED
```

Solo se crea historial cuando:

```text
PreviousCost != NewCost
```

---

## 22. Historial al agregar ingrediente

Ejemplo validado:

```text
PreviousCost = 0
NewCost = 54
Reason = RECIPE_ITEM_ADDED
```

---

## 23. Historial al editar ingrediente

Ejemplo validado:

```text
PreviousCost = 54
NewCost = 63
Reason = RECIPE_ITEM_UPDATED
```

---

## 24. Historial al eliminar ingrediente

Ejemplo validado:

```text
PreviousCost = 63
NewCost = 0
Reason = RECIPE_ITEM_REMOVED
```

---

## 25. Consulta de historial

Endpoint:

```text
GET /api/recipes/{id}/cost-history
```

Devuelve:

```text
Id
RecipeId
PreviousCost
NewCost
Reason
CreatedAt
```

Orden:

```text
CreatedAt DESC
```

Es decir, del movimiento más reciente al más antiguo.

---

## 26. Soft delete de receta

Endpoint:

```text
PATCH /api/recipes/{id}/active
```

Desactivar:

```json
{
  "isActive": false
}
```

No se elimina físicamente:

```text
Recipe
RecipeItems
RecipeCostHistory
```

La receta permanece disponible para trazabilidad.

---

## 27. Reactivar receta

El mismo endpoint permite:

```json
{
  "isActive": true
}
```

También actualiza:

```text
UpdatedAt
UpdatedBy
```

---

## 28. Restricciones de receta inactiva

Una receta inactiva no puede:

```text
agregar ingredientes
editar ingredientes
eliminar ingredientes
```

Esto evita modificar accidentalmente recetas archivadas o desactivadas.

---

## 29. Auditoría

Creación:

```text
CreatedAt
CreatedBy
```

Actualizaciones:

```text
UpdatedAt
UpdatedBy
```

El usuario se obtiene mediante:

```text
ClaimTypes.NameIdentifier
```

---

## 30. Aislamiento multi-tenant

Ningún endpoint de recetas recibe:

```text
TenantId
DatabaseName
ConnectionString
```

para seleccionar contexto.

El flujo obligatorio es:

```text
usuario autenticado
↓
MASTER
↓
tenant Active
↓
ITenantDbContextFactory
↓
TenantDbContext
↓
recipes / recipe_items
```

---

## 31. Pruebas realizadas

Tenant:

```text
000004
```

Base:

```text
sweetsecrets_tenant_000004
```

Receta utilizada:

```text
PASTEL CHOCOLATE PRUEBA TEN-008
```

Id:

```text
46683e43-3c55-4d67-8ebd-9c9731d707f1
```

---

## 32. Validaciones HTTP realizadas

```text
GET /api/recipes
→ 200

GET /api/recipes/{id inexistente}
→ 404

POST /api/recipes
→ 200

GET /api/recipes/{id}
→ 200

POST /api/recipes/{id}/items
→ 200

PUT /api/recipes/{recipeId}/items/{itemId}
→ 200

DELETE /api/recipes/{recipeId}/items/{itemId}
→ 200

PUT /api/recipes/{id}
→ 200

GET /api/recipes/{id}/cost-history
→ 200

PATCH inactive
→ 204

GET inactive
→ 200 / IsActive false

PATCH active
→ persistido correctamente

GET active
→ 200 / IsActive true
```

---

## 33. Validación real de cálculo

Producto de prueba:

```text
UnitCost = 0.18
```

Prueba inicial:

```text
250 × 0.18 = 45
```

Receta con multiplicador:

```text
45 × 3 = 135
```

Posteriormente:

```text
Multiplier = 4
45 × 4 = 180
```

Cambio de cantidad:

```text
300 × 0.18 = 54
54 × 4 = 216
```

Otra actualización:

```text
350 × 0.18 = 63
63 × 4 = 252
```

---

## 34. Validación PostgreSQL del historial

Se comprobaron físicamente los registros:

```text
0  → 54  RECIPE_ITEM_ADDED
54 → 63  RECIPE_ITEM_UPDATED
63 → 0   RECIPE_ITEM_REMOVED
```

La misma información fue validada posteriormente mediante la API.

---

## 35. Estado actual del módulo

Implementado:

```text
listar recetas
detalle
crear
editar
multiplicador
precio sugerido
agregar ingredientes
editar cantidad
eliminar ingredientes
recalcular costo
detalle con ingredientes
historial automático de costos
consulta de historial
soft delete
reactivación
auditoría
aislamiento tenant
```

---

## 36. Pendiente relacionado

Todavía no está implementada la propagación automática:

```text
cambio de Product.UnitCost
↓
buscar RecipeItems afectados
↓
actualizar RecipeItem.UnitCost
↓
recalcular RecipeItem.TotalCost
↓
recalcular Recipe.TotalCost
↓
recalcular Recipe.SuggestedPrice
↓
crear RecipeCostHistory
```

Este comportamiento corresponde al siguiente bloque de recálculo automático.

También queda pendiente diseñar formalmente conversiones como:

```text
KG ↔ GR
L  ↔ ML
```

si el producto y la receta necesitan utilizar unidades distintas.

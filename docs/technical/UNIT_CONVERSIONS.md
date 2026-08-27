# Unit Conversions

## 1. Objetivo

Documentar el soporte formal de conversiones de unidades de SweetSecrets.

Este comportamiento corresponde a:

```text
TEN-010 - Unit Conversions
```

El objetivo es permitir que un producto tenga una unidad base y pueda utilizarse en una receta mediante otra unidad compatible.

Ejemplos:

```text
KG ↔ GR
L  ↔ ML
```

---

## 2. MeasurementType

Se agregó:

```text
MeasurementType
```

Valores:

```text
Mass   = 1
Volume = 2
Count  = 3
```

Permite determinar si dos unidades son compatibles.

Ejemplos:

```text
GR → Mass
KG → Mass

ML → Volume
L  → Volume

PZA → Count
```

---

## 3. Unit

La entidad:

```text
Unit
```

ahora contiene:

```text
Id
Code
Name
Symbol
MeasurementType
ConversionFactor
IsActive
```

---

## 4. ConversionFactor

`ConversionFactor` representa cuántas unidades base contiene una unidad.

Configuración:

```text
GR  → Mass   → 1
KG  → Mass   → 1000

ML  → Volume → 1
L   → Volume → 1000

PZA → Count  → 1
```

---

## 5. Fórmula general

El costo para la unidad utilizada en una receta se calcula:

```text
RecipeItem.UnitCost
=
Product.UnitCost
× RecipeUnit.ConversionFactor
÷ ProductUnit.ConversionFactor
```

---

## 6. Ejemplo KG → GR

Producto:

```text
Unit = KG
UnitCost = 80
```

Ingrediente:

```text
Quantity = 250
Unit = GR
```

Factores:

```text
KG = 1000
GR = 1
```

Conversión:

```text
80 × 1 / 1000
=
0.08 por GR
```

Costo:

```text
250 × 0.08
=
20
```

Resultado:

```text
RecipeItem.UnitCost = 0.08
RecipeItem.TotalCost = 20
```

---

## 7. Ejemplo GR → KG

Producto:

```text
Unit = GR
UnitCost = 0.30
```

Ingrediente:

```text
Quantity = 0.25
Unit = KG
```

Conversión:

```text
0.30 × 1000 / 1
=
300 por KG
```

Costo:

```text
0.25 × 300
=
75
```

Resultado:

```text
RecipeItem.UnitCost = 300
RecipeItem.TotalCost = 75
```

---

## 8. Compatibilidad

Se permiten conversiones cuando:

```text
Product.Unit.MeasurementType
=
RecipeItem.Unit.MeasurementType
```

Permitido:

```text
GR ↔ KG
ML ↔ L
```

No permitido:

```text
GR ↔ ML
KG ↔ L
PZA ↔ GR
PZA ↔ ML
```

---

## 9. Error de incompatibilidad

Si se intenta agregar un ingrediente utilizando una unidad incompatible:

```text
409 Conflict
```

Mensaje:

```text
La unidad del ingrediente no es compatible con la unidad del producto.
```

---

## 10. Validación de factores

Para realizar conversiones:

```text
Product.Unit.ConversionFactor > 0
RecipeItem.Unit.ConversionFactor > 0
```

Si la configuración no es válida:

```text
La configuración de conversión de unidades no es válida.
```

---

## 11. RecipeItem

`RecipeItem.UnitId` representa la unidad utilizada específicamente dentro de la receta.

Por lo tanto:

```text
Product.UnitId
```

y:

```text
RecipeItem.UnitId
```

pueden ser distintos siempre que pertenezcan al mismo `MeasurementType`.

---

## 12. Agregar ingrediente

Al agregar un ingrediente:

```text
Product
↓
Product.Unit
↓
Recipe Unit
↓
validar MeasurementType
↓
convertir Product.UnitCost
↓
RecipeItem.UnitCost
↓
RecipeItem.TotalCost
↓
Recipe.TotalCost
↓
Recipe.SuggestedPrice
```

---

## 13. Recálculo automático

TEN-009 fue adaptado para respetar conversiones.

Cuando cambia:

```text
Product.UnitCost
```

las recetas activas se recalculan utilizando la unidad específica de cada `RecipeItem`.

Ejemplo validado:

```text
Product.Unit = GR
Product.UnitCost = 0.40

RecipeItem.Unit = KG
Quantity = 0.25
```

Conversión:

```text
0.40 × 1000 = 400 por KG
```

Resultado:

```text
RecipeItem.UnitCost = 400
RecipeItem.TotalCost = 100
Recipe.TotalCost = 100
Recipe.SuggestedPrice = 400
```

---

## 14. Historial por cambio de costo

Cuando cambia el costo del producto:

```text
RecipeCostHistory.Reason
=
PRODUCT_UNIT_COST_CHANGED
```

Ejemplo validado:

```text
PreviousCost = 75
NewCost = 100
Reason = PRODUCT_UNIT_COST_CHANGED
```

---

## 15. Cambio de unidad base del producto

TEN-010 permite modificar:

```text
Product.UnitId
```

aunque el producto ya esté utilizado en recetas, siempre que la nueva unidad sea compatible.

Permitido:

```text
GR → KG
KG → GR
ML → L
L → ML
```

---

## 16. Cambio incompatible de unidad

Si el producto está utilizado por recetas y se intenta cambiar entre categorías incompatibles:

```text
Mass → Volume
Mass → Count
Volume → Count
```

la actualización se rechaza.

Respuesta validada:

```text
409 Conflict
```

Mensaje:

```text
No se puede cambiar la unidad del producto porque no es compatible con las unidades utilizadas en sus recetas.
```

---

## 17. Atomicidad

Un cambio incompatible rechazado no modifica:

```text
Product.UnitId
PurchaseQuantity
PurchasePrice
UnitCost
UpdatedAt
UpdatedBy
```

Esto fue validado funcionalmente.

---

## 18. Recálculo por cambio de unidad

El recálculo se ejecuta cuando:

```text
Product.UnitCost cambia
```

o:

```text
Product.UnitId cambia
```

Esto es necesario porque el mismo valor numérico de `UnitCost` puede representar magnitudes diferentes.

Ejemplo:

```text
0.40 por GR
```

no equivale a:

```text
0.40 por KG
```

---

## 19. targetUnit

Durante `ProductCommandService.UpdateAsync`, la nueva unidad se carga explícitamente como:

```text
targetUnit
```

Esto evita utilizar accidentalmente:

```text
product.Unit
```

que representa la unidad anterior cargada desde PostgreSQL durante la misma operación.

---

## 20. Historial por cambio de unidad

Cuando el cambio de unidad modifica el costo de la receta:

```text
RecipeCostHistory.Reason
=
PRODUCT_UNIT_CHANGED
```

Ejemplo validado:

```text
PreviousCost = 120
NewCost = 20.10
Reason = PRODUCT_UNIT_CHANGED
```

---

## 21. Reactivación de recetas

Las recetas inactivas siguen conservando sus costos históricos cuando cambian los productos.

Al reactivarlas:

```text
RecipeItem
↓
RecipeItem.Unit
↓
Product
↓
Product.Unit
↓
convertir UnitCost vigente
↓
recalcular ingrediente
↓
recalcular receta
```

---

## 22. Reactivación con conversión

Ejemplo validado:

Producto:

```text
Unit = KG
UnitCost = 100
```

Ingrediente:

```text
Quantity = 250
Unit = GR
```

Conversión:

```text
100 × 1 / 1000
=
0.10 por GR
```

Resultado:

```text
RecipeItem.UnitCost = 0.10
RecipeItem.TotalCost = 25
```

La receta completa quedó:

```text
TotalCost = 25.10
SuggestedPrice = 100.40
```

---

## 23. Historial al reactivar

Si la sincronización modifica el costo:

```text
RecipeCostHistory.Reason
=
RECIPE_REACTIVATED_COST_SYNC
```

Ejemplo validado:

```text
PreviousCost = 20.10
NewCost = 25.10
Reason = RECIPE_REACTIVATED_COST_SYNC
```

---

## 24. Migración TEN002

Migración:

```text
TEN002_AddUnitConversions
```

Agrega a:

```text
units
```

las columnas:

```text
MeasurementType integer NOT NULL
ConversionFactor numeric(18,6) NOT NULL
```

---

## 25. Backfill

La migración actualiza unidades existentes:

```text
GR  → 1 → 1
KG  → 1 → 1000
ML  → 2 → 1
L   → 2 → 1000
PZA → 3 → 1
```

Primero las columnas se crean como nullable.

Después:

```text
UPDATE units
```

asigna los valores.

Finalmente:

```text
SET NOT NULL
```

Esto evita asignar silenciosamente valores incorrectos a unidades desconocidas.

---

## 26. Tenant template

TEN002 fue aplicada correctamente sobre:

```text
sweetsecrets_tenant_template
```

La estructura quedó:

```text
ConversionFactor numeric(18,6) NOT NULL
MeasurementType integer NOT NULL
```

El template no contenía unidades seed al momento de la validación.

---

## 27. Tenants migrados

TEN002 fue aplicada a los tenants activos existentes:

```text
sweetsecrets_tenant_000001
sweetsecrets_tenant_000003
sweetsecrets_tenant_000004
```

Los tres registran:

```text
TEN001_InitialTenant
TEN002_AddUnitConversions
```

---

## 28. Tenant 000001

`sweetsecrets_tenant_000001` corresponde a una etapa temprana de pruebas.

Estado encontrado:

```text
Units = 0
Products = 0
Settings = 0
Recipes = 0
```

Por esto el backfill de TEN002 reportó:

```text
UPDATE 0
```

La migración se aplicó correctamente.

No se introdujo seed artificial para conservar el estado histórico de esa prueba.

---

## 29. Tenant 000003

TEN002 actualizó:

```text
5 unidades
```

Backfill correcto.

---

## 30. Tenant 000004

TEN002 actualizó:

```text
5 unidades
```

Valores comprobados:

```text
GR  | Mass   | 1
KG  | Mass   | 1000
ML  | Volume | 1
L   | Volume | 1000
PZA | Count  | 1
```

Este tenant fue utilizado para las pruebas funcionales de TEN-010.

---

## 31. Tenant fallido

El tenant:

```text
000002
```

permanece:

```text
Status = Failed
```

No fue modificado durante TEN-010.

Se conserva como evidencia histórica del fallo de provisioning previamente validado.

---

## 32. Pruebas funcionales

Se validó:

```text
GR → KG
KG → GR
```

También:

```text
Mass → Volume
```

fue rechazado correctamente.

---

## 33. Prueba GR → KG

Producto:

```text
Unit = GR
UnitCost = 0.30
```

Ingrediente:

```text
0.25 KG
```

Resultado:

```text
UnitCost = 300
TotalCost = 75
```

---

## 34. Prueba KG → GR

Producto:

```text
Unit = KG
UnitCost = 80
```

Ingrediente:

```text
250 GR
```

Resultado:

```text
UnitCost = 0.08
TotalCost = 20
```

---

## 35. Receta con conversiones combinadas

Se validó una receta con:

```text
Ingrediente 1:
0.25 KG
TotalCost = 100

Ingrediente 2:
250 GR
TotalCost = 20
```

Resultado:

```text
Recipe.TotalCost = 120
Recipe.SuggestedPrice = 480
```

---

## 36. Cambio compatible de unidad

Se modificó un producto usado por recetas:

```text
GR → KG
```

La actualización fue permitida.

El ingrediente:

```text
0.25 KG
```

fue recalculado utilizando la nueva unidad base del producto.

Resultado:

```text
RecipeItem.UnitCost = 0.40
RecipeItem.TotalCost = 0.10
```

La receta quedó:

```text
TotalCost = 20.10
SuggestedPrice = 80.40
```

Historial:

```text
PRODUCT_UNIT_CHANGED
```

---

## 37. Cambio incompatible

Se intentó:

```text
KG → ML
```

Resultado:

```text
409 Conflict
```

y el producto conservó:

```text
Unit = KG
PurchaseQuantity = 800
PurchasePrice = 320
UnitCost = 0.40
```

---

## 38. Estado actual

TEN-010 implementa:

```text
MeasurementType
ConversionFactor
TEN002
backfill de unidades
seed actualizado
compatibilidad Mass
compatibilidad Volume
Count
GR ↔ KG
ML ↔ L
costo convertido al agregar ingredientes
recálculo automático con conversiones
cambio compatible de unidad de producto
bloqueo de cambios incompatibles
historial PRODUCT_UNIT_CHANGED
reactivación con conversiones
aislamiento database-per-tenant
```

---

## 39. Precisión

Las conversiones de costo utilizan:

```text
6 decimales
```

con:

```text
MidpointRounding.AwayFromZero
```

`SuggestedPrice` conserva:

```text
2 decimales
```

---

## 40. Regla crítica

Nunca realizar conversiones entre unidades con diferente:

```text
MeasurementType
```

La compatibilidad debe determinarse por metadatos de dominio, no por condicionales hardcodeados basados únicamente en `Code`.

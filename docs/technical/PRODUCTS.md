# Products

## 1. Objetivo

Documentar el módulo operacional de productos de SweetSecrets.

Cada operación trabaja exclusivamente contra la base PostgreSQL del tenant autenticado.

El módulo utiliza:

```text
ITenantDbContextFactory
```

para resolver el contexto correcto.

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
products
```

El frontend no envía:

```text
TenantId
DatabaseName
ConnectionString
```

para seleccionar la base.

---

## 3. Entidad Product

Campos principales:

```text
Id
Name
PurchaseQuantity
UnitId
PurchasePrice
UnitCost
IsActive
CreatedAt
CreatedBy
UpdatedAt
UpdatedBy
```

También existe navegación hacia:

```text
Unit
```

mediante:

```text
Product.UnitId
→
Unit.Id
```

---

## 4. Relación Product → Unit

La relación se configura explícitamente en:

```text
TenantDbContext
```

con comportamiento:

```text
DeleteBehavior.Restrict
```

Esto evita eliminar una unidad utilizada por productos.

---

## 5. Servicios de consulta

Interfaz:

```text
IProductQueryService
```

Implementación:

```text
ProductQueryService
```

Responsabilidades actuales:

```text
GetAllAsync
GetByIdAsync
```

Ambas operaciones crean el contexto mediante:

```text
ITenantDbContextFactory
```

---

## 6. Listado de productos

Endpoint:

```text
GET /api/products
```

Devuelve:

```text
Id
Name
PurchaseQuantity
UnitId
UnitCode
UnitName
UnitSymbol
PurchasePrice
UnitCost
IsActive
```

Los productos se ordenan por nombre.

La consulta utiliza:

```text
AsNoTracking
```

porque es una operación de lectura.

---

## 7. Detalle de producto

Endpoint:

```text
GET /api/products/{id}
```

Además de la información del listado devuelve:

```text
CreatedAt
CreatedBy
UpdatedAt
UpdatedBy
```

Si el producto no existe dentro de la base del tenant actual:

```text
404 Not Found
```

Aunque el usuario conozca el Guid de un producto perteneciente a otro tenant, la consulta se ejecuta únicamente contra su propia base.

---

## 8. Servicios de comando

Interfaz:

```text
IProductCommandService
```

Implementación:

```text
ProductCommandService
```

Responsabilidades:

```text
CreateAsync
UpdateAsync
SetActiveAsync
```

---

## 9. Crear producto

Endpoint:

```text
POST /api/products
```

Datos requeridos:

```text
Name
PurchaseQuantity
UnitId
PurchasePrice
```

Validaciones:

```text
Name obligatorio
Name <= 200 caracteres
PurchaseQuantity > 0
UnitId válido
Unit activa
PurchasePrice >= 0
nombre no duplicado dentro del tenant
```

Se permite:

```text
PurchasePrice = 0
```

porque el catálogo original ya contiene productos sin precio y una usuaria puede registrar un producto antes de conocer el costo definitivo.

---

## 10. Cálculo de costo unitario

Se calcula:

```text
UnitCost = PurchasePrice / PurchaseQuantity
```

con precisión de hasta:

```text
6 decimales
```

y:

```text
MidpointRounding.AwayFromZero
```

Ejemplo validado:

```text
PurchaseQuantity = 1000
PurchasePrice    = 150
UnitCost         = 0.150000
```

---

## 11. Auditoría de creación

Al crear un producto:

```text
CreatedAt = UTC
CreatedBy = usuario autenticado
```

El usuario se obtiene desde:

```text
ClaimTypes.NameIdentifier
```

---

## 12. Editar producto

Endpoint:

```text
PUT /api/products/{id}
```

Permite modificar:

```text
Name
PurchaseQuantity
UnitId
PurchasePrice
```

Validaciones equivalentes a creación.

También valida que no exista otro producto con el mismo nombre.

---

## 13. Auditoría de actualización

Cuando se modifica:

```text
UpdatedAt = UTC
UpdatedBy = usuario autenticado
```

---

## 14. Historial de precio

Tabla:

```text
product_price_history
```

Entidad:

```text
ProductPriceHistory
```

Se registra automáticamente cuando cambia:

```text
PurchasePrice
```

o cuando cambia:

```text
UnitCost
```

aunque el precio total permanezca igual.

---

## 15. Datos registrados en historial

```text
ProductId
PreviousPrice
NewPrice
PreviousUnitCost
NewUnitCost
ChangedBy
ChangedAt
```

---

## 16. Regla de historial

Ejemplo:

Antes:

```text
PurchasePrice    = 150
PurchaseQuantity = 1000
UnitCost         = 0.150000
```

Después:

```text
PurchasePrice    = 150
PurchaseQuantity = 750
UnitCost         = 0.200000
```

Aunque el precio no cambió, el costo unitario sí cambió.

Por lo tanto se debe crear historial.

Si solo cambia el nombre y precio/costo permanecen iguales, no se genera un registro en `product_price_history`.

---

## 17. Validación real de historial

Producto de prueba:

```text
986bb7f3-c735-4517-b752-b25f1b56e6cc
```

Cambio realizado:

```text
PreviousPrice    = 150
NewPrice         = 180
PreviousUnitCost = 0.150000
NewUnitCost      = 0.180000
```

El registro quedó correctamente guardado con el usuario autenticado en:

```text
ChangedBy
```

---

## 18. Desactivar producto

Endpoint:

```text
PATCH /api/products/{id}/active
```

Body:

```json
{
  "isActive": false
}
```

No se elimina físicamente el registro.

Se utiliza:

```text
IsActive = false
```

---

## 19. Soft delete

No se utiliza:

```text
DELETE FROM products
```

La razón es conservar:

- historial de precios;
- referencias futuras con recetas;
- trazabilidad;
- posibilidad de reactivación.

---

## 20. Reactivar producto

El mismo endpoint permite:

```json
{
  "isActive": true
}
```

Al cambiar estado también se actualizan:

```text
UpdatedAt
UpdatedBy
```

---

## 21. Validación real de soft delete

Se validó:

```text
PATCH → IsActive = false
GET   → producto sigue existiendo
```

Después:

```text
PATCH → IsActive = true
GET   → producto activo nuevamente
```

El producto nunca fue eliminado físicamente.

---

## 22. Endpoints actuales

```text
GET   /api/products
GET   /api/products/{id}
POST  /api/products
PUT   /api/products/{id}
PATCH /api/products/{id}/active
```

Todos requieren usuario autenticado.

---

## 23. Aislamiento multi-tenant

Ningún endpoint de productos recibe:

```text
TenantId
DatabaseName
ConnectionString
```

El aislamiento siempre sigue:

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
products
```

---

## 24. Pruebas realizadas

Tenant utilizado:

```text
000004
```

Base:

```text
sweetsecrets_tenant_000004
```

Se validó:

```text
GET listado → 200
GET detalle → 200
POST producto → 200
PUT producto → 200
PATCH inactive → 204
GET inactive → 200 / IsActive false
PATCH active → 204
GET active → 200 / IsActive true
```

---

## 25. Producto de prueba

Creado durante TEN-007:

```text
PRODUCTO PRUEBA TEN-007
```

Datos iniciales:

```text
PurchaseQuantity = 1000
Unit = GR
PurchasePrice = 150
UnitCost = 0.15
```

Después se actualizó:

```text
PurchasePrice = 180
UnitCost = 0.18
```

El historial fue validado directamente en PostgreSQL.

---

## 26. Estado del módulo

Implementado:

```text
listar
detalle
crear
editar
calcular UnitCost
CreatedBy
UpdatedBy
historial de precio
desactivar
reactivar
aislamiento tenant
```

Pendiente relacionado:

```text
recálculo automático de recetas cuando cambie UnitCost
```

Ese comportamiento se implementará en el módulo de recetas / recálculo, no dentro del CRUD básico de productos.

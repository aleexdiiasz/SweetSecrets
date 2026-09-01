# Estado actual del proyecto

## Actualizacion TEN-016 - Tenant Recipes UI

TEN-016 implementa la interfaz funcional /recetas para TENANT_OWNER: listado, busqueda, alta, edicion, ingredientes, unidades compatibles por MeasurementType, costos del backend, historial y activacion/desactivacion.

Se agrego RecipesApiClient, se reutilizaron ProductsApiClient y UnitsApiClient, y RecipesController quedo restringido a TENANT_OWNER. No requiere migraciones. Detalle: docs/technical/RECIPES_UI.md.

## Validacion funcional en navegador

Se valido correctamente:

- carga real de /recetas y listado existente;
- alta y edicion de receta;
- multiplicador inicial 3 y cambio a 4;
- agregar ingrediente, editar cantidad y eliminar ingrediente;
- recalculo de costos e historial de costos;
- desactivar y reactivar receta;
- conversion GR -> KG: producto con costo $0.300000/g y 0.1 kg con costo $30.00;
- Recipe.TotalCost = $30.00;
- Multiplier = 4;
- SuggestedPrice = $120.00;
- responsive probado aproximadamente a 390 px;
- formularios, botones y scroll horizontal correctos.

No se observaron errores visuales ni funcionales.

---

## Fecha de estado

2026-08-27

## Proyecto

SweetSecrets

## Fase actual

Conversión formal de unidades implementada y validada.

El backend multi-tenant ya permite que un producto y un ingrediente de receta utilicen unidades diferentes siempre que pertenezcan al mismo tipo de medida.

Actualmente se está cerrando:

```text
TEN-010 - Unit Conversions
```

Estado:

```text
Implementado
Build OK
Pruebas funcionales OK
Migración TEN002 aplicada y validada
Documentación UNIT_CONVERSIONS.md creada
Pendiente Git / Pull Request
```

TEN-009 fue integrado a `develop` mediante el Pull Request #6.

TEN-010 agrega:

```text
MeasurementType
ConversionFactor
GR ↔ KG
ML ↔ L
conversiones al agregar RecipeItem
recálculo automático con conversiones
cambio compatible de Product.UnitId
protección de cambios incompatibles
sincronización al reactivar recetas
```

---

# Stack confirmado

- .NET 10
- ASP.NET Core
- Blazor WebAssembly PWA
- Entity Framework Core
- PostgreSQL 18.6
- ASP.NET Core Identity
- Docker
- Docker Compose
- Swagger para desarrollo

---

# Solución

Proyectos existentes:

```text
SweetSecrets.slnx

src/
  SweetSecrets.Api
  SweetSecrets.Web
  SweetSecrets.Domain
  SweetSecrets.Application
  SweetSecrets.Infrastructure
  SweetSecrets.Contracts

tests/
  SweetSecrets.UnitTests
  SweetSecrets.IntegrationTests

docs/
```

Proyectos:

- SweetSecrets.Api
- SweetSecrets.Web
- SweetSecrets.Domain
- SweetSecrets.Application
- SweetSecrets.Infrastructure
- SweetSecrets.Contracts
- SweetSecrets.UnitTests
- SweetSecrets.IntegrationTests

Todos compilan correctamente con:

```powershell
dotnet build
```

---

# Arquitectura general

La solución sigue separación por capas.

## SweetSecrets.Domain

Contiene:

- entidades;
- enums;
- reglas estructurales del dominio.

No depende de Infrastructure ni API.

## SweetSecrets.Application

Contiene:

- interfaces;
- contratos internos;
- casos de uso;
- modelos de aplicación.

No debe conocer detalles específicos de PostgreSQL, HTTP o Swagger.

## SweetSecrets.Infrastructure

Contiene:

- Entity Framework Core;
- PostgreSQL;
- ASP.NET Core Identity;
- implementaciones de servicios;
- acceso a MASTER;
- acceso dinámico a tenant;
- migrations;
- provisioning;
- seed.

## SweetSecrets.Api

Contiene:

- controllers;
- middleware;
- configuración de DI;
- autenticación;
- Swagger;
- exposición HTTP.

## SweetSecrets.Contracts

Contiene DTOs públicos compartidos entre API y clientes.

## SweetSecrets.Web

Frontend:

```text
Blazor WebAssembly PWA
```

La UI final todavía no está implementada.

---

# PostgreSQL

PostgreSQL funciona mediante Docker.

Versión:

```text
PostgreSQL 18.6
```

Contenedor de desarrollo:

```text
sweetsecrets-postgres
```

Usuario de desarrollo:

```text
sweetsecrets_admin
```

Puerto local:

```text
127.0.0.1:5432
```

Las contraseñas no están almacenadas en código ni en Git.

Se utilizan:

- Docker secrets para PostgreSQL.
- User Secrets para desarrollo ASP.NET Core.

Archivo de desarrollo:

```text
compose.dev.yml
```

La carpeta:

```text
docker/
```

está ignorada por Git debido a los secrets.

---

# MASTER DB

Base:

```text
sweetsecrets_master
```

Contexto:

```text
MasterDbContext
```

Responsabilidad:

MASTER contiene información global de plataforma.

No contiene:

- productos tenant;
- recetas tenant;
- configuraciones operativas tenant.

---

# Migraciones MASTER

Migraciones aplicadas:

```text
MST001_InitialMaster
MST002_AddAuditAndSessions
MST003_AddTenantNumberSequence
```

## MST001

Creó:

- Identity;
- tenants;
- usuarios;
- roles.

## MST002

Agregó:

```text
platform_audit_logs
user_sessions
```

## MST003

Agregó la secuencia PostgreSQL:

```text
tenant_number_seq
```

Configuración:

```text
START 1
INCREMENT 1
```

Se utiliza para generar códigos tenant.

Ejemplo:

```text
000001
000002
000003
000004
```

Las secuencias PostgreSQL garantizan unicidad.

No garantizan numeración continua sin huecos.

Un número consumido no se reutiliza aunque un proceso posterior falle.

---

# Tablas MASTER actuales

```text
__EFMigrationsHistory
tenants
platform_users
platform_roles
platform_user_roles
platform_user_claims
platform_user_logins
platform_role_claims
platform_user_tokens
platform_audit_logs
user_sessions
```

---

# Tenant MASTER

Entidad:

```text
SweetSecrets.Domain.Entities.Master.Tenant
```

Campos principales:

```text
Id
Code
Name
DatabaseName
Status
CreatedAt
UpdatedAt
```

---

# Estados tenant

Enum:

```text
TenantStatus
```

Estados:

```text
Provisioning = 1
Active       = 2
Suspended    = 3
Disabled     = 4
Failed       = 5
```

Regla:

```text
Solo un tenant Active puede operar.
```

---

# Roles

Roles creados:

```text
PLATFORM_ADMIN
TENANT_OWNER
TENANT_USER
```

Definidos mediante:

```text
PlatformRoles
```

---

# PLATFORM_ADMIN

Existe un administrador inicial de plataforma.

Características:

```text
TenantId = null
IsActive = true
IsBlocked = false
Role = PLATFORM_ADMIN
```

Las credenciales no están almacenadas en Git.

El PLATFORM_ADMIN administra la plataforma.

No pertenece automáticamente a ningún tenant.

Por diseño:

```text
PLATFORM_ADMIN
TenantId = null
```

no puede obtener un contexto operacional tenant mediante el resolver normal.

---

# TENANT_OWNER

Representa al propietario principal de una cuenta SweetSecrets.

Características:

```text
TenantId != null
Role = TENANT_OWNER
```

El TenantId relaciona al usuario con el registro correspondiente en:

```text
MASTER.tenants
```

---

# TENANT_USER

Rol preparado para permitir en el futuro usuarios adicionales dentro de una misma cuenta tenant.

Todavía no existe flujo funcional de creación y administración de TENANT_USER.

---

# ASP.NET Core Identity

Entidad:

```text
ApplicationUser
```

Hereda de:

```text
IdentityUser<Guid>
```

Campos adicionales:

```text
TenantId
FullName
IsActive
IsBlocked
LastLoginAt
LastActivityAt
CreatedAt
```

---

# Tablas Identity

Nombres configurados:

```text
platform_users
platform_roles
platform_user_roles
platform_user_claims
platform_user_logins
platform_role_claims
platform_user_tokens
```

---

# Autenticación

Implementado:

- login;
- logout;
- cookies ASP.NET Core Identity;
- sesiones;
- auditoría de login;
- auditoría de logout;
- auditoría de login fallido;
- lockout;
- endpoint de usuario actual;
- autorización basada en roles;
- autoregistro público.

Servicio:

```text
IAuthenticationService
```

Implementación:

```text
AuthenticationService
```

---

# Configuración de contraseña

Identity exige:

```text
mínimo 10 caracteres
dígito
minúscula
mayúscula
carácter no alfanumérico
```

Correo único:

```text
RequireUniqueEmail = true
```

Lockout:

```text
5 intentos
15 minutos
```

---

# Cookies de autenticación

Cookie:

```text
SweetSecrets.Auth
```

Configuración:

```text
HttpOnly = true
Secure = Always
SameSite = Lax
SlidingExpiration = true
Expiration = 8 horas
```

---

# Comportamiento API 401 / 403

Las rutas:

```text
/api/*
```

no deben redirigir a páginas MVC.

Cuando falta autenticación:

```text
401 Unauthorized
```

Cuando falta autorización:

```text
403 Forbidden
```

No deben devolver:

```text
/Account/Login
/Account/AccessDenied
```

Esto ya fue validado.

---

# Endpoints de autenticación

Actualmente:

```text
POST /api/auth/register
POST /api/auth/login
POST /api/auth/logout
GET  /api/auth/me
```

---

# Login

Flujo:

```text
email
password
↓
buscar usuario
↓
IsActive
↓
IsBlocked
↓
validar password
↓
crear UserSession
↓
actualizar LastLoginAt
↓
actualizar LastActivityAt
↓
SignInAsync
↓
LOGIN_SUCCESS
```

Claims utilizados:

```text
NameIdentifier
session_id
tenant_id
```

`tenant_id` solo existe cuando el usuario pertenece a un tenant.

---

# Logout

Flujo:

```text
usuario autenticado
↓
finalizar UserSession
↓
SignOutAsync
↓
LOGOUT
```

---

# Sesiones

Entidad:

```text
UserSession
```

Tabla:

```text
user_sessions
```

Registra:

```text
Id
UserId
StartedAt
LastActivityAt
EndedAt
IpAddress
UserAgent
IsActive
EndReason
```

---

# UserActivityMiddleware

Middleware:

```text
UserActivityMiddleware
```

Para usuarios autenticados:

1. obtiene `session_id`;
2. obtiene `NameIdentifier`;
3. actualiza sesión;
4. actualiza `ApplicationUser.LastActivityAt`.

Orden relevante:

```text
UseHttpsRedirection
UseAuthentication
UserActivityMiddleware
UseAuthorization
MapControllers
```

---

# Usuario online

Un usuario se considera online según:

```text
sesión activa
+
LastActivityAt dentro de una ventana reciente
```

La ventana utilizada en las consultas administrativas es de aproximadamente:

```text
5 minutos
```

---

# Administración de usuarios

Servicio:

```text
IPlatformUserAdminService
```

Implementación:

```text
PlatformUserAdminService
```

Consulta:

```text
IPlatformUserQueryService
```

---

# Endpoints administrativos de usuarios

```text
GET  /api/admin/users
POST /api/admin/users/{userId}/block
POST /api/admin/users/{userId}/unblock
```

Solo:

```text
PLATFORM_ADMIN
```

puede acceder.

---

# Información administrativa de usuario

La consulta devuelve:

```text
Id
TenantId
Email
FullName
IsActive
IsBlocked
IsOnline
LastLoginAt
LastActivityAt
CreatedAt
```

---

# Bloqueo de usuario

Al bloquear:

```text
IsBlocked = true
↓
UpdateSecurityStamp
↓
cerrar sesiones activas
↓
USER_BLOCKED
```

También se evita que el administrador se bloquee a sí mismo.

---

# Desbloqueo

Al desbloquear:

```text
IsBlocked = false
↓
UpdateSecurityStamp
↓
USER_UNBLOCKED
```

---

# Auditoría global

Interfaz:

```text
IPlatformAuditService
```

Implementación:

```text
PlatformAuditService
```

Tabla:

```text
platform_audit_logs
```

Entidad:

```text
PlatformAuditLog
```

Campos:

```text
Id
UserId
TenantId
Action
Entity
EntityId
Description
OldValues
NewValues
IpAddress
UserAgent
CreatedAt
```

---

# Eventos de auditoría

Implementados o previstos:

```text
LOGIN_SUCCESS
LOGIN_FAILED
LOGIN_LOCKED_OUT
LOGOUT
USER_BLOCKED
USER_UNBLOCKED
TENANT_CREATED
TENANT_FAILED
NOTIFICATION_SENT
```

---

# Arquitectura multi-tenant

Modelo:

```text
Database-per-tenant
```

MASTER:

```text
sweetsecrets_master
```

Tenant:

```text
sweetsecrets_tenant_000001
sweetsecrets_tenant_000002
sweetsecrets_tenant_000003
sweetsecrets_tenant_000004
...
```

Cada tenant tiene una base PostgreSQL independiente.

---

# Regla crítica de aislamiento

Cada usuario tenant debe trabajar únicamente contra la base asociada a su cuenta.

Flujo:

```text
Usuario autenticado
        ↓
MASTER.platform_users
        ↓
TenantId
        ↓
MASTER.tenants
        ↓
Status = Active
        ↓
DatabaseName
        ↓
TenantDbContext
        ↓
PostgreSQL tenant
```

El frontend nunca debe decidir:

```text
TenantId para cambiar de contexto
DatabaseName
ConnectionString
```

---

# Provisioning de tenant

Implementado.

Servicio:

```text
ITenantProvisioningService
```

Implementación:

```text
TenantProvisioningService
```

---

# Flujo de provisioning

```text
Solicitud
↓
ITenantIdentifierGenerator
↓
tenant_number_seq
↓
generar Code
↓
generar DatabaseName
↓
registrar MASTER
Status = Provisioning
↓
crear PostgreSQL DB
↓
aplicar TEN001
↓
ejecutar TenantSeedService
↓
Status = Active
↓
TENANT_CREATED
```

---

# Identificador tenant

Interfaz:

```text
ITenantIdentifierGenerator
```

Implementación:

```text
PostgresTenantIdentifierGenerator
```

Resultado:

```text
TenantIdentifier
```

Contiene:

```text
Number
Code
DatabaseName
```

Ejemplo:

```text
Number       = 4
Code         = 000004
DatabaseName = sweetsecrets_tenant_000004
```

---

# Tenant registry

Servicio:

```text
ITenantRegistryService
```

Implementación:

```text
TenantRegistryService
```

Responsabilidades:

```text
CreateProvisioningAsync
MarkActiveAsync
MarkFailedAsync
```

---

# Creación física de base tenant

Interfaz:

```text
ITenantDatabaseManager
```

Implementación:

```text
PostgresTenantDatabaseManager
```

Valida nombres con formato:

```text
sweetsecrets_tenant_XXXXXX
```

donde:

```text
XXXXXX = exactamente seis dígitos
```

No permite crear arbitrariamente:

```text
sweetsecrets_master
sweetsecrets_tenant_template
otro_nombre
```

---

# Manejo de fallo de provisioning

Si ocurre una excepción:

```text
Provisioning
↓
Failed
↓
TENANT_FAILED
```

La base no se elimina automáticamente.

Razones:

- trazabilidad;
- diagnóstico;
- evitar ocultar errores;
- evitar operaciones destructivas automáticas.

---

# Tenant DB

Contexto:

```text
TenantDbContext
```

Migraciones actuales:

```text
TEN001_InitialTenant
TEN002_AddUnitConversions
```

`TEN001_InitialTenant` crea la estructura operacional inicial del tenant.

`TEN002_AddUnitConversions` agrega soporte formal de conversiones a la tabla:

```text
units
```

mediante:

```text
MeasurementType integer NOT NULL
ConversionFactor numeric(18,6) NOT NULL
```

La arquitectura continúa siendo:

```text
1 PostgreSQL DB independiente por tenant
```

---

# Tenant template

Base de desarrollo:

```text
sweetsecrets_tenant_template
```

Se utiliza para probar migraciones tenant.

No pertenece a un usuario real.

Migraciones aplicadas:

```text
TEN001_InitialTenant
TEN002_AddUnitConversions
```

TEN002 fue validada estructuralmente en el template.

Al momento de la validación el template contenía:

```text
0 unidades
```

por lo que se validó ahí la estructura de columnas y el backfill real se comprobó posteriormente en tenants con datos.

---

# Tablas tenant

Actualmente:

```text
__EFMigrationsHistory
units
products
recipes
recipe_items
settings
product_price_history
recipe_cost_history
```

---

# Unidades

Entidad:

```text
Unit
```

Campos actuales:

```text
Id
Code
Name
Symbol
MeasurementType
ConversionFactor
IsActive
```

Enum:

```text
MeasurementType
```

Valores:

```text
Mass   = 1
Volume = 2
Count  = 3
```

Seed inicial:

```text
GR  - Gramo      - Mass   - 1
KG  - Kilogramo  - Mass   - 1000
ML  - Mililitro  - Volume - 1
L   - Litro      - Volume - 1000
PZA - Pieza      - Count  - 1
```

Total inicial:

```text
5
```

`ConversionFactor` representa cuántas unidades base contiene cada unidad.

Compatibilidad permitida:

```text
GR ↔ KG
ML ↔ L
```

`PZA` pertenece a `Count` y no se convierte hacia unidades de masa o volumen.

La compatibilidad se determina por:

```text
MeasurementType
```

y no mediante condicionales hardcodeados por `Code` durante la operación normal.

---

# Productos

Módulo operacional implementado.

Entidad:

```text
Product
```

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

Navegación:

```text
Product.Unit
```

Relación:

```text
Product.UnitId
→
Unit.Id
```

La relación se configura explícitamente en `TenantDbContext` con:

```text
DeleteBehavior.Restrict
```

Esto evita eliminar una unidad que esté siendo utilizada por productos.

## Servicios

Consultas:

```text
IProductQueryService
ProductQueryService
```

Comandos:

```text
IProductCommandService
ProductCommandService
```

Todos utilizan:

```text
ITenantDbContextFactory
```

por lo que las operaciones se ejecutan exclusivamente contra la base del tenant autenticado.

## Endpoints

```text
GET   /api/products
GET   /api/products/{id}
POST  /api/products
PUT   /api/products/{id}
PATCH /api/products/{id}/active
```

Todos requieren autenticación.

## Listado

`GET /api/products` devuelve:

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

Las consultas de lectura utilizan:

```text
AsNoTracking
```

## Detalle

`GET /api/products/{id}` devuelve además:

```text
CreatedAt
CreatedBy
UpdatedAt
UpdatedBy
```

Si el producto no existe dentro de la base del tenant autenticado:

```text
404 Not Found
```

Conocer el Guid de un producto perteneciente a otro tenant no permite acceder a él porque la consulta se ejecuta contra una base PostgreSQL diferente.

## Crear producto

Endpoint:

```text
POST /api/products
```

Datos:

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
Unidad activa
PurchasePrice >= 0
Nombre no duplicado dentro del tenant
```

Se permite:

```text
PurchasePrice = 0
```

porque el catálogo legacy contiene productos sin precio y una usuaria puede registrar un producto antes de conocer su costo definitivo.

Al crear:

```text
CreatedAt = UTC
CreatedBy = usuario autenticado
```

## Editar producto

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

Al actualizar:

```text
UpdatedAt = UTC
UpdatedBy = usuario autenticado
```

## Costo unitario

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

## Soft delete

No se elimina físicamente el producto.

Endpoint:

```text
PATCH /api/products/{id}/active
```

Desactivar:

```json
{
  "isActive": false
}
```

Reactivar:

```json
{
  "isActive": true
}
```

Al cambiar estado se actualizan:

```text
UpdatedAt
UpdatedBy
```

Se conserva el registro para mantener:

- historial;
- trazabilidad;
- referencias con recetas;
- posibilidad de reactivación.

---

# Catálogo inicial de productos

Cada tenant nuevo recibe:

```text
102 productos
```

Origen:

```text
Data/Tenant/Seed/Data/products.seed.json
```

El archivo es:

```text
EmbeddedResource
```

dentro de SweetSecrets.Infrastructure.

Cada tenant recibe su propia copia.

Los productos pueden modificarse posteriormente sin afectar otras cuentas.

---

# Costo unitario

El costo inicial se calcula:

```text
PurchasePrice / PurchaseQuantity
```

Se conserva con precisión de hasta:

```text
6 decimales
```

Esto evita perder precisión al calcular recetas.

---

# Productos iniciales con precio cero

El catálogo legacy contiene algunos productos con:

```text
PurchasePrice = 0
```

Se conservaron así inicialmente porque forman parte del catálogo original.

Ejemplos identificados:

```text
AZUCAR MASCABADA
CHOCO SEMI AMARGO TURÍN
HARINA DE TRIGO
QUESO PHILADELPHIA
```

Cada tenant podrá actualizar posteriormente sus precios.

---

# Tenant settings

Entidad:

```text
TenantSetting
```

Tabla:

```text
settings
```

Configuración inicial:

```text
MULTIPLIER = 3
```

Proviene del comportamiento original de la aplicación MAUI.

---

# TenantSeedService

Interfaz:

```text
ITenantSeedService
```

Implementación:

```text
TenantSeedService
```

Orden:

```text
Seed units
↓
Seed settings
↓
Seed products
```

Es idempotente mediante verificaciones de existencia.

---

# Recetas

Módulo operacional implementado.

Entidad:

```text
Recipe
```

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

Servicios:

```text
IRecipeQueryService
RecipeQueryService
IRecipeCommandService
RecipeCommandService
```

Todos trabajan mediante:

```text
ITenantDbContextFactory
```

El frontend no selecciona:

```text
TenantId
DatabaseName
ConnectionString
```

## Endpoints

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

## Reglas de cálculo

```text
RecipeItem.TotalCost = RecipeItem.Quantity × RecipeItem.UnitCost
Recipe.TotalCost = SUM(RecipeItem.TotalCost)
Recipe.SuggestedPrice = Recipe.TotalCost × Recipe.Multiplier
```

El costo se conserva con precisión de hasta 6 decimales y el precio sugerido se redondea a 2 decimales con `MidpointRounding.AwayFromZero`.

## Regla actual de unidades

```text
RecipeItem.UnitId = Product.UnitId
```

Todavía no existe una capa formal de conversiones `KG ↔ GR` o `L ↔ ML`; no se realizan conversiones implícitas.

## Ingredientes

Implementado:

```text
agregar ingrediente
editar cantidad
eliminar ingrediente
consultar ingredientes en detalle
```

Cada cambio recalcula:

```text
RecipeItem.TotalCost
Recipe.TotalCost
Recipe.SuggestedPrice
```

## Soft delete

Las recetas no se eliminan físicamente. Se utiliza `IsActive = false` y se permite reactivar con `IsActive = true`.

Una receta inactiva no permite agregar, editar ni eliminar ingredientes.

## Auditoría

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

El usuario se obtiene mediante `ClaimTypes.NameIdentifier`.

---

# RecipeItem

Entidad:

```text
RecipeItem
```

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

Las recetas son relacionales.

No se almacenan como un único JSON.

`RecipeItem.UnitId` representa la unidad utilizada específicamente dentro de la receta y puede ser diferente de:

```text
Product.UnitId
```

siempre que ambas unidades compartan el mismo:

```text
MeasurementType
```

Fórmula de conversión del costo unitario:

```text
RecipeItem.UnitCost
=
Product.UnitCost
× RecipeUnit.ConversionFactor
÷ ProductUnit.ConversionFactor
```

Después:

```text
RecipeItem.TotalCost
=
RecipeItem.Quantity × RecipeItem.UnitCost
```

Ejemplos validados:

```text
Product KG 80 → RecipeItem 250 GR
UnitCost receta = 0.08
TotalCost = 20

Product GR 0.30 → RecipeItem 0.25 KG
UnitCost receta = 300
TotalCost = 75
```

El detalle de receta expone:

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

---

# Historial de precio

Entidad:

```text
ProductPriceHistory
```

Tabla:

```text
product_price_history
```

Campos:

```text
Id
ProductId
PreviousPrice
NewPrice
PreviousUnitCost
NewUnitCost
ChangedBy
ChangedAt
```

La lógica automática ya está implementada dentro de:

```text
ProductCommandService.UpdateAsync
```

Se crea un registro cuando cambia:

```text
PurchasePrice
```

o cuando cambia:

```text
UnitCost
```

aunque el precio total permanezca igual.

Si solo cambia el nombre y precio/costo permanecen iguales, no se genera un registro.

La prueba de TEN-007 confirmó físicamente en PostgreSQL:

```text
PreviousPrice    = 150
NewPrice         = 180
PreviousUnitCost = 0.150000
NewUnitCost      = 0.180000
ChangedBy        = usuario autenticado
```

---

# Historial de costo de recetas

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

Eventos actuales:

```text
RECIPE_ITEM_ADDED
RECIPE_ITEM_UPDATED
RECIPE_ITEM_REMOVED
PRODUCT_UNIT_COST_CHANGED
PRODUCT_UNIT_CHANGED
RECIPE_REACTIVATED_COST_SYNC
```

Solo se crea historial cuando:

```text
PreviousCost != NewCost
```

TEN-010 agregó trazabilidad específica para cambios de unidad base del producto:

```text
PRODUCT_UNIT_CHANGED
```

Ejemplo validado:

```text
120 → 20.10
PRODUCT_UNIT_CHANGED
```

La reactivación con conversiones también conserva:

```text
RECIPE_REACTIVATED_COST_SYNC
```

Ejemplo validado:

```text
20.10 → 25.10
RECIPE_REACTIVATED_COST_SYNC
```

Endpoint:

```text
GET /api/recipes/{id}/cost-history
```

Orden:

```text
CreatedAt DESC
```

---

# Resolución segura de tenant

Implementado.

Interfaz:

```text
ICurrentTenantResolver
```

Implementación:

```text
CurrentTenantResolver
```

---

# CurrentTenantResolver

Obtiene el usuario desde:

```text
HttpContext.User
```

Claim principal:

```text
ClaimTypes.NameIdentifier
```

Después valida:

```text
usuario existe
IsActive = true
IsBlocked = false
TenantId != null
tenant existe
tenant Status = Active
DatabaseName configurado
```

Devuelve:

```text
CurrentTenantInfo
```

con:

```text
TenantId
Code
Name
DatabaseName
```

DatabaseName es información interna.

---

# PLATFORM_ADMIN y resolución tenant

El PLATFORM_ADMIN tiene:

```text
TenantId = null
```

Por lo tanto:

```text
GET /api/tenant/current
```

devuelve:

```text
403 Forbidden
```

Esto ya fue probado.

---

# TenantDbContext dinámico

Interfaz:

```text
ITenantDbContextFactory
```

Implementación:

```text
CurrentTenantDbContextFactory
```

Flujo:

```text
ICurrentTenantResolver
↓
CurrentTenantInfo
↓
validar DatabaseName
↓
construir connection string internamente
↓
TenantDbContext
```

---

# Regla de DatabaseName

Debe cumplir:

```text
sweetsecrets_tenant_XXXXXX
```

con:

```text
XXXXXX = 6 dígitos
```

Si no cumple, se rechaza.

---

# Endpoint de contexto tenant

```text
GET /api/tenant/current
```

Respuesta:

```json
{
  "tenantId": "...",
  "code": "000003",
  "name": "Tenant Prueba 002"
}
```

No expone:

```text
DatabaseName
ConnectionString
credenciales PostgreSQL
```

---

# Endpoint de diagnóstico tenant

Durante desarrollo existe:

```text
GET /api/tenant/summary
```

Servicio:

```text
ICurrentTenantDataService
```

Implementación:

```text
CurrentTenantDataService
```

Consulta físicamente la base del tenant autenticado.

La prueba realizada devolvió:

```text
units    = 5
products = 102
recipes  = 0
```

Esto confirmó que:

```text
CurrentTenantDbContextFactory
```

está conectando a la base correcta.

Este endpoint debe revisarse cuando se complete la API funcional definitiva.

---

# Creación administrativa de TENANT_OWNER

Existe soporte temporal administrativo mediante:

```text
ITenantUserProvisioningService
```

Implementación:

```text
TenantUserProvisioningService
```

Permite crear un:

```text
TENANT_OWNER
```

para un tenant Active.

Valida:

```text
tenant existe
tenant Active
correo no existe
Identity crea usuario
se asigna TENANT_OWNER
```

---

# Endpoint administrativo temporal de owner

Actualmente existe:

```text
POST /api/admin/tenants/owner
```

Solo:

```text
PLATFORM_ADMIN
```

puede utilizarlo.

Este endpoint fue creado para validar TEN-005.

Debe revisarse posteriormente cuando exista administración completa de usuarios tenant.

---

# Endpoint administrativo temporal de provisioning

Actualmente existe:

```text
POST /api/admin/tenants/provision
```

Solo:

```text
PLATFORM_ADMIN
```

puede utilizarlo.

Fue utilizado durante TEN-003/TEN-004 para probar provisioning.

Debe revisarse cuando se complete el flujo administrativo definitivo.

---

# Autoregistro

Implementado.

Caso de uso:

```text
ISelfRegistrationService
```

Implementación:

```text
SelfRegistrationService
```

---

# Contratos internos de autoregistro

```text
SelfRegistrationCommand
SelfRegistrationResult
```

Command:

```text
BusinessName
FullName
Email
Password
```

Result:

```text
UserId
TenantId
TenantCode
BusinessName
Email
```

---

# Endpoint público de autoregistro

```text
POST /api/auth/register
```

Configuración:

```text
AllowAnonymous
```

No requiere usuario autenticado.

---

# Flujo de autoregistro

```text
BusinessName
FullName
Email
Password
        ↓
POST /api/auth/register
        ↓
SelfRegistrationService
        ↓
validar datos
        ↓
validar correo duplicado
        ↓
TenantProvisioningService
        ↓
crear MASTER tenant
        ↓
crear PostgreSQL DB
        ↓
aplicar TEN001
        ↓
seed inicial
        ↓
tenant Active
        ↓
TenantUserProvisioningService
        ↓
crear TENANT_OWNER
        ↓
cuenta lista
```

---

# Validaciones de autoregistro

Actualmente:

```text
BusinessName obligatorio
FullName obligatorio
Email obligatorio
Password obligatorio
BusinessName <= 200
FullName <= 200
correo no registrado
```

Identity aplica adicionalmente las reglas de contraseña.

---

# Manejo de error después del provisioning

Si:

```text
TenantProvisioningService
```

termina correctamente pero falla:

```text
TenantUserProvisioningService
```

el tenant se intenta marcar como:

```text
Failed
```

La base no se elimina automáticamente.

---

# Seguridad de autoregistro

El cliente no envía:

```text
TenantCode
TenantId generado
DatabaseName
ConnectionString
Role
```

El backend genera internamente:

```text
TenantCode
DatabaseName
TenantId
TENANT_OWNER
```

---

# Respuesta pública de registro

Ejemplo:

```json
{
  "userId": "...",
  "tenantId": "...",
  "tenantCode": "000004",
  "businessName": "Repostería Prueba 004",
  "email": "owner000004@sweetsecrets.local"
}
```

No incluye:

```text
Password
PasswordHash
DatabaseName
ConnectionString
credenciales PostgreSQL
```

---

# Validaciones reales realizadas

## Tenant 000001

Estado:

```text
Active
```

Base:

```text
sweetsecrets_tenant_000001
```

Fue el primer provisioning exitoso.

---

## Tenant 000002

Estado:

```text
Failed
```

Motivo registrado:

```text
No se encontró el recurso
SweetSecrets.Infrastructure.Data.Tenant.Seed.Data.products.seed.json
```

La causa real fue que durante la prueba el cambio del recurso embebido todavía no había sido guardado.

Esto permitió comprobar correctamente:

```text
Status = Failed
TENANT_FAILED
```

El tenant se conserva como evidencia de trazabilidad.

No reutilizar:

```text
000002
```

---

## Tenant 000003

Estado:

```text
Active
```

Base:

```text
sweetsecrets_tenant_000003
```

Validación:

```text
units    = 5
products = 102
recipes  = 0
MULTIPLIER = 3
```

Se creó un TENANT_OWNER temporal para validar resolución segura.

La resolución:

```text
GET /api/tenant/current
```

funcionó correctamente.

---

## Tenant 000004

Creado mediante:

```text
POST /api/auth/register
```

Información:

```text
Code = 000004
Name = Repostería Prueba 004
Status = Active
DatabaseName = sweetsecrets_tenant_000004
```

Validación del usuario:

```text
Role = TENANT_OWNER
```

Validación de base:

```text
units    = 5
products = 102
recipes  = 0
```

Esto confirma el flujo completo de autoregistro.

---

# Estado de pruebas de TEN-003

Validado:

- generación de número tenant;
- registro MASTER;
- CREATE DATABASE;
- TEN001;
- Active;
- Failed;
- TENANT_CREATED;
- TENANT_FAILED.

---

# Estado de pruebas de TEN-004

Validado:

```text
5 unidades
102 productos
MULTIPLIER = 3
```

por tenant.

---

# Estado de pruebas de TEN-005

Validado:

```text
PLATFORM_ADMIN sin tenant → 403
TENANT_OWNER con tenant → 200
```

También:

```text
CurrentTenantDbContextFactory
```

consultó físicamente:

```text
sweetsecrets_tenant_000003
```

con resultado:

```text
units = 5
products = 102
recipes = 0
```

---

# Estado de pruebas de TEN-006

Validado:

```text
POST /api/auth/register → 200
```

Se creó automáticamente:

```text
Tenant 000004
TENANT_OWNER
PostgreSQL DB
TEN001
Seed
```

y quedó:

```text
Status = Active
```

---

# Estado de pruebas de TEN-007

Tenant utilizado:

```text
000004
```

Base:

```text
sweetsecrets_tenant_000004
```

Validado:

```text
GET /api/products → 200
GET /api/products/{id} → 200
POST /api/products → 200
PUT /api/products/{id} → 200
PATCH inactive → 204
GET inactive → 200 / IsActive false
PATCH active → 204
GET active → 200 / IsActive true
```

Producto utilizado:

```text
PRODUCTO PRUEBA TEN-007
```

Id:

```text
986bb7f3-c735-4517-b752-b25f1b56e6cc
```

Creación:

```text
PurchaseQuantity = 1000
Unit = GR
PurchasePrice = 150
UnitCost = 0.150000
```

Actualización:

```text
PurchasePrice = 180
UnitCost = 0.180000
```

Historial comprobado directamente en PostgreSQL:

```text
PreviousPrice    = 150
NewPrice         = 180
PreviousUnitCost = 0.150000
NewUnitCost      = 0.180000
ChangedBy        = usuario autenticado
```

Soft delete y reactivación fueron validados correctamente.

El módulo usa `ITenantDbContextFactory` y no recibe `TenantId`, `DatabaseName` ni `ConnectionString` desde el frontend.

---

# Estado de pruebas de TEN-008

Tenant utilizado:

```text
000004
```

Base:

```text
sweetsecrets_tenant_000004
```

Receta de prueba:

```text
PASTEL CHOCOLATE PRUEBA TEN-008
```

Id:

```text
46683e43-3c55-4d67-8ebd-9c9731d707f1
```

Validado:

```text
GET /api/recipes → 200 / []
GET /api/recipes/{id inexistente} → 404
POST /api/recipes → 200
GET /api/recipes/{id} → 200
PUT /api/recipes/{id} → 200
POST /api/recipes/{id}/items → 200
PUT /api/recipes/{recipeId}/items/{itemId} → 200
DELETE /api/recipes/{recipeId}/items/{itemId} → 200
GET /api/recipes/{id}/cost-history → 200
PATCH inactive → 204
GET inactive → 200 / IsActive false
GET active después de reactivación → 200 / IsActive true
```

Cálculos validados:

```text
250 × 0.18 = 45
45 × 3 = 135
45 × 4 = 180
300 × 0.18 = 54
54 × 4 = 216
350 × 0.18 = 63
63 × 4 = 252
```

Historial validado directamente en PostgreSQL:

```text
0  → 54  RECIPE_ITEM_ADDED
54 → 63  RECIPE_ITEM_UPDATED
63 → 0   RECIPE_ITEM_REMOVED
```

Soft delete y reactivación fueron validados correctamente.

El módulo usa `ITenantDbContextFactory` sin selección de `TenantId`, `DatabaseName` ni `ConnectionString` desde el cliente.

---

# Estado de pruebas de TEN-009

Tenant utilizado:

```text
000004
```

Base:

```text
sweetsecrets_tenant_000004
```

Producto utilizado:

```text
PRODUCTO PRUEBA TEN-007
```

ProductId:

```text
986bb7f3-c735-4517-b752-b25f1b56e6cc
```

Receta utilizada:

```text
PASTEL CHOCOLATE PRUEBA TEN-008
```

RecipeId:

```text
46683e43-3c55-4d67-8ebd-9c9731d707f1
```

Ingrediente:

```text
Quantity = 300
Unit = GR
```

## Cambio de precio

Estado inicial:

```text
PurchaseQuantity = 1000
PurchasePrice = 180
UnitCost = 0.18
Recipe.TotalCost = 54
Recipe.SuggestedPrice = 216
```

Cambio:

```text
PurchasePrice = 200
UnitCost = 0.20
```

Resultado:

```text
RecipeItem.UnitCost = 0.20
RecipeItem.TotalCost = 60
Recipe.TotalCost = 60
Recipe.SuggestedPrice = 240
```

Historial:

```text
54 → 60
PRODUCT_UNIT_COST_CHANGED
```

## Cambio de cantidad de compra

Cambio:

```text
PurchasePrice = 200
PurchaseQuantity = 800
UnitCost = 0.25
```

Resultado:

```text
RecipeItem.UnitCost = 0.25
RecipeItem.TotalCost = 75
Recipe.TotalCost = 75
Recipe.SuggestedPrice = 300
```

Historial:

```text
60 → 75
PRODUCT_UNIT_COST_CHANGED
```

Esto confirmó que el recálculo depende del cambio real de:

```text
Product.UnitCost
```

y no únicamente de `PurchasePrice`.

## Protección de cambio de unidad

Se intentó cambiar el producto usado por la receta de:

```text
GR → KG
```

Resultado:

```text
409 Conflict
```

Mensaje:

```text
No se puede cambiar la unidad de un producto que ya está siendo utilizado en recetas.
```

Después del rechazo se validó que el producto conservó:

```text
Unit = GR
PurchaseQuantity = 800
PurchasePrice = 200
UnitCost = 0.25
UpdatedAt sin cambio
```

## Receta inactiva

La receta fue desactivada:

```text
IsActive = false
```

Después se actualizó el producto a:

```text
PurchaseQuantity = 800
PurchasePrice = 240
UnitCost = 0.30
```

La receta inactiva conservó:

```text
RecipeItem.UnitCost = 0.25
RecipeItem.TotalCost = 75
Recipe.TotalCost = 75
Recipe.SuggestedPrice = 300
```

Esto confirmó que recetas inactivas no se recalculan automáticamente.

## Reactivación

Al reactivar:

```text
IsActive = true
```

se sincronizó con el costo actual:

```text
Product.UnitCost = 0.30
RecipeItem.UnitCost = 0.30
RecipeItem.TotalCost = 90
Recipe.TotalCost = 90
Recipe.SuggestedPrice = 360
```

Historial:

```text
75 → 90
RECIPE_REACTIVATED_COST_SYNC
```

## Build

Todos los bloques incrementales de TEN-009 finalizaron con:

```text
Build succeeded
```

---

# Estado de pruebas de TEN-010

Rama:

```text
feature/TEN-010-unit-conversions
```

Tenant funcional principal:

```text
000004
sweetsecrets_tenant_000004
```

## Migración TEN002

Migración:

```text
20260827194205_TEN002_AddUnitConversions
```

Agrega:

```text
MeasurementType integer NOT NULL
ConversionFactor numeric(18,6) NOT NULL
```

La migración fue corregida manualmente para evitar defaults inválidos `0` en datos existentes.

Proceso:

```text
crear columnas nullable
↓
backfill por Code
↓
SET NOT NULL
```

Backfill:

```text
GR  → Mass   → 1
KG  → Mass   → 1000
ML  → Volume → 1
L   → Volume → 1000
PZA → Count  → 1
```

## Tenant template

TEN002 aplicada correctamente a:

```text
sweetsecrets_tenant_template
```

Estructura validada:

```text
ConversionFactor numeric(18,6) NOT NULL
MeasurementType integer NOT NULL
```

El template tenía `0` unidades durante la prueba.

## Tenants activos migrados

MASTER reportó:

```text
000001 Active
000003 Active
000004 Active
000002 Failed
```

TEN002 fue aplicada a:

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

El tenant `000002` no fue modificado porque permanece `Failed` como evidencia histórica.

## Tenant 000001 histórico

Se encontró:

```text
Units = 0
Products = 0
Settings = 0
Recipes = 0
```

Por eso TEN002 reportó:

```text
UPDATE 0
```

La migración terminó correctamente y no se agregó seed artificial para conservar el estado histórico de la prueba.

## Tenant 000003

TEN002 realizó:

```text
UPDATE 5
```

## Tenant 000004

TEN002 realizó:

```text
UPDATE 5
```

Valores comprobados:

```text
GR  | 1 | 1.000000
KG  | 1 | 1000.000000
ML  | 2 | 1.000000
L   | 2 | 1000.000000
PZA | 3 | 1.000000
```

## Conversión GR → KG

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

Resultado:

```text
RecipeItem.UnitCost = 300
RecipeItem.TotalCost = 75
Recipe.TotalCost = 75
Recipe.SuggestedPrice = 300
```

## Recálculo con unidad convertida

Cambio del producto:

```text
UnitCost 0.30 → 0.40 por GR
```

La receta conservó el ingrediente en KG:

```text
RecipeItem.UnitCost = 400
RecipeItem.TotalCost = 100
Recipe.TotalCost = 100
Recipe.SuggestedPrice = 400
```

Historial:

```text
75 → 100
PRODUCT_UNIT_COST_CHANGED
```

## Incompatibilidad

Se intentó utilizar:

```text
Product = GR / Mass
RecipeItem = ML / Volume
```

Resultado:

```text
409 Conflict
```

Mensaje:

```text
La unidad del ingrediente no es compatible con la unidad del producto.
```

## Conversión KG → GR

Producto creado:

```text
PRODUCTO KG PRUEBA TEN-010
Unit = KG
PurchaseQuantity = 1
PurchasePrice = 80
UnitCost = 80
```

Ingrediente:

```text
Quantity = 250
Unit = GR
```

Resultado:

```text
RecipeItem.UnitCost = 0.08
RecipeItem.TotalCost = 20
```

## Receta combinada

Se validaron simultáneamente:

```text
0.25 KG → TotalCost 100
250 GR  → TotalCost 20
```

Resultado:

```text
Recipe.TotalCost = 120
Recipe.SuggestedPrice = 480
```

## Cambio compatible Product.Unit

Se modificó un producto ya utilizado:

```text
GR → KG
```

Fue permitido porque:

```text
Mass = Mass
```

Aunque el valor numérico de `UnitCost` permaneció `0.40`, el significado cambió a costo por KG.

El RecipeItem en KG se recalculó:

```text
Quantity = 0.25 KG
UnitCost = 0.40
TotalCost = 0.10
```

La receta completa quedó:

```text
TotalCost = 20.10
SuggestedPrice = 80.40
```

Historial:

```text
120 → 20.10
PRODUCT_UNIT_CHANGED
```

## Cambio incompatible Product.Unit

Se intentó:

```text
KG / Mass → ML / Volume
```

Resultado:

```text
409 Conflict
```

Mensaje:

```text
No se puede cambiar la unidad del producto porque no es compatible con las unidades utilizadas en sus recetas.
```

Después del rechazo el producto conservó:

```text
Unit = KG
PurchaseQuantity = 800
PurchasePrice = 320
UnitCost = 0.40
UpdatedAt sin cambio
```

## Reactivación con conversión

Receta desactivada:

```text
IsActive = false
TotalCost = 20.10
```

Mientras estaba inactiva se modificó un producto:

```text
Product.Unit = KG
UnitCost 80 → 100
```

El RecipeItem en GR conservó mientras estuvo inactivo:

```text
UnitCost = 0.08
TotalCost = 20
```

Al reactivar:

```text
100 por KG
→ 0.10 por GR
→ 250 GR = 25
```

Resultado final:

```text
Recipe.TotalCost = 25.10
Recipe.SuggestedPrice = 100.40
```

Historial:

```text
20.10 → 25.10
RECIPE_REACTIVATED_COST_SYNC
```

## Build

Todos los bloques incrementales finalizaron con:

```text
Build succeeded
```

---


# Swagger

Swagger está habilitado únicamente como herramienta de desarrollo.

URL:

```text
https://localhost:7010/swagger
```

No se considera una interfaz de usuario final.

---

# Git

Repositorio:

```text
https://github.com/aleexdiiasz/SweetSecrets.git
```

Ramas base:

```text
main
develop
```

Flujo:

```text
develop
↓
feature/*
↓
Pull Request
↓
develop
```

---

# TEN-003 / TEN-004 Git

Rama:

```text
feature/TEN-003-tenant-provisioning
```

Commit:

```text
e2ca0c4 feat: add tenant provisioning and initial seed
```

Pull Request:

```text
#1
TEN-003/TEN-004 - Tenant provisioning and initial seed
```

Estado:

```text
Merged → develop
```

---

# TEN-005 Git

Rama:

```text
feature/TEN-005-tenant-resolution
```

Commit:

```text
b0dd68f feat: add secure tenant resolution
```

Pull Request:

```text
#2
TEN-005 - Secure tenant resolution
```

Estado:

```text
Merged → develop
```

---

# TEN-006 Git

Rama:

```text
feature/TEN-006-self-registration
```

Commit:

```text
e54eed4 feat: add tenant self registration
```

Pull Request:

```text
#3
TEN-006 - Tenant self registration
```

Estado:

```text
Merged → develop
```

El `develop` local fue sincronizado después del merge.

---

# TEN-007 Git

Rama:

```text
feature/TEN-007-products
```

Pull Request:

```text
#4
TEN-007 - Tenant products module
```

Estado:

```text
Merged → develop
```

Después del merge:

```text
develop sincronizado
working tree clean
```

TEN-007 está cerrado.

---

# TEN-008 Git

Rama:

```text
feature/TEN-008-recipes
```

Commit:

```text
5e44fed feat: add tenant recipes module
```

Pull Request:

```text
#5
TEN-008 - Tenant recipes module
```

Estado:

```text
Merged → develop
```

Después del merge:

```text
develop sincronizado
working tree clean
```

TEN-008 está cerrado.

---

# TEN-009 Git

Rama:

```text
feature/TEN-009-recipe-recalculation
```

Commit:

```text
d2a5ebe feat: add automatic recipe recalculation
```

Pull Request:

```text
#6
TEN-009 - Automatic recipe recalculation
```

Estado:

```text
Merged → develop
```

Después del merge:

```text
develop sincronizado
working tree clean
```

TEN-009 está cerrado.

---

# TEN-010 Git

Rama actual:

```text
feature/TEN-010-unit-conversions
```

Estado funcional:

```text
Implementado
Build OK
Pruebas funcionales OK
TEN002 aplicada y validada
UNIT_CONVERSIONS.md creado
CURRENT_STATE.md actualizado
```

Cambios principales:

```text
Unit.cs
MeasurementType.cs
TenantDbContext.cs
TenantSeedService.cs
TEN002_AddUnitConversions
ProductCommandService.cs
RecipeCommandService.cs
```

Pendiente:

```text
git add .
git status
commit
push
Pull Request → develop
```

No asumir que TEN-010 está en `develop` hasta completar el Pull Request.

---


# Documentación técnica existente

Actualmente existen, entre otros:

```text
docs/architecture/ARCHITECTURE.md
docs/database/MASTER_DATABASE.md
docs/database/TENANT_DATABASE.md
docs/decisions/ADR-001-MULTITENANCY.md
docs/functional/PROJECT_SCOPE.md
docs/security/AUTHENTICATION.md
docs/technical/TENANT_PROVISIONING.md
docs/technical/TENANT_RESOLUTION.md
docs/technical/TENANT_SELF_REGISTRATION.md
docs/technical/PRODUCTS.md
docs/technical/RECIPES.md
docs/technical/RECIPE_RECALCULATION.md
docs/technical/UNIT_CONVERSIONS.md
docs/ai/CURRENT_STATE.md
```

---

# README y AGENTS

Raíz:

```text
README.md
AGENTS.md
```

`AGENTS.md` contiene reglas para agentes e IA.

Entre las reglas importantes:

- respetar arquitectura;
- database-per-tenant;
- nunca mezclar datos tenant;
- no permitir selección arbitraria de TenantId/DatabaseName;
- no almacenar secretos;
- mantener documentación sincronizada.

---

# Seguridad

Reglas actuales:

1. No guardar passwords en código.
2. No guardar connection strings sensibles en Git.
3. Usar ASP.NET Core Identity.
4. Usar cookies HttpOnly.
5. PLATFORM_ADMIN tiene TenantId null.
6. Usuario tenant obtiene contexto desde MASTER.
7. DatabaseName no se expone al cliente.
8. ConnectionString no se expone al cliente.
9. Backend valida tenant Active.
10. Backend valida nombre de base.
11. Cada tenant utiliza base PostgreSQL independiente.
12. Un usuario tenant no puede elegir otra base mediante parámetros HTTP.

---

# Arquitectura objetivo de acceso operacional

Toda operación futura de productos, recetas y configuración debe seguir:

```text
HTTP request
↓
usuario autenticado
↓
CurrentTenantResolver
↓
MASTER
↓
Tenant Active
↓
ITenantDbContextFactory
↓
TenantDbContext
↓
base PostgreSQL del usuario
↓
operación
```

No utilizar:

```text
request.TenantId
query.TenantId
body.DatabaseName
header.DatabaseName
```

para seleccionar base tenant.

---

# Módulos operativos todavía no implementados

## Configuración

Pendiente CRUD de:

```text
settings
```

La tabla y el seed inicial ya existen.

Configuración inicial conocida:

```text
MULTIPLIER = 3
```

## Usuarios tenant

Pendiente:

- crear TENANT_USER;
- editar;
- bloquear;
- permisos;
- administración por TENANT_OWNER.

## Frontend operacional

Pendiente construir la experiencia final en:

```text
Blazor WebAssembly PWA
```

consumiendo únicamente la API.

---

# Funcionalidades todavía no implementadas

- confirmación de correo;
- recuperación de contraseña;
- cambio de contraseña desde UI;
- CRUD configuración;
- administración completa de usuarios tenant;
- notificaciones;
- SignalR;
- Web Push;
- UI Blazor final;
- login UI final;
- registro UI final;
- dashboard PLATFORM_ADMIN;
- dashboard TENANT_OWNER;
- dashboard TENANT_USER;
- Docker de producción;
- backups;
- restore;
- monitoreo;
- health checks definitivos;
- despliegue productivo.

---

# Notificaciones

Objetivo futuro:

PLATFORM_ADMIN podrá enviar notificaciones como:

```text
Nueva actualización
Mantenimiento
Aviso importante
Información de plataforma
```

Canales previstos:

```text
In-App
Web Push
```

Tecnologías previstas:

```text
SignalR
Web Push
```

Todavía no implementado.

---

# Administración PLATFORM_ADMIN prevista

El administrador global deberá poder:

- ver usuarios;
- ver tenants;
- ver usuarios online;
- bloquear/desbloquear;
- revisar movimientos;
- consultar auditoría;
- enviar notificaciones;
- revisar estado de tenants;
- administrar plataforma.

Actualmente ya existe:

- consulta de usuarios;
- estado online;
- bloqueo;
- desbloqueo;
- auditoría base.

---

# PWA

Frontend objetivo:

```text
Blazor WebAssembly PWA
```

Roles previstos:

```text
TENANT_OWNER
TENANT_USER
```

La PWA deberá consumir únicamente la API.

No deberá conectarse directamente a PostgreSQL.

No deberá manejar connection strings tenant.

---

# Offline

Decisión V1:

```text
requiere conexión a Internet
```

No se implementará sincronización offline de datos operativos en la primera versión.

El soporte PWA no implica que la información tenant se almacene offline.

---

# Base de datos y crecimiento

La arquitectura está pensada inicialmente para aproximadamente:

```text
25–30 reposteras
```

pero puede escalar posteriormente.

Cada nueva cuenta obtiene:

```text
1 registro MASTER
1 base PostgreSQL independiente
1 TENANT_OWNER
1 catálogo inicial propio
```

---

# Próximo objetivo

Objetivo inmediato:

```text
Cerrar Git de TEN-010
```

Secuencia:

```text
git add
↓
commit
↓
push
↓
Pull Request
↓
merge → develop
↓
sincronizar develop local
```

Después del merge de TEN-010 debe definirse formalmente el siguiente issue.

Pendientes prioritarios actuales:

```text
CRUD de configuración tenant
administración de TENANT_USER
frontend Blazor WebAssembly PWA
```

No se debe asignar un número al siguiente bloque hasta definir su alcance.

---

# TEN-010 implementado

TEN-007 Products está cerrado.

TEN-008 Recipes está cerrado mediante PR #5.

TEN-009 Automatic Recipe Recalculation está cerrado mediante PR #6.

TEN-010 Unit Conversions quedó implementado funcionalmente.

Componentes completados:

```text
MeasurementType
ConversionFactor
TEN002_AddUnitConversions
backfill seguro de unidades existentes
seed actualizado
GR ↔ KG
ML ↔ L
validación por MeasurementType
costo convertido al agregar RecipeItem
recálculo automático con unidades convertidas
cambio compatible de Product.UnitId
bloqueo de cambios incompatibles
RecipeCostHistory PRODUCT_UNIT_CHANGED
reactivación con conversiones
migración de tenants activos existentes
```

Estado pendiente de TEN-010:

```text
Git commit
push
Pull Request → develop
```

No iniciar otro módulo antes de cerrar TEN-010 en Git.

---

# Reglas críticas para agentes / IA

## Multi-tenancy

Nunca permitir que un usuario tenant seleccione directamente:

```text
TenantId
DatabaseName
ConnectionString
```

para elegir su contexto operacional.

Siempre utilizar:

```text
usuario autenticado
↓
MASTER
↓
tenant Active
↓
ITenantDbContextFactory
```

---

## Seguridad

Nunca:

- guardar passwords;
- mostrar secretos;
- subir Docker secrets;
- subir User Secrets;
- retornar connection strings;
- retornar PasswordHash;
- confiar en TenantId enviado por frontend para seleccionar DB.

---

## Datos tenant

Nunca consultar operaciones tenant desde MASTER.

Datos como:

```text
products
recipes
recipe_items
settings
```

pertenecen exclusivamente a la base tenant.

---

## Desarrollo incremental

Cada bloque debe seguir:

```text
Código
↓
dotnet build
↓
Prueba
↓
Documentación
↓
git status
↓
git add
↓
commit
↓
push
↓
PR
```

No avanzar varios módulos sin cerrar el anterior.

---

## Documentación

Mantener sincronizados:

```text
docs/technical/
docs/ai/CURRENT_STATE.md
```

cuando se cierre un bloque funcional importante.

No borrar información histórica relevante al actualizar CURRENT_STATE.

Actualizar únicamente el estado real.

---

# Regla crítica de continuidad

Antes de comenzar un nuevo módulo, verificar:

```powershell
git status
```

Debe estar:

```text
nothing to commit, working tree clean
```

y la rama base debe estar actualizada con:

```text
origin/develop
```

---

# Estado resumido

Actualmente SweetSecrets ya tiene:

```text
✅ Arquitectura .NET 10
✅ PostgreSQL Docker
✅ MASTER DB
✅ Tenant DB
✅ Database-per-tenant
✅ Identity
✅ Roles
✅ PLATFORM_ADMIN
✅ Login
✅ Logout
✅ Cookies
✅ Sesiones
✅ Usuario online
✅ Auditoría
✅ Bloqueo/desbloqueo
✅ Tenant sequence
✅ Tenant provisioning
✅ CREATE DATABASE automático
✅ Migraciones tenant automáticas
✅ Seed unidades
✅ Seed configuración
✅ Seed 102 productos
✅ Manejo Failed
✅ Resolución segura tenant
✅ TenantDbContext dinámico
✅ TENANT_OWNER
✅ Autoregistro público
✅ Validación real de aislamiento
✅ CRUD operacional de productos
✅ UnitCost de productos
✅ Historial de precio de productos
✅ Soft delete de productos
✅ Reactivación de productos
✅ CRUD operacional de recetas
✅ RecipeItems
✅ Detalle de receta con ingredientes
✅ Cálculo de costo de receta
✅ Multiplicador
✅ Precio sugerido
✅ Historial automático de costos de receta
✅ Consulta de historial por API
✅ Soft delete de recetas
✅ Reactivación de recetas
✅ Propagación Product.UnitCost → RecipeItem
✅ Recálculo automático Recipe.TotalCost
✅ Recálculo automático SuggestedPrice
✅ Historial PRODUCT_UNIT_COST_CHANGED
✅ Recetas inactivas preservadas ante cambios de producto
✅ Sincronización de costos al reactivar
✅ MeasurementType
✅ ConversionFactor
✅ TEN002_AddUnitConversions
✅ GR ↔ KG
✅ ML ↔ L
✅ RecipeItem con unidad distinta al Product
✅ Recálculo automático respetando conversiones
✅ Cambio compatible de Product.UnitId
✅ Historial PRODUCT_UNIT_CHANGED
✅ Bloqueo de cambios de unidad incompatibles
✅ Reactivación respetando conversiones
✅ TEN002 aplicada a tenants activos existentes
```

Pendiente principal:

```text
⏳ Cerrar Git de TEN-010
⏳ Configuration CRUD
⏳ Administración TENANT_USER
⏳ Notifications
⏳ Blazor UI
⏳ Production deployment
```

---

# Punto exacto para continuar

Rama:

```text
feature/TEN-010-unit-conversions
```

TEN-010 ya:

```text
compila
funciona
fue probado
TEN002 fue creada
TEN002 fue aplicada al template
TEN002 fue aplicada a tenants activos existentes
UNIT_CONVERSIONS.md fue creado
CURRENT_STATE.md fue actualizado
```

Cambios principales:

```text
src/SweetSecrets.Domain/Entities/Tenant/Unit.cs
src/SweetSecrets.Domain/Enums/MeasurementType.cs
src/SweetSecrets.Infrastructure/Data/Tenant/TenantDbContext.cs
src/SweetSecrets.Infrastructure/Data/Tenant/Seed/TenantSeedService.cs
src/SweetSecrets.Infrastructure/Data/Tenant/Migrations/20260827194205_TEN002_AddUnitConversions.cs
src/SweetSecrets.Infrastructure/Data/Tenant/Migrations/20260827194205_TEN002_AddUnitConversions.Designer.cs
src/SweetSecrets.Infrastructure/Data/Tenant/Migrations/TenantDbContextModelSnapshot.cs
src/SweetSecrets.Infrastructure/Services/Products/ProductCommandService.cs
src/SweetSecrets.Infrastructure/Services/Recipes/RecipeCommandService.cs
docs/technical/UNIT_CONVERSIONS.md
docs/ai/CURRENT_STATE.md
```

Se debe terminar:

```text
git add .
git status
commit
push
PR a develop
```

Después del merge:

```text
git checkout develop
git pull origin develop
git status
```

No volver a implementar:

```text
provisioning
seed base
resolver tenant
autoregistro
CRUD productos
CRUD recetas
RecipeItems
historial de costos
recálculo automático por Product.UnitCost
soft delete / reactivación
conversiones GR ↔ KG
conversiones ML ↔ L
cambio compatible de Product.UnitId
```

Esos bloques ya existen y fueron validados.

Después de cerrar TEN-010 se debe definir formalmente el siguiente issue antes de crear una nueva rama.

---

# TEN-011 - Tenant Settings

## Estado

Implementaci�n funcional completada y probada en la rama:

feature/TEN-011-tenant-settings

## Implementado

- ISettingQueryService y SettingQueryService.
- ISettingCommandService y SettingCommandService.
- Contratos p�blicos de Settings.
- SettingsController.
- Registro de servicios en DI.
- GET /api/settings.
- GET /api/settings/{key}.
- PUT /api/settings/{key}.
- Resoluci�n exclusiva mediante ITenantDbContextFactory.
- Normalizaci�n de Key con Trim y ToUpperInvariant.
- Actualizaci�n de UpdatedAt en UTC.
- PUT �nicamente actualiza configuraciones existentes.
- PUT no crea claves arbitrarias.

## Autorizaci�n

- TENANT_OWNER puede consultar y modificar settings.
- TENANT_USER puede consultar settings.
- TENANT_USER no puede modificar settings.
- PLATFORM_ADMIN no utiliza el contexto operacional tenant normal.

La prueba funcional directa de TENANT_USER queda pendiente hasta implementar el flujo de administraci�n de usuarios tenant.

## MULTIPLIER

El seed existente mantiene:

MULTIPLIER = 3

La validaci�n exige un decimal mayor que cero.

Se utiliza CultureInfo.InvariantCulture con NumberStyles.AllowDecimalPoint.

Validado:

- 4.5 -> v�lido.
- 4,5 -> 400 Bad Request.
- 0 -> 400 Bad Request.

Durante las pruebas se detect� que NumberStyles.Number interpretaba 4,5 como 45. El problema fue corregido antes del cierre de TEN-011.

## Aislamiento tenant

Se comprob� f�sicamente:

- sweetsecrets_tenant_000003 mantuvo MULTIPLIER = 3.
- sweetsecrets_tenant_000004 cambi� temporalmente a MULTIPLIER = 4.5.
- La modificaci�n de tenant 000004 no afect� tenant 000003.
- Al finalizar las pruebas, tenant 000004 fue restaurado a MULTIPLIER = 3.

Esto confirma aislamiento database-per-tenant para settings.

## Recipe.Multiplier

TEN-011 no modifica la l�gica de Recipes.

Actualmente settings.MULTIPLIER y Recipe.Multiplier son datos diferenciados.
Cada receta contin�a recibiendo y almacenando su propio Recipe.Multiplier.

Usar settings.MULTIPLIER como valor predeterminado para nuevas recetas requerir� una decisi�n funcional independiente.

## Migraciones

TEN-011 no requiere una nueva migraci�n.
La tabla settings ya existe desde TEN001_InitialTenant.

## Pruebas funcionales

- GET /api/settings -> 200.
- GET /api/settings/multiplier -> 200.
- PUT MULTIPLIER = 4.5 -> 200.
- PUT MULTIPLIER = 0 -> 400.
- Error de validaci�n no altera el valor anterior.
- GET NO_EXISTE -> 404.
- PUT NO_EXISTE -> 404.
- NO_EXISTE no fue creada.
- 4,5 -> 400 despu�s de corregir validaci�n decimal.
- 4.5 -> 200.
- aislamiento 000003 / 000004 confirmado.
- valor de prueba restaurado a 3.

## Documentaci�n

Se cre� docs/technical/SETTINGS.md.

## Git anterior

TEN-010 - Unit conversions fue integrado mediante PR #7 a develop.

## Punto de continuidad

TEN-011 est� implementado, probado y documentado.
Pendiente: build final, staging, commit, push y Pull Request a develop.
No iniciar TEN-012 antes de cerrar TEN-011.

---

# TEN-011 - Tenant Settings - Cierre Git

TEN-011 fue integrado correctamente a develop.

Rama: feature/TEN-011-tenant-settings

Commit: 4b5b1ea feat: add tenant settings module

Pull Request: #8 - TEN-011 - Tenant Settings

Estado: Merged -> develop

Después del merge: develop sincronizado y working tree clean.

TEN-011 está cerrado.

---

# TEN-012 - Tenant User Management

TEN-012 fue evaluado como siguiente bloque para permitir que TENANT_OWNER administre usuarios TENANT_USER.

Estado: PAUSADO.

Decision funcional actual:

La primera version funcional continuara unicamente con TENANT_OWNER.

No se implementara por ahora:

- creacion de TENANT_USER
- edicion de TENANT_USER
- bloqueo de TENANT_USER
- administracion de permisos tenant
- UI de administracion de usuarios tenant

El rol TENANT_USER puede permanecer definido en la plataforma, pero no forma parte del flujo funcional actual.

No reanudar TEN-012 hasta que exista una decision funcional explicita.

---

# TEN-013 - Blazor PWA Foundation

Rama actual:

feature/TEN-013-blazor-pwa-foundation

Objetivo:

Construir la base funcional de SweetSecrets.Web como Blazor WebAssembly PWA y conectarla de forma segura con SweetSecrets.Api.

La primera experiencia funcional esta enfocada exclusivamente en TENANT_OWNER.

## Puertos de desarrollo

API:
- https://localhost:7010
- http://localhost:5183

Web:
- https://localhost:7011
- http://localhost:5078

Se corrigio el conflicto anterior donde API y Web utilizaban el mismo puerto HTTPS.

## Comunicacion Web -> API

La API permite el origen de desarrollo:

https://localhost:7011

La politica CORS permite headers, methods y credentials.

No se utiliza AllowAnyOrigin.

SweetSecrets.Web utiliza HttpClient contra:

https://localhost:7010/

Se implemento CookieCredentialsHandler utilizando BrowserRequestCredentials.Include.

Esto permite utilizar de forma segura la cookie HttpOnly SweetSecrets.Auth.

## Authentication State

Se implemento ApiAuthenticationStateProvider.

Consulta:

GET /api/auth/me

El estado de autenticacion del frontend incluye:

- NameIdentifier
- Email
- tenant_id
- session_id
- roles

## AuthApiClient

Se implemento AuthApiClient para:

- POST /api/auth/login
- POST /api/auth/logout
- actualizar AuthenticationStateProvider
- manejar errores basicos de autenticacion

## Login

Se creo:

Pages/Login.razor

Ruta:

/login

Incluye:

- correo electronico
- password
- Recordarme
- estado de envio
- mensajes de error

Tambien se creo AuthLayout para las pantallas de autenticacion.

## Proteccion de rutas

App.razor utiliza AuthorizeRouteView.

Se creo:

Components/Auth/RedirectToLogin.razor

Un usuario sin sesion que intenta acceder a una ruta protegida es enviado automaticamente a /login.

## TENANT_OWNER

La ruta / se protegio temporalmente con:

[Authorize(Roles = TENANT_OWNER)]

Se valido acceso correcto con el TENANT_OWNER del tenant 000004.

## Logout

Se valido:

TENANT_OWNER autenticado
-> POST /api/auth/logout
-> sesion finalizada
-> navegacion a /login

Despues del logout, intentar entrar nuevamente a / redirige a /login.

## Pruebas funcionales

Validado:

- Web HTTPS inicia en puerto 7011
- API HTTPS inicia en puerto 7010
- /login carga correctamente
- login TENANT_OWNER exitoso
- cookie HttpOnly funciona
- GET /api/auth/me resuelve la sesion
- rol TENANT_OWNER disponible en Blazor
- ruta / permite acceso autenticado
- logout funciona
- ruta / sin sesion redirige a /login

## Build

Resultado final:

Build succeeded

## Estado actual

TEN-013 tiene validado el nucleo de autenticacion del frontend.

Completado:

- conexion Web -> API
- CORS con credentials
- CookieCredentialsHandler
- AuthenticationStateProvider
- login
- autorizacion TENANT_OWNER
- logout
- redireccion a login
- build
- prueba funcional

Todavia no se considera terminada la UI operacional.

No implementar TENANT_USER durante esta fase.

---

# TEN-014 - Tenant Owner Application Shell

Rama actual:

feature/TEN-014-tenant-owner-shell

## Objetivo

Reemplazar la interfaz del template de Blazor por el shell operacional inicial de SweetSecrets para TENANT_OWNER.

## Implementado

- MainLayout operacional
- sidebar
- header
- logout global
- navegacion principal
- dashboard inicial
- responsive
- rutas placeholder protegidas

## Navegacion

Rutas disponibles:

- /
- /productos
- /recetas
- /configuracion

Opciones visibles:

- Inicio
- Productos
- Recetas
- Configuracion

La opcion activa cambia visualmente.

## Dashboard TENANT_OWNER

Home contiene tarjetas informativas para:

- Productos
- Recetas
- Configuracion

Todavia no consumen endpoints operacionales.

## Logout

Cerrar sesion fue movido desde Home hacia MainLayout.

Se valido:

TENANT_OWNER
-> Cerrar sesion
-> POST /api/auth/logout
-> /login

## Responsive

Validado aproximadamente a 390 px:

- sidebar pasa arriba
- contenido queda debajo
- dashboard pasa a una columna
- logout permanece visible y funcional

## Paginas placeholder

Se crearon:

- Pages/Products.razor
- Pages/Recipes.razor
- Pages/Settings.razor

Las tres rutas estan protegidas para TENANT_OWNER.

## Build y pruebas

Resultado:

Build succeeded

Validado manualmente:

- navegacion completa
- estado activo del menu
- dashboard
- logout
- responsive

## Documentacion

Se creo:

docs/technical/FRONTEND_SHELL.md

## Limites de TEN-014

No implementa:

- CRUD real de productos
- CRUD real de recetas
- configuracion funcional
- TENANT_USER

TENANT_USER permanece pausado.

## Estado actual

TEN-014 implementado, compilado, probado y documentado.

Pendiente:

- git status
- git diff --check
- git add
- commit
- push
- Pull Request a develop

---

# TEN-015 - Tenant Products UI

Rama actual:

feature/TEN-015-products-ui

## Implementado

- GET /api/units
- UnitListItem
- IUnitQueryService
- UnitQueryService
- UnitListItemResponse
- UnitsController
- UnitsApiClient
- ProductsApiClient
- listado real de productos
- busqueda por nombre
- alta de producto
- edicion de producto
- activar/desactivar
- badges de estado
- cultura es-MX
- responsive

## Validaciones

GET /api/units -> 200

Unidades:

- GR
- KG
- L
- ML
- PZA

Producto creado:

PRODUCTO PRUEBA TEN-015
Cantidad = 1000
Unidad = GR
Precio = 125
UnitCost = 0.125000

Producto editado:

PRODUCTO PRUEBA TEN-015 EDITADO
Cantidad = 500
Unidad = GR
Precio = 150
UnitCost = 0.300000

Activacion validada:

Activo -> Inactivo -> Activo

## Seguridad

La UI no envia TenantId, DatabaseName ni ConnectionString.

El contexto tenant sigue resolviendose mediante usuario autenticado -> MASTER -> tenant Active -> ITenantDbContextFactory.

## Responsive

Validado aproximadamente a 390 px.

## Documentacion

Se creo:

docs/technical/PRODUCTS_UI.md

## Estado

TEN-015 implementado, compilado, probado y documentado.

Pendiente:

- git status
- git diff --check
- git add
- commit
- push
- Pull Request a develop

TENANT_USER permanece pausado.

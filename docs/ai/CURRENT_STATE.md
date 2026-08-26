# Estado actual del proyecto

## Fecha de estado

2026-08-26

## Proyecto

SweetSecrets

## Fase actual

Infraestructura multi-tenant, autenticación, provisioning automático, resolución segura de tenant y autoregistro funcional.

El núcleo backend necesario para comenzar los módulos operativos de cada tenant ya está disponible.

Actualmente se está trabajando en:

```text
TEN-006 - Self Registration
```

La funcionalidad ya fue implementada y validada funcionalmente.

Pendiente inmediato:

```text
Documentación final
Git checkpoint
Pull Request a develop
```

Después de cerrar TEN-006, el siguiente módulo previsto es:

```text
TEN-007 - Products
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

Migración:

```text
TEN001_InitialTenant
```

---

# Tenant template

Base de desarrollo:

```text
sweetsecrets_tenant_template
```

Se utiliza para probar migraciones tenant.

No pertenece a un usuario real.

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

Seed inicial:

```text
GR  - Gramo
KG  - Kilogramo
ML  - Mililitro
L   - Litro
PZA - Pieza
```

Total inicial:

```text
5
```

---

# Productos

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

La lógica automática todavía no está implementada.

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

La lógica automática de recálculo todavía no está implementada.

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

Rama actual:

```text
feature/TEN-006-self-registration
```

Estado funcional:

```text
Implementado
Build OK
Prueba funcional OK
Documentación técnica creada
```

Antes de cerrar la rama se debe:

```text
actualizar docs/ai/CURRENT_STATE.md
git add
commit
push
Pull Request → develop
```

No asumir que TEN-006 está en `develop` hasta completar ese flujo.

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

## Productos

Pendiente:

- listar;
- consultar detalle;
- crear;
- editar;
- desactivar/eliminar;
- historial de precios;
- validaciones;
- auditoría.

## Recetas

Pendiente:

- listar;
- crear;
- editar;
- agregar ingredientes;
- eliminar ingredientes;
- calcular costo;
- precio sugerido;
- multiplicador;
- historial de costos.

## Recálculo

Pendiente:

```text
Cambio precio producto
↓
recalcular UnitCost
↓
buscar recetas afectadas
↓
recalcular RecipeItem
↓
recalcular Recipe.TotalCost
↓
recalcular SuggestedPrice
↓
guardar RecipeCostHistory
```

## Configuración

Pendiente CRUD de:

```text
settings
```

## Usuarios tenant

Pendiente:

- crear TENANT_USER;
- editar;
- bloquear;
- permisos;
- administración por TENANT_OWNER.

---

# Funcionalidades todavía no implementadas

- confirmación de correo;
- recuperación de contraseña;
- cambio de contraseña desde UI;
- CRUD Productos;
- historial automático de cambios de precio;
- CRUD Recetas;
- recálculo automático de recetas;
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

Después de cerrar TEN-006:

```text
TEN-007 - Products
```

Objetivo:

Implementar el primer módulo operacional real sobre:

```text
TenantDbContext
```

usando obligatoriamente:

```text
ITenantDbContextFactory
```

---

# TEN-007 previsto

Orden recomendado:

```text
TEN-007A
Contratos de productos

TEN-007B
Servicio query listado

TEN-007C
GET /api/products

TEN-007D
GET /api/products/{id}

TEN-007E
Crear producto

TEN-007F
Editar producto

TEN-007G
Cambio de precio + historial

TEN-007H
Desactivar/eliminar según reglas

TEN-007I
Documentación + pruebas + Git
```

No implementar recetas antes de estabilizar productos.

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
```

Pendiente principal:

```text
⏳ Cerrar Git de TEN-006
⏳ TEN-007 Products
⏳ Recipes
⏳ Recalculation
⏳ Configuration CRUD
⏳ Notifications
⏳ Blazor UI
⏳ Production deployment
```

---

# Punto exacto para continuar

Rama:

```text
feature/TEN-006-self-registration
```

TEN-006 ya:

```text
compila
funciona
fue probado
```

Se debe terminar:

```text
actualizar CURRENT_STATE.md
git add .
git status
commit
push
PR a develop
```

Después:

```text
checkout develop
pull origin develop
crear feature/TEN-007-products
```

No volver a implementar provisioning, seed, tenant resolution o autoregistro.

Esos bloques ya existen y fueron validados.
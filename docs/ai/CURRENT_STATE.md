# Estado actual del proyecto

## Fecha de estado

2026-08-26

## Fase actual

Infraestructura multi-tenant y autenticación base.

## Stack confirmado

- .NET 10
- ASP.NET Core
- Blazor WebAssembly PWA
- Entity Framework Core
- PostgreSQL 18.6
- ASP.NET Core Identity
- Docker
- Docker Compose
- Swagger para desarrollo

## Solución

Proyectos existentes:

- SweetSecrets.Api
- SweetSecrets.Web
- SweetSecrets.Domain
- SweetSecrets.Application
- SweetSecrets.Infrastructure
- SweetSecrets.Contracts
- SweetSecrets.UnitTests
- SweetSecrets.IntegrationTests

Todos compilan correctamente.

## PostgreSQL

PostgreSQL funciona mediante Docker.

Contenedor de desarrollo:

sweetsecrets-postgres

Usuario de desarrollo:

sweetsecrets_admin

Las contraseñas no están almacenadas en código.

Se utilizan:

- Docker secrets para PostgreSQL.
- User Secrets para desarrollo ASP.NET.

## MASTER DB

Base:

sweetsecrets_master

Contexto:

MasterDbContext

Migraciones aplicadas:

- MST001_InitialMaster
- MST002_AddAuditAndSessions

## Tablas MASTER actuales

- __EFMigrationsHistory
- tenants
- platform_users
- platform_roles
- platform_user_roles
- platform_user_claims
- platform_user_logins
- platform_role_claims
- platform_user_tokens
- platform_audit_logs
- user_sessions

## Roles

Roles creados:

- PLATFORM_ADMIN
- TENANT_OWNER
- TENANT_USER

## PLATFORM_ADMIN

Existe un administrador inicial.

Características:

- TenantId = null
- IsActive = true
- IsBlocked = false
- rol PLATFORM_ADMIN

Las credenciales no están almacenadas en Git.

## Autenticación

Implementado:

- login;
- logout;
- cookies ASP.NET Core Identity;
- sesiones;
- auditoría de login;
- auditoría de logout;
- auditoría de login fallido;
- bloqueo temporal por intentos fallidos;
- endpoint de usuario autenticado;
- autorización basada en roles.

Endpoints actuales:

POST /api/auth/login

POST /api/auth/logout

GET /api/auth/me

## Sesiones

La tabla user_sessions registra:

- Id de sesión;
- usuario;
- inicio;
- última actividad;
- finalización;
- estado;
- IP;
- User-Agent;
- motivo de cierre.

El middleware de actividad actualiza:

- user_sessions.LastActivityAt
- platform_users.LastActivityAt

Un usuario se considera online según una ventana reciente de actividad.

## Administración de usuarios

Implementado:

GET /api/admin/users

POST /api/admin/users/{userId}/block

POST /api/admin/users/{userId}/unblock

Solo PLATFORM_ADMIN puede acceder.

La consulta administrativa devuelve:

- Id;
- TenantId;
- correo;
- nombre;
- activo;
- bloqueado;
- online;
- último login;
- última actividad;
- fecha de creación.

## Bloqueo

Al bloquear un usuario:

1. IsBlocked cambia a true.
2. Se actualiza SecurityStamp.
3. Se cierran sus sesiones activas.
4. Se registra USER_BLOCKED en auditoría.

Al desbloquear:

1. IsBlocked cambia a false.
2. Se actualiza SecurityStamp.
3. Se registra USER_UNBLOCKED.

## Auditoría global

Existe IPlatformAuditService.

Implementación:

PlatformAuditService

Tabla:

platform_audit_logs

Eventos previstos o implementados:

- LOGIN_SUCCESS
- LOGIN_FAILED
- LOGIN_LOCKED_OUT
- LOGOUT
- USER_BLOCKED
- USER_UNBLOCKED
- TENANT_CREATED
- TENANT_FAILED
- NOTIFICATION_SENT

## Tenant DB

Contexto:

TenantDbContext

Migración actual:

TEN001_InitialTenant

Base de desarrollo:

sweetsecrets_tenant_template

Esta base es únicamente una plantilla de desarrollo para probar migraciones.

No pertenece a un usuario real.

## Tablas tenant actuales

- __EFMigrationsHistory
- units
- products
- recipes
- recipe_items
- settings
- product_price_history
- recipe_cost_history

## Modelo multi-tenant

Se utiliza:

Database-per-tenant.

Flujo previsto:

Usuario
-> MASTER DB
-> TenantId
-> Tenant
-> DatabaseName
-> PostgreSQL tenant independiente

Cada usuario tenant trabajará únicamente con su base correspondiente.

## Productos

Modelo definido.

Campos principales:

- Name
- PurchaseQuantity
- UnitId
- PurchasePrice
- UnitCost
- IsActive
- CreatedAt
- CreatedBy
- UpdatedAt
- UpdatedBy

## Recetas

Modelo relacional definido.

Una receta contiene RecipeItems.

RecipeItem relaciona:

- Recipe
- Product
- Unit

Las recetas no se almacenan como un único JSON.

## Historial

Definidas tablas:

product_price_history

recipe_cost_history

El objetivo es conservar trazabilidad cuando cambien costos.

## Configuración

Existe tabla tenant:

settings

Permitirá configuraciones como:

MULTIPLIER

## Swagger

Swagger está habilitado únicamente como herramienta de desarrollo.

URL de desarrollo actual:

https://localhost:7010/swagger

No se considera una interfaz de usuario final.

## No implementado todavía

- autorregistro;
- creación automática de tenant;
- creación automática de base tenant;
- ejecución automática de migraciones tenant;
- catálogo inicial;
- seed de unidades;
- seed de configuración;
- confirmación de correo;
- recuperación de contraseña;
- CRUD Productos;
- CRUD Recetas;
- recálculo automático de recetas;
- CRUD configuración;
- notificaciones;
- SignalR;
- Web Push;
- UI Blazor final;
- dashboard PLATFORM_ADMIN;
- dashboard TENANT_OWNER;
- Docker de producción;
- backups;
- monitoreo.

## Próximo objetivo

Implementar TenantProvisioningService.

Debe ser capaz de:

1. crear registro Tenant en MASTER;
2. generar código único;
3. generar nombre de base;
4. crear base PostgreSQL;
5. ejecutar migraciones tenant;
6. cargar unidades iniciales;
7. cargar catálogo inicial;
8. cargar configuración inicial;
9. activar el tenant;
10. registrar auditoría.

## Regla crítica

No implementar registro público antes de terminar y probar el provisioning
automático de tenants.
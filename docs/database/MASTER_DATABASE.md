# Tenant Database

## Propósito

Cada tenant de SweetSecrets posee una base PostgreSQL independiente.

El objetivo es proporcionar aislamiento fuerte de información.

## Estrategia

Database-per-tenant.

Ejemplo:

sweetsecrets_tenant_000001

sweetsecrets_tenant_000002

sweetsecrets_tenant_000003

## Contexto EF Core

TenantDbContext

## Migraciones

Primera migración:

TEN001_InitialTenant

## Base template

Durante desarrollo existe:

sweetsecrets_tenant_template

Su objetivo es:

- probar migraciones;
- validar esquema;
- detectar errores.

No representa un tenant real.

No debe usarse como base compartida de usuarios.

## Tablas actuales

### units

Catálogo normalizado de unidades.

Campos conceptuales:

- Id
- Code
- Name
- Symbol
- IsActive

Ejemplos previstos:

GR
ML
KG
L
PZA

## products

Productos propios del tenant.

Campos principales:

- Id
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

## recipes

Recetas propias del tenant.

Campos principales:

- Id
- Name
- Description
- Multiplier
- TotalCost
- SuggestedPrice
- IsActive
- CreatedAt
- CreatedBy
- UpdatedAt
- UpdatedBy

## recipe_items

Ingredientes de una receta.

Relaciona:

Recipe
Product
Unit

Campos principales:

- RecipeId
- ProductId
- Quantity
- UnitId
- UnitCost
- TotalCost

## settings

Configuraciones del tenant.

Modelo clave/valor.

Ejemplo:

Key:

MULTIPLIER

Value:

3

## product_price_history

Conserva cambios históricos de productos.

Campos:

- ProductId
- PreviousPrice
- NewPrice
- PreviousUnitCost
- NewUnitCost
- ChangedBy
- ChangedAt

## recipe_cost_history

Conserva cambios de costo en recetas.

Campos:

- RecipeId
- PreviousCost
- NewCost
- Reason
- CreatedAt

## Reglas

Una base tenant:

- no contiene usuarios Identity;
- no contiene roles de plataforma;
- no contiene otros tenants;
- no conoce bases de otros tenants.

Los usuarios viven en MASTER.

## Creación futura

TenantProvisioningService será responsable de:

1. generar DatabaseName;
2. crear la base;
3. construir connection string;
4. ejecutar TenantDbContext.Database.MigrateAsync();
5. cargar seed;
6. activar tenant.

## Seed inicial

Cada base tenant recibirá:

### Unidades

Catálogo inicial normalizado.

### Productos

Catálogo inicial proveniente de la aplicación MAUI anterior.

Este catálogo es una copia.

Después cada tenant podrá:

- agregar;
- editar;
- desactivar/eliminar.

### Configuración

Configuración inicial prevista:

MULTIPLIER = 3

## Seguridad

Nunca aceptar DatabaseName desde una petición cliente.

DatabaseName debe provenir de MASTER.

Nunca colocar credenciales PostgreSQL dentro de la base tenant.

## Backups

Pendiente de implementación.

La estrategia futura deberá permitir:

- backup MASTER;
- backup individual por tenant;
- restauración individual por tenant.
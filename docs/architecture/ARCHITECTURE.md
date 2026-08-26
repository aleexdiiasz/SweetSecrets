# Arquitectura de SweetSecrets

## Objetivo

SweetSecrets es una PWA multi-tenant para control de productos,
costos y recetas.

La aplicación está diseñada inicialmente para aproximadamente 25 a 30
usuarios, pero debe poder crecer sin cambiar su arquitectura principal.

## Stack

- .NET 10
- ASP.NET Core
- Blazor WebAssembly PWA
- Entity Framework Core
- PostgreSQL
- ASP.NET Core Identity
- SignalR
- Web Push
- Docker
- Docker Compose

## Arquitectura lógica

Cliente
-> Blazor WebAssembly PWA
-> ASP.NET Core API
-> Application
-> Infrastructure
-> PostgreSQL

## Proyectos

### SweetSecrets.Domain

Contiene:

- entidades;
- enumeraciones;
- reglas puras del dominio.

No debe depender de Infrastructure, API o Web.

### SweetSecrets.Application

Contiene:

- interfaces;
- casos de uso;
- modelos de aplicación;
- reglas de negocio;
- contratos internos.

### SweetSecrets.Infrastructure

Contiene:

- Entity Framework Core;
- PostgreSQL;
- Identity;
- persistencia;
- servicios;
- auditoría;
- sesiones;
- implementación de interfaces.

### SweetSecrets.Contracts

Contiene:

- requests HTTP;
- responses HTTP;
- DTOs compartidos con Web.

### SweetSecrets.Api

Responsable de:

- HTTP;
- autenticación;
- autorización;
- middleware;
- controllers;
- resolución de usuario;
- resolución de tenant;
- Swagger de desarrollo.

### SweetSecrets.Web

Blazor WebAssembly PWA.

Debe contener:

- componentes;
- páginas;
- layout;
- servicios HTTP;
- estado visual.

No debe acceder directamente a PostgreSQL.

### Tests

SweetSecrets.UnitTests:

- reglas de negocio.

SweetSecrets.IntegrationTests:

- API;
- PostgreSQL;
- infraestructura;
- multi-tenancy.

## Flujo de datos

El flujo obligatorio es:

Blazor
-> API
-> Application
-> Infrastructure
-> PostgreSQL

No está permitido:

Blazor
-> PostgreSQL

Tampoco:

Controller
-> SQL directo

## Multi-tenancy

Modelo elegido:

Database-per-tenant.

Existe una base MASTER independiente.

Ejemplo:

sweetsecrets_master

Y bases tenant independientes:

sweetsecrets_tenant_000001
sweetsecrets_tenant_000002
sweetsecrets_tenant_000003

## MASTER DB

MASTER administra:

- usuarios;
- Identity;
- roles;
- tenants;
- sesiones;
- auditoría global;
- administración de plataforma.

MASTER no almacena:

- productos;
- recetas;
- ingredientes;
- configuraciones operativas tenant.

## Tenant DB

Cada tenant almacena únicamente sus datos.

Actualmente:

- units;
- products;
- recipes;
- recipe_items;
- settings;
- product_price_history;
- recipe_cost_history.

## Resolución futura de tenant

Después de autenticar:

Usuario Identity
-> TenantId
-> tenants
-> DatabaseName
-> conexión PostgreSQL
-> TenantDbContext

El frontend nunca debe enviar una base de datos arbitraria.

El frontend tampoco debe controlar libremente TenantId.

El tenant se obtiene desde la identidad autenticada.

## PLATFORM_ADMIN

PLATFORM_ADMIN no pertenece a un tenant.

Configuración:

TenantId = null

Puede administrar la plataforma global.

## TENANT_OWNER

Representa al propietario principal del tenant.

Inicialmente cada repostera será TENANT_OWNER de su tenant.

## TENANT_USER

Rol previsto para crecimiento.

Permitirá que un tenant tenga varios usuarios en el futuro.

## Autenticación

ASP.NET Core Identity administra:

- contraseña;
- hash;
- roles;
- intentos fallidos;
- SecurityStamp;
- cookies.

La PWA utilizará cookie segura HttpOnly.

No almacenar JWT o contraseña en localStorage.

## Sesiones

Cada login crea un registro en:

user_sessions

La sesión permite:

- identificar usuario conectado;
- registrar inicio;
- registrar actividad;
- cerrar sesión;
- invalidar sesiones;
- detectar usuarios online.

## Auditoría

Los movimientos importantes deben registrarse.

Ejemplos:

- login;
- logout;
- bloqueo;
- desbloqueo;
- creación de tenant;
- fallo de provisioning;
- envío de notificación.

## Provisioning

El registro de una nueva repostera no será únicamente un INSERT de usuario.

Debe realizar:

Registro
-> crear Tenant MASTER
-> crear PostgreSQL DB
-> ejecutar migraciones
-> cargar seed inicial
-> crear usuario TENANT_OWNER
-> activar tenant

Si el proceso falla, el tenant no debe quedar Active.

## Consistencia

El proceso de provisioning debe considerar que CREATE DATABASE no puede
participar de la misma transacción EF que MASTER.

Por ello se utilizarán estados:

Provisioning
Active
Suspended
Disabled
Failed

El proceso debe poder detectar y registrar fallos parciales.

## Datos iniciales

Todo tenant nuevo recibirá una copia propia de:

- unidades;
- catálogo inicial de productos;
- configuración inicial.

Después de copiar los datos, cada tenant será independiente.

Modificar un producto en un tenant no debe afectar a otro tenant.

## Productos y recetas

Los productos son relacionales.

Las recetas son relacionales.

RecipeItem relaciona Recipe con Product.

No almacenar la receta completa únicamente como JSON.

## Cambio de costo

Regla de negocio objetivo:

Cambio de PurchasePrice
-> recalcular UnitCost
-> guardar ProductPriceHistory
-> localizar recetas afectadas
-> recalcular RecipeItem
-> recalcular Recipe.TotalCost
-> recalcular SuggestedPrice
-> guardar RecipeCostHistory

## PWA

SweetSecrets requiere conexión a Internet en V1.

No implementar sincronización offline de datos.

La capacidad PWA permitirá:

- instalación;
- acceso desde navegador;
- icono;
- service worker;
- notificaciones futuras.

## Infraestructura inicial

Internet
-> Reverse Proxy HTTPS
-> ASP.NET Core
-> PostgreSQL

Inicialmente no utilizar:

- Kubernetes;
- microservicios;
- Redis;
- colas externas.

Solo agregar infraestructura cuando exista necesidad real.

## Docker

Desarrollo utiliza Docker Compose.

PostgreSQL no debe exponerse públicamente en producción.

La aplicación será la única capa con acceso operativo a las bases.

## Regla de evolución

No cambiar una decisión arquitectónica importante sin documentarla mediante ADR.
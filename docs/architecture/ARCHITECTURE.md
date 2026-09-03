# Arquitectura de SweetSecrets

## Validación de arquitectura TEN-033

La ruta real `Nginx -> API -> Application/Infrastructure -> PostgreSQL` fue ejercitada con dos tenants y un restore completo. El frontend no seleccionó TenantId ni DatabaseName; la identidad resolvió cada base y las consultas de productos, recetas, settings, historiales y dashboard permanecieron aisladas. MASTER conservó Identity, sesiones, tenants y auditoría; cada tenant mantuvo su propia base.

La prueba confirmó el migrador MASTER one-shot, provisioning con migrations/seed tenant, PostgreSQL no publicado, persistencia del key ring y recuperación del conjunto MASTER + tenants + Data Protection. Detalle: [PRODUCTION_E2E_VALIDATION.md](../technical/PRODUCTION_E2E_VALIDATION.md).

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

## Topología Docker de producción

TEN-031 implementa una baseline con Nginx sirviendo Blazor WebAssembly y actuando como reverse proxy same-origin. Solo Nginx publica puerto; API y PostgreSQL quedan en una red Docker privada. PostgreSQL mantiene MASTER y las bases tenant en un volumen, y la API mantiene sus claves de Data Protection en otro volumen.

Las migraciones MASTER se ejecutan en un contenedor one-shot antes de la API. El proceso API normal no migra al arrancar. El provisioning conserva la creación y migración de cada tenant nuevo; la coordinación de migraciones para tenants existentes sigue siendo una responsabilidad operacional pendiente.

## Unidad de backup

Por el modelo database-per-tenant, la unidad completa de recuperación es MASTER + todas las bases listadas en `MASTER.tenants.DatabaseName` + el key ring Data Protection. Un dump aislado no representa la plataforma completa. TEN-032 usa snapshots consistentes por base con `pg_dump -Fc`; no existe una transacción distribuida entre bases, por lo que el backup es coordinado y debe reducir escrituras durante la ventana operacional.

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

## Disponibilidad operacional

TEN-023 agrega `/health/live` para disponibilidad del proceso y `/health/ready` para conectividad con MASTER. Readiness no recorre bases tenant: cada base operacional se abre únicamente después de resolver al tenant desde una identidad autenticada. Los detalles y requisitos Production están documentados en `docs/technical/PRODUCTION_READINESS.md`.

## Docker

Desarrollo utiliza Docker Compose.

PostgreSQL no debe exponerse públicamente en producción.

La aplicación será la única capa con acceso operativo a las bases.

## Regla de evolución

No cambiar una decisión arquitectónica importante sin documentarla mediante ADR.

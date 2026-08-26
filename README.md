# SweetSecrets

SweetSecrets es una aplicación web PWA para administración de productos,
costos y recetas orientada inicialmente a reposteras.

## Objetivo

Permitir a cada usuario administrar de forma independiente:

- Productos.
- Costos.
- Recetas.
- Configuración.
- Historial de cambios.
- Notificaciones.

La plataforma contará con administración global de usuarios y tenants.

## Tecnología

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

## Arquitectura

La aplicación utilizará arquitectura multi-tenant.

Existirá:

- Una base PostgreSQL MASTER.
- Una base PostgreSQL independiente por tenant.

La base MASTER administrará la plataforma.

Cada tenant almacenará únicamente su información operativa.

## Proyectos

- SweetSecrets.Api
- SweetSecrets.Web
- SweetSecrets.Domain
- SweetSecrets.Application
- SweetSecrets.Infrastructure
- SweetSecrets.Contracts
- SweetSecrets.UnitTests
- SweetSecrets.IntegrationTests

## Estado

Proyecto inicial creado.

Todos los proyectos compilan correctamente con .NET 10.

Consultar:

- `AGENTS.md`
- `docs/architecture/ARCHITECTURE.md`
- `docs/functional/PROJECT_SCOPE.md`
- `docs/ai/CURRENT_STATE.md`
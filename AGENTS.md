# AGENTS.md

## Propósito

Este archivo define las reglas obligatorias para cualquier IA,
agente o desarrollador que trabaje en SweetSecrets.

## Stack obligatorio

- .NET 10
- ASP.NET Core
- Blazor WebAssembly PWA
- PostgreSQL
- Entity Framework Core
- Docker

No sustituir estas tecnologías sin una decisión arquitectónica documentada.

## Arquitectura

La solución utiliza separación por capas:

- Domain
- Application
- Infrastructure
- Contracts
- Api
- Web

No colocar lógica de negocio directamente en componentes Blazor.

No acceder directamente a PostgreSQL desde SweetSecrets.Web.

El flujo debe ser:

Blazor Web -> ASP.NET API -> Application -> Infrastructure -> PostgreSQL

## Multi-tenancy

SweetSecrets es multi-tenant.

Debe existir:

1. Base MASTER independiente.
2. Base PostgreSQL independiente por tenant.

Nunca mezclar información operativa de distintos tenants.

El frontend nunca debe poder seleccionar arbitrariamente la base de datos
o TenantId sobre el cual desea operar.

El tenant debe resolverse desde la identidad autenticada.

## Roles previstos

### PLATFORM_ADMIN

Administrador global de la plataforma.

Puede administrar:

- Usuarios.
- Tenants.
- Bloqueos.
- Sesiones.
- Notificaciones.
- Movimientos.
- Estado de plataforma.

### TENANT_OWNER

Propietario del tenant.

Puede administrar:

- Productos.
- Recetas.
- Configuración.
- Información propia.

### TENANT_USER

Rol preparado para crecimiento futuro.

## Registro

Los usuarios pueden autorregistrarse.

Un registro exitoso deberá posteriormente:

1. Crear usuario.
2. Crear tenant.
3. Crear base tenant.
4. Ejecutar migraciones.
5. Cargar catálogo inicial.
6. Crear configuración inicial.

## Productos

Cada tenant tiene sus propios productos.

Existe un catálogo base utilizado solamente para inicializar tenants.

Después de inicializar el tenant, los productos pueden ser:

- creados;
- modificados;
- desactivados/eliminados.

## Recetas

Cada receta pertenece exclusivamente a un tenant.

Los ingredientes deben relacionarse con productos mediante relaciones
de base de datos.

No almacenar la receta completa únicamente como JSON.

## Costos

Cuando cambia el precio de un producto:

1. recalcular costo unitario;
2. localizar recetas afectadas;
3. recalcular sus costos;
4. recalcular precio sugerido;
5. registrar historial.

## Historial

No sobrescribir información importante sin trazabilidad.

Debe existir historial para cambios relevantes como:

- precios;
- costos;
- recetas;
- bloqueos;
- configuraciones.

## Notificaciones

El PLATFORM_ADMIN podrá crear notificaciones personalizadas.

Deben contemplarse:

- notificaciones internas;
- SignalR;
- Web Push.

## Internet

SweetSecrets requiere conexión a Internet.

No implementar sincronización offline en V1.

## Seguridad

Nunca almacenar:

- contraseñas en texto plano;
- connection strings productivas en código;
- API keys en código;
- secretos SMTP en código.

Usar configuración segura y variables de entorno.

## Desarrollo

Cada cambio debe:

1. tener objetivo claro;
2. respetar arquitectura;
3. compilar;
4. incluir migraciones cuando correspondan;
5. actualizar documentación cuando cambie comportamiento;
6. incluir pruebas cuando exista lógica de negocio.

No continuar sobre un build roto.

## Documentación

Actualizar siempre que corresponda:

- documentación técnica;
- documentación funcional;
- documentación para usuarios;
- documentación para IA;
- decisiones arquitectónicas.
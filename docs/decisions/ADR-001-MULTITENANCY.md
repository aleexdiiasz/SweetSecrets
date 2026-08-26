# ADR-001: Estrategia Multi-Tenant

## Estado

Aceptada.

## Fecha

2026-08-26

## Contexto

SweetSecrets será utilizada inicialmente por aproximadamente 25 a 30
reposteras.

Cada usuaria debe tener:

- sus productos;
- sus recetas;
- sus precios;
- su configuración;
- su historial.

Los datos de una usuaria no deben mezclarse con los de otra.

Existe posibilidad de crecimiento futuro.

## Decisión

Utilizar arquitectura:

Database-per-tenant.

Existirá:

1. una base MASTER;
2. una base PostgreSQL independiente por tenant.

## MASTER

MASTER almacena:

- usuarios;
- Identity;
- roles;
- tenants;
- sesiones;
- auditoría global.

## Tenant

Cada tenant almacena:

- productos;
- recetas;
- ingredientes;
- unidades;
- configuración;
- historial de precios;
- historial de costos.

## Razones

### Aislamiento

Una base tenant no contiene información operativa de otro tenant.

### Seguridad

Reduce el riesgo de que un error de filtro TenantId exponga datos cruzados.

### Backup

Será posible respaldar o restaurar un tenant de forma independiente.

### Mantenimiento

Una base individual puede analizarse o restaurarse sin afectar a todas.

### Crecimiento

La arquitectura permite mover tenants a otra instancia PostgreSQL en el futuro
sin rediseñar el modelo funcional.

## Costos

La estrategia aumenta:

- número de bases;
- complejidad de migraciones;
- complejidad del provisioning;
- administración de backups.

El costo se considera aceptable debido al número inicial de tenants y al nivel
de aislamiento deseado.

## Alternativa rechazada

Una sola base con TenantId en todas las tablas operativas.

Ventaja:

- menor complejidad.

Problema:

- mayor riesgo de mezcla de datos;
- mayor dependencia de filtros correctos;
- backup/restauración individual más complejos.

## Provisioning

Cada registro nuevo deberá:

1. crear Tenant en MASTER con estado Provisioning;
2. generar DatabaseName;
3. crear PostgreSQL DB;
4. ejecutar migraciones;
5. cargar seed;
6. crear TENANT_OWNER;
7. cambiar estado a Active.

Si falla:

estado = Failed.

## Identificación

El frontend no seleccionará el tenant.

Se obtiene mediante:

Usuario autenticado
-> TenantId
-> MASTER
-> DatabaseName

## Nombres

Formato previsto:

sweetsecrets_tenant_XXXXXX

El nombre real se genera exclusivamente en backend.

## Consecuencia

Toda funcionalidad operativa futura deberá ejecutarse mediante TenantDbContext.

MASTER DbContext no debe utilizarse para productos o recetas.

## Regla

Cualquier cambio de esta estrategia requiere un nuevo ADR.
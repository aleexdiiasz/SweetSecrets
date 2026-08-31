# Tenant Owner Application Shell

## Issue

TEN-014 - Tenant Owner Application Shell

## Objetivo

Reemplazar la interfaz visual del template de Blazor por el shell operacional inicial de SweetSecrets para TENANT_OWNER.

Este bloque no implementa CRUD de productos, recetas ni configuracion.

## Rama

feature/TEN-014-tenant-owner-shell

## Layout principal

Se reemplazo el MainLayout del template por una estructura con:

- sidebar
- header
- area principal de contenido
- logout global
- comportamiento responsive

## Navegacion

El menu principal contiene:

- Inicio
- Productos
- Recetas
- Configuracion

Rutas:

- /
- /productos
- /recetas
- /configuracion

Las rutas operacionales estan protegidas con TENANT_OWNER.

## Logout

Cerrar sesion fue movido desde Home hacia MainLayout.

Esto permite cerrar sesion desde cualquier pantalla que utilice el shell.

Flujo:

TENANT_OWNER autenticado
-> Cerrar sesion
-> POST /api/auth/logout
-> /login

## Dashboard

La pagina inicial incorpora un dashboard visual con tarjetas para:

- Productos
- Recetas
- Configuracion

Las tarjetas son informativas en TEN-014 y no consumen todavia endpoints operacionales.

## Paginas placeholder

Se crearon pantallas protegidas para:

- Products.razor
- Recipes.razor
- Settings.razor

Su objetivo es dejar completa la navegacion del shell antes de implementar cada modulo funcional.

## Responsive

Validado manualmente aproximadamente a 390 px:

- sidebar pasa a la parte superior
- contenido queda debajo
- tarjetas del dashboard pasan a una columna
- logout permanece visible y funcional

## Validacion funcional

Comprobado:

- Inicio navega correctamente
- Productos navega correctamente
- Recetas navega correctamente
- Configuracion navega correctamente
- opcion activa cambia visualmente
- logout funciona desde el header
- responsive funciona

## Build

Todos los bloques incrementales finalizaron con:

Build succeeded

## Limites del alcance

TEN-014 no implementa:

- listado real de productos
- CRUD de productos
- listado real de recetas
- CRUD de recetas
- configuracion funcional
- TENANT_USER

Estos modulos deben continuar en issues independientes.

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

- sidebar exclusivo de escritorio
- header
- area principal de contenido
- navegación inferior fija para móvil y tablet
- comportamiento responsive

## Navegacion

El menu principal contiene:

- Inicio
- Productos
- Recetas
- Cuenta

Rutas:

- /
- /productos
- /recetas
- /cuenta

Las rutas operacionales estan protegidas con TENANT_OWNER.

`/configuracion` continúa protegida y funcional, pero no forma parte del menú principal. El acceso normal es Cuenta -> Configuración -> `/configuracion`, y Configuración ofrece un enlace claro para volver a Cuenta.

## Logout

TEN-030 concentra Cerrar sesión dentro de `/cuenta`. MainLayout, sidebar y navegación inferior no muestran logout. Se conserva exactamente el flujo existente:

Flujo:

TENANT_OWNER autenticado
-> Cuenta -> Cerrar sesion
-> POST /api/auth/logout
-> /login

## Dashboard

TEN-022 reemplaza las tarjetas informativas iniciales por un dashboard operacional conectado a `GET /api/dashboard`. Muestra conteos reales de productos y recetas, costo promedio de recetas activas, actividad reciente y accesos rápidos. La API resuelve la base tenant desde la identidad y no acepta identificadores tenant desde Web.

La especificación vigente se encuentra en `docs/technical/DASHBOARD_UI.md`.

## Paginas placeholder

Se crearon pantallas protegidas para:

- Products.razor
- Recipes.razor
- Settings.razor

Su objetivo es dejar completa la navegacion del shell antes de implementar cada modulo funcional.

## Responsive

La arquitectura responsive tenant es:

- Desktop con puntero fino y ancho desde 1280 px: sidebar persistente.
- Móvil y tablet hasta 1279 px: header compacto, contenido y bottom navigation fija.
- Dispositivos con `hover: none` o `pointer: coarse`: bottom navigation aunque la tablet horizontal alcance o supere 1280 px.
- Móvil hasta 760 px: grids y formularios de una columna, tablas transformadas en cards.

La barra inferior respeta safe areas, reserva padding de contenido y muestra Inicio, Productos, Recetas y Cuenta. Esto cubre móvil vertical/horizontal y tablet vertical/horizontal sin convertir una tablet ancha automáticamente en desktop.

## Validacion funcional

Comprobado:

- Inicio navega correctamente
- Productos navega correctamente
- Recetas navega correctamente
- Cuenta navega correctamente
- Configuracion se abre desde Cuenta y permite volver
- opcion activa cambia visualmente
- logout funciona desde Cuenta
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

## Separacion del area administrativa

TEN-024 agrega un shell independiente en `/admin` para `PLATFORM_ADMIN`. No reutiliza `MainLayout`, no muestra navegacion tenant y no consulta bases tenant. Detalle: `docs/technical/PLATFORM_ADMIN_SHELL.md`.

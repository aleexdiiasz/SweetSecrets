# Tenant Products UI

## Issue

TEN-015 - Tenant Products UI

## Rama

feature/TEN-015-products-ui

## Objetivo

Implementar la interfaz funcional de Productos para TENANT_OWNER conectada a la API multi-tenant existente.

## Seguridad multi-tenant

La UI no envia:

- TenantId
- DatabaseName
- ConnectionString

El contexto operacional continua resolviendose mediante:

usuario autenticado
-> MASTER
-> tenant Active
-> ITenantDbContextFactory
-> base PostgreSQL del tenant

## Endpoint de unidades

Para alimentar los selectores de productos se agrego lectura de unidades:

GET /api/units

Componentes:

- UnitListItem
- IUnitQueryService
- UnitQueryService
- UnitListItemResponse
- UnitsController
- UnitsApiClient

La consulta utiliza ITenantDbContextFactory y AsNoTracking.

No se implemento CRUD de unidades.

Unidades validadas:

- GR - Gramo
- KG - Kilogramo
- L - Litro
- ML - Mililitro
- PZA - Pieza

## ProductsApiClient

Se implementaron operaciones Web para:

- GET /api/products
- POST /api/products
- PUT /api/products/{id}
- PATCH /api/products/{id}/active

## Listado

La ruta:

/productos

consulta productos reales del tenant autenticado.

Columnas:

- Producto
- Cantidad compra
- Unidad
- Precio compra
- Costo unitario
- Estado
- Acciones

## Busqueda

Se implemento busqueda local en memoria por nombre.

El filtrado ocurre mientras el usuario escribe y no requiere una nueva solicitud HTTP.

## Alta de producto

El formulario permite capturar:

- Nombre
- Cantidad de compra
- Unidad
- Precio de compra

Validaciones UI:

- nombre obligatorio
- cantidad mayor que cero
- unidad obligatoria
- precio no negativo

El backend conserva las validaciones definitivas.

Prueba funcional:

PRODUCTO PRUEBA TEN-015
Cantidad = 1000
Unidad = GR
Precio = 125
UnitCost = 0.125000

## Edicion de producto

Se implemento edicion con formulario precargado.

Prueba funcional:

PRODUCTO PRUEBA TEN-015 EDITADO
Cantidad = 500
Unidad = GR
Precio = 150
UnitCost = 0.300000

El listado se actualiza despues de guardar.

## Activacion y desactivacion

Se implemento:

PATCH /api/products/{id}/active

Validado:

Activo -> Inactivo -> Activo

La UI actualiza:

- badge de estado
- texto Activar / Desactivar

## Cultura y moneda

SweetSecrets.Web utiliza cultura:

es-MX

Se habilito:

BlazorWebAssemblyLoadAllGlobalizationData

Los precios ahora se muestran con simbolo $.

Las cantidades eliminan ceros decimales innecesarios mediante formato 0.####.

## Responsive

Validado aproximadamente a 390 px:

- buscador debajo del titulo
- boton Nuevo producto visible
- tabla con desplazamiento horizontal
- acciones accesibles
- formulario Nuevo producto en una columna
- formulario Editar producto en una columna

## Validacion funcional

Comprobado:

- listado real del tenant
- busqueda
- alta
- edicion
- desactivacion
- reactivacion
- selector de unidades
- recalculo de UnitCost por backend
- formato monetario es-MX
- responsive

## Build

Todos los bloques incrementales finalizaron con:

Build succeeded

## Fuera de alcance

TEN-015 no implementa:

- CRUD de unidades
- recetas
- configuracion
- TENANT_USER

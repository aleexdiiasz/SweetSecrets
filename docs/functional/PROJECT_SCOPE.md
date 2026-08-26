# Alcance funcional

## Objetivo

Crear una aplicación sencilla para ayudar a reposteras a controlar costos
de productos y recetas.

## Usuarios

Inicialmente se esperan aproximadamente 25 a 30 usuarios.

El sistema deberá poder crecer sin cambiar su arquitectura principal.

## Plataforma

Cada repostera tendrá un tenant independiente.

Su información será privada y estará almacenada en su propia base de datos.

## Funciones del usuario

El usuario podrá:

- registrarse;
- iniciar sesión;
- recuperar contraseña;
- administrar perfil;
- crear productos;
- modificar productos;
- eliminar o desactivar productos;
- consultar costos;
- crear recetas;
- modificar recetas;
- eliminar o desactivar recetas;
- consultar recetas;
- configurar multiplicadores;
- recibir notificaciones.

## Productos iniciales

Al crear una cuenta se cargará automáticamente un catálogo base de productos.

Este catálogo será una copia independiente.

Los cambios realizados por un usuario no modificarán el catálogo de otros.

## Costos

Los productos tendrán:

- cantidad comprada;
- unidad;
- precio;
- costo unitario.

Cuando cambie un precio deberán recalcularse automáticamente las recetas
relacionadas.

## Administración global

El PLATFORM_ADMIN podrá:

- consultar usuarios;
- consultar usuarios activos;
- consultar sesiones;
- bloquear usuarios;
- desbloquear usuarios;
- consultar movimientos;
- consultar tenants;
- enviar notificaciones;
- administrar estado de la plataforma.

## Notificaciones

El administrador podrá escribir manualmente:

- título;
- mensaje;
- tipo;
- destinatarios.

Ejemplos:

- nueva actualización;
- mantenimiento;
- nueva funcionalidad;
- aviso importante.

## Conectividad

SweetSecrets requiere Internet.

No se contempla modificación offline de información en la primera versión.
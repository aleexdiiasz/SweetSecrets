# TEN-030 - UI/UX Redesign + Design System

## Objetivo

TEN-030 unifica la experiencia visual de SweetSecrets para escritorio, tablet y móvil sin cambiar reglas de negocio, contratos, endpoints, autenticación, autorización, resolución tenant, auditoría, health checks ni rate limiting.

El rediseño cubre las pantallas públicas de identidad, el espacio TENANT_OWNER y el espacio PLATFORM_ADMIN. Las rutas, formularios, mensajes y acciones existentes conservan su comportamiento.

## Sistema de diseño

La hoja `wwwroot/css/design-system.css`, cargada después de los estilos aislados de Blazor, es la capa visual compartida. Define tokens CSS reutilizables para colores de marca y estados, espaciado, radios, sombras, superficies, cards, botones, controles de formulario, badges, tablas, mensajes y layouts responsive.

La paleta usa fondo cálido `#FFF9F7`, cards blancas, bordes `#F4ECE8`, texto `#4A3F3A` y acentos pastel rosa, vainilla, menta, azul y lavanda. La acción primaria se basa en `#59C7C2`; las acciones destructivas utilizan una variante accesible de `#FF7F7F`.

La marca se representa mediante un monograma SS y superficies sutiles inspiradas en repostería. No se agregaron imágenes, fuentes remotas ni dependencias externas.

## Layouts responsive

### Escritorio

- Sidebar persistente para TENANT_OWNER y PLATFORM_ADMIN.
- Header sticky con contexto, identidad/cuenta y cierre de sesión.
- Contenido con ancho máximo, más aire y jerarquía consistente.
- Dashboards en grids; formularios y paneles en cards suaves.

### Tablet

- Usa la misma arquitectura de aplicación que móvil: header, contenido y navegación inferior fija.
- No muestra sidebar, tampoco en orientación horizontal.
- Grids de dos columnas donde el contenido lo permite.
- Filtros administrativos reorganizados sin perder controles.

### Móvil

- Navegación inferior fija tipo app con objetivos táctiles y safe areas.
- El menú TENANT_OWNER contiene Inicio, Productos, Recetas y Cuenta.
- Headers, filtros, formularios y acciones pasan a una columna.
- Tablas de Productos y Recetas se convierten visualmente en cards con etiquetas por campo.
- Listados administrativos se convierten en bloques apilados.
- El contenido reserva espacio para que la navegación fija no oculte acciones.

El shell tenant usa navegación inferior hasta 1279 px. Además, `hover: none` o `pointer: coarse` mantienen la experiencia de aplicación en tablets touch de mayor resolución —incluidas orientaciones horizontales de 1280 px o superiores— sin depender únicamente del ancho. El sidebar queda reservado para escritorio con puntero fino desde 1280 px. Los ajustes de contenido móvil se aplican a 760 px y existe refinamiento adicional en 390 px.

## Pantallas cubiertas

Las pantallas públicas `/login`, `/register`, `/forgot-password`, `/reset-password` y `/confirm-email` comparten layout cálido, card de acceso, marca, campos, estados y llamadas a la acción.

TENANT_OWNER cubre `/`, `/productos`, `/recetas`, `/configuracion` y `/cuenta`. Se preservan carga, búsqueda, altas, edición, estados, costos, ingredientes, historial y configuración. `/configuracion` deja de formar parte del menú principal y se accede desde la card Configuración de Mi cuenta.

`/cuenta` concentra información personal, acceso a preferencias, seguridad y cierre de sesión. El formulario de contraseña se muestra en un modal centrado en escritorio/tablet y como bottom sheet en móvil. Cancelar o completar el cambio limpia los campos sensibles. Logout reutiliza el flujo existente y ya no aparece en el header global.

PLATFORM_ADMIN cubre `/admin`, `/admin/tenants`, `/admin/tenants/{id}`, `/admin/users`, `/admin/users/{id}`, `/admin/sessions`, `/admin/audit` y `/admin/audit/{id}`. Dashboards, filtros, resultados, detalles, estados y confirmaciones usan el mismo lenguaje visual sin modificar consultas ni autorización.

## Accesibilidad

- Documento configurado como `es-MX`.
- Indicador de foco visible y consistente.
- Contraste reforzado para texto, acción primaria y estados.
- Controles principales con altura touch-friendly.
- Navegación semántica y regiones `role=status`/`role=alert` preservadas.
- Movimiento reducido con `prefers-reduced-motion`.
- Encabezados de tablas permanecen en el DOM; en móvil las cards muestran etiquetas equivalentes.

## Validación

- Build completo de la solución sin warnings ni errores.
- Revisión real en navegador de la experiencia pública en escritorio y 390 px.
- En 390 px no se detectó overflow horizontal en Login.
- Se verificaron card, campos, jerarquía, CTA y adaptación a una columna.
- Las áreas autenticadas se revisaron estructuralmente y mediante sus breakpoints CSS; requieren una pasada manual final con sesiones TENANT_OWNER y PLATFORM_ADMIN y datos reales.

## Fuera de alcance

- No se cambió lógica Application/Infrastructure.
- No se cambiaron contratos, endpoints ni modelos.
- No se agregaron migraciones.
- No se modificaron políticas de seguridad ni operación.

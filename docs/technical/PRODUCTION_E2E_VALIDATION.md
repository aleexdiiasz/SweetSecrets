# TEN-033 - Production End-to-End Validation

## Alcance y fecha

Validación ejecutada el 2 de septiembre de 2026 sobre `feature/TEN-033-production-e2e-validation`. Se usó Docker Compose con `ASPNETCORE_ENVIRONMENT=Production`, credenciales aleatorias efímeras no versionadas y volúmenes exclusivos `sweetsecrets-ten033-*`. No se usaron bases, volúmenes, SMTP ni secretos reales.

El tráfico siguió `cliente -> Nginx -> API -> PostgreSQL`. Solo Web quedó publicado para la aplicación. Nginx fijó `X-Forwarded-Proto: https` como simulación explícita de un terminador TLS confiable; no se debilitó `CookieSecurePolicy.Always`. Mailpit 1.31.0 quedó limitado a loopback y exigió STARTTLS con una CA/certificado de dos días generados para la prueba. Los archivos privados, `.env`, cookies, tokens, dumps y resultados están bajo `deploy/e2e/artifacts/`, ignorado por Git.

## Fixtures versionados

- `deploy/e2e/compose.validation.yml`: Mailpit STARTTLS y mounts de prueba.
- `deploy/e2e/compose.restore.yml`: PostgreSQL vacío para restore, sin alterar el Compose Production.
- `deploy/e2e/nginx.validation.conf`: terminación TLS simulada y proxy same-origin.
- `deploy/e2e/new-test-certificates.ps1`: CA, certificado SMTP y CRL efímeros.
- `deploy/e2e/install-test-ca.ps1`: instala la CA solo en un contenedor cuyo label Compose cumple `sweetsecrets-ten033-*`.

## Matriz

| ID | Escenario | Resultado | Evidencia / observación |
|---|---|---|---|
| E2E-001 | Configuración Production válida | PASS | El validador aceptó la configuración completa sin secretos versionados. |
| E2E-002 | Build de imágenes y arranque limpio | PASS | PostgreSQL, Mailpit, API y Web saludables; migrador one-shot terminó en cero. |
| E2E-003 | Health normal | PASS | `live` y `ready` devolvieron 200 y `{"status":"Healthy"}`. |
| E2E-004 | Bootstrap PLATFORM_ADMIN | PASS | Login 200 y dashboard admin 200. |
| E2E-005 | Registro y provisioning | PASS | Se crearon usuarios/tenants MASTER y bases tenant mediante el flujo público. |
| E2E-006 | Migrations | PASS | MASTER contiene 3 migrations; cada tenant restaurado contiene 2. No se agregó auto-migrate al startup normal. |
| E2E-007 | Seed tenant | PASS | Ambos tenants objetivo contienen unidades, productos iniciales y `MULTIPLIER=3`. |
| E2E-008 | Confirmación de correo | PASS | Antes de confirmar login 401; token Identity 200; después login 200 para A y B. |
| E2E-009 | SMTP Production-like | PASS | Entrega en Mailpit mediante STARTTLS validado, CA confiable y CRL, sin desactivar revocación. |
| E2E-010 | Dashboard owner | PASS | Métricas reales y diferenciadas por tenant. |
| E2E-011 | Productos | PASS | Listar, crear, editar precio, desactivar y reactivar. |
| E2E-012 | Costo unitario | PASS | Compra de 1000 g por $300 produjo $0.30/g. |
| E2E-013 | Recetas e ingredientes | PASS | Crear, agregar item, editar cantidad, editar multiplicador, desactivar/reactivar. |
| E2E-014 | Cálculo inicial | PASS | 100 g a $0.30/g produjo costo $30 y sugerido $90 con multiplicador 3. |
| E2E-015 | Multiplicador propio | PASS | Cambiar receta a 4 produjo sugerido $120; persistió independiente del setting. |
| E2E-016 | Recalculation e historial | PASS | Cantidad 200 g y cambio a $0.40/g produjo costo $80/sugerido $320 e historial >= 2. |
| E2E-017 | Setting MULTIPLIER | PASS | 3 -> 4.5; receta existente conservó 4/$320 y nueva receta usó 4.5. |
| E2E-018 | Aislamiento A/B | PASS | Productos, recetas, settings, historiales y dashboards no se cruzaron vía API. |
| E2E-019 | Cuenta/cambio de contraseña | PASS | Cuenta accesible; cambio aceptado, sesión actual continuó y password anterior fue rechazado. |
| E2E-020 | Password recovery | PASS | Forgot genérico, entrega local, reset Identity y login solo con password final. |
| E2E-021 | Suspender/reactivar tenant | PASS | Login suspendido 401; reactivación restauró acceso y conservó datos. |
| E2E-022 | Usuarios admin | PASS | Listado/detalle, block/unblock; login bloqueado 401 y luego restaurado. |
| E2E-023 | Sesiones | PASS | Listado y revocación; la cookie owner revocada recibió 401. |
| E2E-024 | Auditoría | PASS | Listado/detalle y eventos operacionales presentes sin payload secreto. |
| E2E-025 | Protección PLATFORM_ADMIN | PASS | No puede bloquearse; owner recibe 403 en API admin. |
| E2E-026 | Rate limit login | PASS | Intento 11 devolvió 429, `Retry-After` y mensaje estándar. |
| E2E-027 | Rate limit register | PASS | Intento 4 devolvió 429, `Retry-After` y mensaje estándar. |
| E2E-028 | Rate limit email | PASS | Forgot y resend devolvieron 429 en intento 6, de forma independiente. |
| E2E-029 | Rate limit tokens | PASS | Reset y confirm devolvieron 429 en intento 11, de forma independiente. |
| E2E-030 | Endpoints fuera del limitador | PASS | Health y dashboard admin continuaron 200 al agotar login. |
| E2E-031 | Forwarded headers / spoof | PASS | Cambiar `X-Forwarded-For` del cliente no creó una partición nueva detrás de Nginx. |
| E2E-032 | MASTER caído | PASS | PostgreSQL detenido: live 200; ready 503 con solo `{"status":"Unhealthy"}`. |
| E2E-033 | Recuperación health | PASS | Readiness regresó a 200 al iniciar PostgreSQL. |
| E2E-034 | Restart de servicios | PASS | MASTER, productos, recetas, settings y auditoría conservaron valores. |
| E2E-035 | Data Protection | PASS | Checksums estables y cookies owner/admin válidas después de restart/recreate. No se imprimieron keys. |
| E2E-036 | Backup completo | PASS | Manifest `Completed`, 5 tenant DB descubiertas desde MASTER y 7 archivos con SHA-256 verificados. |
| E2E-037 | Restore aislado | PASS | MASTER + 5 tenants + Data Protection restaurados en otro proyecto/volúmenes, sin overwrite. |
| E2E-038 | Aplicación contra restore | PASS | Health, cookies previas, login A/B/admin, tenant resolution, productos, recetas, settings y audit. |
| E2E-039 | SPA y rutas directas | PASS | Rutas públicas, owner y admin devolvieron shell 200 mediante fallback Nginx. |
| E2E-040 | PWA assets | PASS | Service worker, manifest e iconos 192/512 disponibles; se corrigió MIME del manifest. |
| E2E-041 | Smoke público en navegador | PASS | Login, register, forgot, reset y confirm cargaron con `main`, labels, controles y mensajes en español. |
| E2E-042 | Smoke autenticado responsive | PASS | Owner y admin pasaron 1366x768, 390x844, 768x1024 y 1180x820 sin overflow; sidebar solo desktop y navegación inferior en móvil/tablet. |
| E2E-043 | Cookies | PASS | `HttpOnly`, `Secure` y `SameSite=Lax`; password ausente del body. |
| E2E-044 | Logs Production | PASS | Tras el fix: cero SQL EF informativo, `SecurityStamp`, connection strings, stack traces o tokens completos. |
| E2E-045 | TLS/DNS/SMTP/backup reales | NOT TESTED | Prerrequisitos del deployment final; deliberadamente fuera de TEN-033 local. |

## Bugs encontrados y corregidos

1. El SMTP de prueba inicialmente falló la validación de revocación. Se generó una CRL firmada y un CDP accesible, manteniendo STARTTLS y la validación de certificados activa.
2. Production heredaba logging EF Core `Information`, por lo que registraba SQL parametrizado y nombres técnicos como `SecurityStamp`. `appsettings.Production.json` eleva `Microsoft.EntityFrameworkCore.Database.Command` a `Warning`; se agregó regresión y se verificó cero coincidencias después de recrear API.
3. Nginx entregaba `manifest.webmanifest` como `application/octet-stream`. La configuración base y la fixture ahora responden `application/manifest+json`; existe prueba de regresión.
4. Platform Admin conservaba sidebar lateral en tablets y las tablas owner mantenían `min-width: 900px` a 390 px por precedencia de CSS isolation. El design system aplica navegación inferior a ambos shells hasta 1279 px y fuerza `min-width: 0 !important` en las cards móviles. La repetición autenticada pasó en las cuatro resoluciones.

## Backup, restore y conservación

El backup se ejecutó con `deploy/scripts/backup.ps1`, se validó con `verify-backup.ps1` y se restauró con `restore.ps1`. El destino fue un segundo proyecto Compose con bases y volúmenes nuevos. Se restauraron las bases con sus nombres originales solo dentro del PostgreSQL destino vacío para poder arrancar la aplicación sin alterar metadata MASTER. Las claves se copiaron a un volumen Data Protection separado antes de iniciar API.

Los contenedores fuente se retiraron con `docker compose down` sin `-v`; sus volúmenes permanecen. No se borró ningún volumen histórico ni se restauró sobre el origen.

Al terminar el smoke se detuvo también el stack restore sin `-v` y se eliminó únicamente `deploy/e2e/artifacts/`. Con ello dejaron de existir en disco de trabajo los `.env`, passwords, cookies, tokens, certificados privados, dumps y perfiles de navegador efímeros; los volúmenes TEN-033 permanecen para no realizar borrados destructivos.

## Seguridad revisada

- El frontend no envió TenantId, DatabaseName ni connection strings para seleccionar base.
- Owner obtuvo 403 en API admin; bloqueos, suspensión y revocación produjeron 401 según política.
- Las respuestas health fueron mínimas; las respuestas auth no incluyeron passwords, hashes, stamps ni tokens internos.
- Las credenciales, cookies, tokens, certificados privados, key ring, dumps y `.env` quedaron ignorados.
- Nginx sobrescribe el proto en la fixture y agrega la IP del socket; la API confía solo en la red proxy configurada y un hop.

## Evaluación

Resultado TEN-033: **GO condicionado a prerrequisitos de infraestructura real** para preparar el deployment V1. No quedan fallos funcionales, operacionales, de restore ni responsive conocidos en el entorno Production-like. Esto no significa autorización para publicar todavía.

Prerrequisitos Production:

- DNS, dominio y terminación TLS/certificados reales;
- SMTP real y secretos en un secret manager;
- storage cifrado off-site para backups, retención/alertas y simulacro en infraestructura destino;
- firewall, monitoreo/alertas y política operacional;
- ejecutar nuevamente esta matriz contra el ambiente final sin reutilizar fixtures TEN-033.

## Validación técnica final

- `dotnet build .\SweetSecrets.slnx`: PASS, 0 warnings y 0 errores. El primer intento reprodujo `MSB4216/MSB4027`; después de `dotnet build-server shutdown` se ejecutó fuera del sandbox y pasó.
- `dotnet test .\SweetSecrets.slnx --no-build`: PASS final, 111 unit tests y 1 integration test.
- `docker compose -f deploy/compose.production.yml config --quiet`: PASS usando variables efímeras completas.
- builds Docker API, migrador y Web: PASS.
- `git diff --check`: PASS; solo avisos informativos de conversión LF/CRLF.
- secret review: PASS; cero asignaciones de password/token/cookie/key, PEM privado, `.env` o credenciales en el diff; `deploy/e2e/artifacts/` figura ignorado.

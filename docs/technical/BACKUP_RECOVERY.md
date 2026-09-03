# Backup & Recovery

## Ejercicio E2E TEN-033

Se ejecutó un backup completo con datos funcionales, se verificaron manifest y SHA-256, y se restauraron MASTER, cinco bases tenant y Data Protection en un segundo proyecto Compose aislado. La aplicación restaurada pasó health, continuidad de cookies, login nuevo de Tenant A, Tenant B y PLATFORM_ADMIN, tenant resolution, productos, recetas, settings y auditoría.

El restore no sobrescribió el origen. Los contenedores fuente se retiraron sin `-v` y sus volúmenes se conservaron. Los dumps, keys y manifests de ejecución permanecen en `deploy/e2e/artifacts/`, ignorado por Git. Evidencia: [PRODUCTION_E2E_VALIDATION.md](PRODUCTION_E2E_VALIDATION.md).

## Alcance y unidad de recuperación

TEN-032 define un respaldo reproducible para la arquitectura database-per-tenant. Un backup completo está formado por:

1. MASTER (`sweetsecrets_master`);
2. todas las bases cuyo `DatabaseName` aparece en `MASTER.tenants`;
3. el key ring persistente de ASP.NET Core Data Protection.

Separar cualquiera de estos elementos produce un backup parcial. MASTER contiene usuarios, roles, sesiones, auditoría y el registro que relaciona cada tenant con su base. Las bases tenant contienen productos, recetas, ingredientes, unidades, settings e historiales. Data Protection permite mantener continuidad criptográfica de cookies y tokens Identity.

## Herramientas

Los scripts se encuentran en `deploy/scripts` y requieren PowerShell 7, Docker y Docker Compose:

- `backup.ps1`: crea dumps, copia Data Protection y genera el manifest;
- `verify-backup.ps1`: valida estado, inventario, tamaños y SHA-256 sin restaurar;
- `restore.ps1`: restaura el conjunto completo o un tenant hacia bases nuevas;
- `Backup.Common.ps1`: validaciones compartidas.

Las operaciones PostgreSQL se ejecutan dentro del contenedor oficial PostgreSQL 18.6. No se publica el puerto 5432 ni se requiere `pg_dump` local. Cada dump usa formato custom `pg_dump -Fc`; restore utiliza `pg_restore --exit-on-error --no-owner --no-privileges`.

## Backup completo

Crear fuera del repositorio un directorio protegido o usar un directorio `backups/` ignorado durante una prueba local:

```powershell
pwsh ./deploy/scripts/backup.ps1 `
  -OutputRoot D:/SweetSecretsBackups `
  -ComposeFile ./deploy/compose.production.yml `
  -EnvFile ./deploy/.env
```

Por defecto el script resuelve los contenedores `postgres` y `api` del proyecto Compose. Si se usa `-ProjectName`, debe ser el mismo nombre con el que se levantó la pila. `DataProtectionSourcePath` permite una ruta local/montada explícita para pruebas u operación fuera del contenedor API.

El flujo:

1. valida Docker, nombres y servicios;
2. crea un directorio UTC que nunca sobrescribe;
3. respalda MASTER;
4. consulta `tenants."DatabaseName"` y `Status` desde MASTER;
5. incluye `Provisioning`, `Active`, `Suspended`, `Disabled` y `Failed` cuando tienen nombre válido;
6. exige que cada base exista y pueda respaldarse;
7. copia los archivos Data Protection sin imprimirlos;
8. calcula tamaños y SHA-256;
9. escribe `manifest.json` como `Completed` solo al finalizar todo.

Una base referenciada ausente, un nombre inválido, un dump fallido o un key ring vacío produce exit code distinto de cero y manifest `Failed`. No existe éxito parcial silencioso.

## Estructura y manifest

```text
2026-09-02T215656Z/
  manifest.json
  master/sweetsecrets_master.dump
  tenants/sweetsecrets_tenant_000001.dump
  tenants/sweetsecrets_tenant_000002.dump
  dataprotection/key-....xml
```

El manifest versión 1 contiene timestamp UTC, estado, versión `pg_dump`, nombre MASTER, versión lógica SweetSecrets, tenants y sus estados, número de tenants, rutas relativas, tamaños y SHA-256. Nunca contiene passwords, connection strings, credenciales SMTP, tokens ni contenido de las claves.

Validar antes de transferir o restaurar:

```powershell
pwsh ./deploy/scripts/verify-backup.ps1 -BackupPath D:/SweetSecretsBackups/2026-09-02T215656Z
```

La verificación rechaza manifest incompleto/fallido, path traversal, archivos agregados/faltantes/duplicados, tamaño o checksum distinto, conteo tenant inconsistente y dumps no declarados.

## Consistencia

`pg_dump` obtiene un snapshot consistente dentro de cada base. PostgreSQL no proporciona automáticamente una transacción distribuida consistente entre MASTER y todas las bases tenant. V1 acepta snapshots por base tomados en minutos/segundos cercanos.

Operacionalmente se recomienda anunciar ventana, evitar provisioning y cambios administrativos, pausar escrituras de aplicación o ponerla en mantenimiento, ejecutar el backup, verificarlo y reanudar. El script no inventa un bloqueo distribuido. Para RPO menor se debe evaluar PITR/WAL archiving en una iniciativa futura.

## Restore seguro

El script verifica el backup completo antes de crear una base. Nunca elimina ni sobrescribe bases. Sus defaults restauran a:

- `sweetsecrets_restore_master`;
- `sweetsecrets_restore_sweetsecrets_tenant_000001`, etcétera.

También exige una ruta vacía y aislada para Data Protection:

```powershell
pwsh ./deploy/scripts/restore.ps1 `
  -BackupPath D:/SweetSecretsBackups/2026-09-02T215656Z `
  -Mode Full `
  -DataProtectionTargetPath D:/SweetSecretsRestore/keys `
  -ComposeFile ./deploy/compose.production.yml `
  -EnvFile ./deploy/.env
```

Después de `pg_restore`, valida `__EFMigrationsHistory`. Para MASTER exige `tenants`, `platform_users`, `platform_roles`, `user_sessions` y `platform_audit_logs`. Para tenants exige `products`, `recipes`, `recipe_items`, `units`, `settings`, `product_price_history` y `recipe_cost_history`. Solo muestra nombres de componentes y conteo de migraciones; no imprime hashes, emails ni datos personales.

Si una etapa falla, las bases nuevas ya creadas pueden quedar parciales. No apuntar la aplicación a ellas; inspeccionarlas y eliminarlas únicamente mediante un procedimiento explícito autorizado.

## Recuperación real en clúster vacío

Orden obligatorio:

1. provisionar PostgreSQL compatible y verificarlo;
2. detener API/Web o mantenerlos sin conexión al nuevo clúster;
3. verificar manifest/checksums;
4. restaurar MASTER;
5. restaurar todos los tenants;
6. restaurar Data Protection en un volumen nuevo/vacío;
7. validar tablas, migraciones y conteos agregados;
8. configurar la API hacia el conjunto completo;
9. iniciar API/Web;
10. comprobar `/health/live` y `/health/ready`;
11. validar login, roles y resolución de al menos dos tenants.

En un clúster totalmente vacío se pueden solicitar nombres originales explícitamente:

```powershell
pwsh ./deploy/scripts/restore.ps1 `
  -BackupPath D:/SweetSecretsBackups/2026-09-02T215656Z `
  -Mode Full `
  -MasterTargetDatabase sweetsecrets_master `
  -TenantTargetPrefix '' `
  -DataProtectionTargetPath D:/SweetSecretsRestore/keys
```

El comando seguirá fallando si algún destino ya existe. No se ofrece `--force`, drop ni replace. Copiar las claves restauradas al volumen final requiere que el volumen sea nuevo/vacío y una acción operacional explícita; nunca mezclar dos key rings. Proteger permisos antes de iniciar API.

## Restore de un tenant

```powershell
pwsh ./deploy/scripts/restore.ps1 `
  -BackupPath D:/SweetSecretsBackups/2026-09-02T215656Z `
  -Mode Tenant `
  -TenantDatabase sweetsecrets_tenant_000001
```

El default crea `sweetsecrets_restore_sweetsecrets_tenant_000001`. Para recuperar una pérdida real, verificar primero que el nombre original no existe y usar `-TenantTargetPrefix ''`. Restaurar solo un tenant a un punto anterior mientras MASTER permanece actual puede producir diferencia temporal en usuarios, estados o auditoría. El script no modifica MASTER para compensarla. La activación/cambio de nombres requiere análisis y ventana de mantenimiento.

## Escenarios de desastre

### A. Pérdida de una tenant DB

Mantener MASTER y otras bases sin cambios. Detener acceso del tenant afectado, verificar el backup, restaurar su dump al nombre aislado, validar datos/migrations y recién entonces planear el cambio al nombre original. Impacto: solo ese tenant; existe riesgo de desfase temporal con MASTER.

### B. Pérdida de MASTER

Detener toda la aplicación. Restaurar MASTER y validar tenants, usuarios, roles, sesiones, auditoría e historial EF. Confirmar que cada `DatabaseName` registrado existe y corresponde al mismo punto coordinado. Conservar/restaurar el key ring asociado. Sin MASTER no debe arrancarse operación tenant normal.

### C. Pérdida completa

Crear infraestructura vacía; restaurar MASTER, todas las bases tenant y Data Protection en ese orden; reconfigurar secretos externos; validar schema, health, autenticación y aislamiento tenant antes de abrir tráfico. Perder Data Protection puede invalidar cookies y tokens vigentes aunque las bases se recuperen.

## Retención, RPO y RTO

Política inicial recomendada, todavía manual:

- 7 diarios;
- 4 semanales;
- 12 mensuales.

TEN-032 no borra backups automáticamente. Cualquier cleanup futuro debe empezar con dry-run, respetar hold legal/operacional y exigir confirmación sobre rutas exactas.

RPO inicial objetivo: hasta 24 horas con backup diario. No es garantía; cambios entre backups pueden perderse. RTO: depende del tamaño, cantidad de tenants, ancho de banda y validaciones. Debe medirse periódicamente. En la fixture local pequeña de TEN-032, backup completo tomó aproximadamente 3.92 s y restore completo 9.09 s; no extrapolar estos tiempos a Production.

## Almacenamiento y seguridad

Los dumps contienen emails, hashes Identity, auditoría y datos operativos. El key ring es material criptográfico. Requisitos:

- mínimo acceso y cuentas separadas;
- cifrado en tránsito y reposo;
- copia local operacional más copia externa/off-site;
- versionado/inmutabilidad cuando el destino lo permita;
- monitoreo de acceso, retención y eliminación controlada;
- nunca Git, correo, chat ni almacenamiento público;
- nunca usar backups Production en desarrollo sin sanitización aprobada;
- no guardar el único backup en el servidor PostgreSQL.

TEN-032 no selecciona S3, Azure Blob, GCS ni KMS. Tampoco configura cron real. Automatización futura debe ejecutar backup, verify, transferencia cifrada y alerta por exit code sin registrar secretos.

## Compatibilidad y troubleshooting

Los dumps se probaron con PostgreSQL 18.6. Restaurar en otra major version requiere ensayo previo con `pg_restore` compatible; no asumir compatibilidad futura.

- Servicio no encontrado: comprobar `docker compose ... ps` y `-ProjectName`.
- Tenant ausente: resolver la discrepancia MASTER/base; no quitarlo del manifest para forzar éxito.
- Checksum inválido: aislar el artefacto y recuperar otra copia; no restaurar.
- Destino existente: elegir un nombre aislado o un clúster vacío; el script no sobrescribe.
- Data Protection vacío: verificar el volumen/ruta persistente y detener el backup como incompleto.
- Restore parcial: no iniciar la aplicación; conservar logs no sensibles y repetir en destinos nuevos.

## Validación TEN-032

En PostgreSQL 18.6 aislado se crearon MASTER y dos bases tenant con datos reconocibles. Se comprobó:

- descubrimiento de un tenant `Active` y uno `Suspended`;
- backup custom de las tres bases y Data Protection;
- manifest `Completed` y SHA-256;
- restore MASTER y ambos tenants a nombres aislados;
- presencia de migrations/tablas y conteos agregados;
- coincidencia de producto, receta, `MULTIPLIER` e historiales en restore selectivo;
- checksum idéntico de Data Protection restaurado;
- rechazo de destino existente;
- detección de dump alterado y archivo faltante;
- manifest `Failed` y exit 1 cuando MASTER referencia una base tenant ausente.

Las bases y claves históricas del desarrollador no se usaron ni modificaron.

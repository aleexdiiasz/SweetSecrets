# Tenant Settings

## 1. Objetivo

Documentar el módulo operacional de configuración tenant implementado en:

```text
TEN-011 - Tenant Settings
```

El módulo permite consultar y modificar configuraciones pertenecientes exclusivamente al tenant autenticado.

---

## 2. Arquitectura multi-tenant

La configuración operacional pertenece a la base PostgreSQL de cada tenant.

Flujo:

```text
HTTP Request
↓
usuario autenticado
↓
CurrentTenantResolver
↓
MASTER
↓
Tenant Active
↓
ITenantDbContextFactory
↓
TenantDbContext
↓
settings del tenant
```

El frontend nunca selecciona directamente:

```text
TenantId
DatabaseName
ConnectionString
```

---

## 3. Entidad existente

El módulo utiliza la entidad:

```text
TenantSetting
```

Campos:

```text
Id
Key
Value
Description
CreatedAt
UpdatedAt
```

Tabla PostgreSQL:

```text
settings
```

---

## 4. Restricciones de persistencia

`Key`:

```text
máximo 100 caracteres
obligatoria
índice UNIQUE
```

`Value`:

```text
máximo 1000 caracteres
obligatorio
```

`Description`:

```text
máximo 500 caracteres
opcional
```

---

## 5. Configuración inicial

El seed tenant incluye:

```text
Key   = MULTIPLIER
Value = 3
```

Descripción:

```text
Multiplicador predeterminado para nuevas recetas.
```

---

## 6. Servicios Application

Consultas:

```text
ISettingQueryService
```

Operaciones:

```text
ISettingCommandService
```

Modelos internos:

```text
SettingListItem
SettingDetail
UpdateSettingCommand
```

---

## 7. Infrastructure

Implementaciones:

```text
SettingQueryService
SettingCommandService
```

Ambos utilizan:

```text
ITenantDbContextFactory
```

Por lo tanto, no reciben `TenantId` ni información de conexión desde HTTP.

---

## 8. Consultas

`SettingQueryService` utiliza:

```text
AsNoTracking
```

para operaciones de lectura.

Las configuraciones se ordenan por:

```text
Key
```

---

## 9. Endpoints

TEN-011 agrega:

```text
GET /api/settings
GET /api/settings/{key}
PUT /api/settings/{key}
```

---

## 10. GET /api/settings

Devuelve todas las configuraciones del tenant autenticado.

Respuesta:

```text
Key
Value
Description
CreatedAt
UpdatedAt
```

Prueba funcional:

```text
GET /api/settings
→ 200 OK
```

Resultado validado:

```text
MULTIPLIER = 3
```

---

## 11. GET /api/settings/{key}

Obtiene una configuración específica.

La clave recibida se normaliza mediante:

```text
Trim()
ToUpperInvariant()
```

Por lo tanto:

```text
/api/settings/multiplier
```

puede resolver:

```text
MULTIPLIER
```

Prueba funcional:

```text
GET /api/settings/multiplier
→ 200 OK
```

---

## 12. Clave inexistente

Si la configuración no existe:

```text
GET /api/settings/NO_EXISTE
→ 404 Not Found
```

El endpoint no crea registros automáticamente.

---

## 13. PUT /api/settings/{key}

Permite actualizar una configuración existente.

Ejemplo:

```text
PUT /api/settings/multiplier
```

Body:

```json
{
  "value": "4.5"
}
```

Resultado validado:

```text
200 OK
MULTIPLIER = 4.5
UpdatedAt != null
```

---

## 14. PUT no crea configuraciones

TEN-011 no implementa creación arbitraria de claves mediante `PUT`.

Prueba:

```text
PUT /api/settings/NO_EXISTE
```

Resultado:

```text
404 Not Found
```

Posteriormente:

```text
GET /api/settings
```

confirmó que:

```text
NO_EXISTE
```

no fue creado.

---

## 15. Normalización de Key

Las claves de actualización se normalizan:

```text
Trim()
ToUpperInvariant()
```

Ejemplo:

```text
multiplier
```

se procesa como:

```text
MULTIPLIER
```

---

## 16. Validación genérica

Toda actualización valida:

```text
Key obligatoria
Key <= 100 caracteres
Value obligatorio
Value <= 1000 caracteres
```

---

## 17. Validación específica de MULTIPLIER

`TenantSetting.Value` permanece como `string` porque el sistema debe soportar diferentes tipos de configuración.

La validación específica se determina por `Key`.

Para:

```text
MULTIPLIER
```

se exige:

```text
decimal válido
valor > 0
```

---

## 18. Cultura decimal

`MULTIPLIER` utiliza:

```text
CultureInfo.InvariantCulture
```

y:

```text
NumberStyles.AllowDecimalPoint
```

Formato permitido:

```text
4.5
```

Formato rechazado:

```text
4,5
```

Esto evita depender de la configuración regional del servidor.

---

## 19. Bug detectado durante pruebas

Durante las pruebas iniciales se utilizó:

```text
NumberStyles.Number
```

con `InvariantCulture`.

Esto provocó:

```text
"4,5"
↓
45
```

porque la coma era interpretada como separador de miles.

El problema fue detectado antes del cierre de TEN-011.

La implementación fue corregida para utilizar:

```text
NumberStyles.AllowDecimalPoint
```

Resultado final:

```text
"4.5" → 200 OK
"4,5" → 400 Bad Request
```

---

## 20. Valor cero

Prueba:

```json
{
  "value": "0"
}
```

Resultado:

```text
400 Bad Request
```

Mensaje:

```text
MULTIPLIER debe ser un número mayor que cero.
```

---

## 21. Atomicidad

Una actualización inválida no altera el valor existente.

Prueba:

```text
MULTIPLIER = 4.5
↓
PUT value = 0
↓
400 Bad Request
↓
GET MULTIPLIER
↓
4.5
```

El valor anterior permaneció intacto.

---

## 22. UpdatedAt

Cuando una configuración se actualiza correctamente:

```text
UpdatedAt = DateTime.UtcNow
```

`CreatedAt` permanece sin modificación.

---

## 23. Autorización

Lectura y modificación operacional actual:

```text
TENANT_OWNER ✅
TENANT_USER  ❌
```

`SettingsController` utiliza:

```text
[Authorize(Roles = PlatformRoles.TenantOwner)]
```

---

## 24. PLATFORM_ADMIN

`PLATFORM_ADMIN` no forma parte de los roles autorizados del `SettingsController`.

Por diseño:

```text
PLATFORM_ADMIN
TenantId = null
```

y no obtiene contexto tenant mediante el resolver operacional normal.

La administración global continúa separada bajo endpoints administrativos de plataforma.

---

## 25. TENANT_USER

El rol:

```text
TENANT_USER
```

existe en `PlatformRoles`.

Sin embargo, actualmente no existe un flujo funcional para crear y administrar usuarios adicionales `TENANT_USER`.

El rol permanece pausado para la aplicación operacional. A partir de TEN-017, el controlador completo de configuración está restringido a:

```text
TENANT_OWNER
```

No se implementan lectura, modificación ni UI de configuración para `TENANT_USER` hasta que un issue futuro reactive expresamente ese rol.

---

## 26. Aislamiento database-per-tenant

La prueba funcional se realizó utilizando:

```text
sweetsecrets_tenant_000003
sweetsecrets_tenant_000004
```

Antes de la modificación:

```text
000003 → MULTIPLIER = 3
000004 → MULTIPLIER = 3
```

Después de modificar desde la API autenticada como tenant `000004`:

```text
000003 → MULTIPLIER = 3
000004 → MULTIPLIER = 4.5
```

Esto confirma que:

```text
settings
```

permanece físicamente aislado por tenant.

---

## 27. Restauración de datos de prueba

Después de completar las pruebas:

```text
sweetsecrets_tenant_000004
MULTIPLIER
```

fue restaurado a:

```text
3
```

---

## 28. MULTIPLIER y Recipe.Multiplier

Existen dos datos diferenciados:

```text
settings.MULTIPLIER
Recipe.Multiplier
```

Regla oficial a partir de TEN-017:

```text
settings.MULTIPLIER
→ valor predeterminado al abrir una receta nueva
→ se copia al CreateRecipeRequest

CreateRecipeRequest.Multiplier
→ Recipe.Multiplier persistido
```

Cada receta conserva su multiplicador propio después de guardarse. El usuario puede modificarlo posteriormente desde la edición de la receta.

Actualmente:

```text
CreateRecipeRequest.Multiplier
↓
CreateRecipeCommand.Multiplier
↓
Recipe.Multiplier
```

Cambiar:

```text
settings.MULTIPLIER
```

no modifica ni recalcula recetas existentes.

---

## 29. Decisión de alcance

TEN-011 implementa la administración operacional de:

```text
settings
```

TEN-017 define y aplica `settings.MULTIPLIER` exclusivamente como valor inicial de nuevas recetas en la Web. El backend de recetas sigue recibiendo y persistiendo `CreateRecipeRequest.Multiplier`; no consulta settings durante la creación ni altera recetas existentes.

---

## 30. Migraciones

TEN-011 no requiere una nueva migración PostgreSQL.

La tabla:

```text
settings
```

y su estructura ya existían desde:

```text
TEN001_InitialTenant
```

TEN-011 agrega únicamente la capa operacional:

```text
Application
Infrastructure
Contracts
API
Authorization
Validation
```

---

## 31. Pruebas funcionales realizadas

Validado:

```text
GET /api/settings                       → 200
GET /api/settings/multiplier            → 200
PUT /api/settings/multiplier = 4.5      → 200
PUT /api/settings/multiplier = 0        → 400
GET posterior después de error          → valor intacto
GET /api/settings/NO_EXISTE             → 404
PUT /api/settings/NO_EXISTE             → 404
clave inexistente no creada             → confirmado
"4,5"                                   → 400
"4.5"                                   → 200
aislamiento tenant 000003 / 000004      → confirmado
restauración MULTIPLIER = 3             → confirmado
```

---

## 32. Swagger

Los códigos:

```text
400
404
```

pueden aparecer en Swagger como:

```text
Undocumented
```

porque todavía no se agregó metadata explícita de OpenAPI para esas respuestas.

Esto no representa un fallo funcional de los endpoints.

---

## 33. Estado TEN-011

Implementado:

```text
✅ Query Settings
✅ Detail Setting
✅ Update Setting
✅ normalización de Key
✅ validaciones genéricas
✅ validación específica MULTIPLIER
✅ decimal invariant
✅ protección ante coma decimal
✅ UpdatedAt
✅ autorización TENANT_OWNER
✅ autorización operacional TENANT_OWNER
✅ aislamiento database-per-tenant
✅ no creación arbitraria mediante PUT
✅ pruebas funcionales
```

Pendiente fuera de TEN-011:

```text
administración funcional de TENANT_USER
```

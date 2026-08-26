# Tenant Resolution

## 1. Objetivo

Documentar cómo SweetSecrets determina de forma segura qué base de datos tenant debe utilizar para cada usuario autenticado.

La resolución del tenant se realiza exclusivamente en el backend.

El frontend nunca decide ni proporciona:

- TenantId para seleccionar contexto
- DatabaseName
- cadena de conexión

---

## 2. Flujo de resolución

```text
Usuario autenticado
        ↓
Claim NameIdentifier
        ↓
MASTER.platform_users
        ↓
Validar usuario
        ↓
Obtener TenantId
        ↓
MASTER.tenants
        ↓
Validar Status = Active
        ↓
Obtener DatabaseName
        ↓
CurrentTenantDbContextFactory
        ↓
TenantDbContext
        ↓
Base PostgreSQL exclusiva del tenant
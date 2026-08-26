# Tenant Self Registration

## 1. Objetivo

Documentar el proceso de autoregistro de nuevas cuentas SweetSecrets.

El objetivo es permitir que una repostera pueda crear su propia cuenta sin intervención manual del PLATFORM_ADMIN.

---

## 2. Flujo

```text
Nombre del negocio
Nombre del propietario
Correo
Contraseña
        ↓
POST /api/auth/register
        ↓
SelfRegistrationService
        ↓
Validar correo
        ↓
Provisionar tenant
        ↓
Crear base PostgreSQL
        ↓
Aplicar migraciones
        ↓
Ejecutar seed inicial
        ↓
Crear usuario TENANT_OWNER
        ↓
Cuenta lista
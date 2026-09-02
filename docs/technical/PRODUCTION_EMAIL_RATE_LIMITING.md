# Production Email Delivery & Rate Limiting

## Arquitectura de email

TEN-029 conserva `ITransactionalEmailSender` y `TransactionalEmailMessage` en Application. Infrastructure implementa dos destinos:

- Development: `DevelopmentTransactionalEmailSender` escribe en `%TEMP%/SweetSecrets/email`.
- ambientes no Development: `SmtpTransactionalEmailSender` construye un sobre explícito y `MailKitSmtpTransport` realiza la entrega SMTP estándar.

Application no conoce SMTP, MailKit ni proveedores comerciales. `UnconfiguredTransactionalEmailSender` ya no se registra: Production no puede arrancar silenciosamente con entrega inutilizable.

## Configuración SMTP

La sección tipada `Email:Smtp` contiene `Host`, `Port`, `UseSsl`, `Username`, `Password`, `FromEmail` y `FromName`. `UseSsl=true` usa SSL al conectar; `false` exige STARTTLS. Usuario y password son opcionales para servidores sin autenticación, pero deben configurarse juntos cuando se use autenticación.

Production valida antes del startup: host, puerto 1-65535, remitente válido, nombre del remitente y par usuario/password. Los errores nombran únicamente claves de configuración y nunca imprimen valores secretos.

Variables de entorno equivalentes:

```text
Email__Smtp__Host
Email__Smtp__Port
Email__Smtp__UseSsl
Email__Smtp__Username
Email__Smtp__Password
Email__Smtp__FromEmail
Email__Smtp__FromName
```

No hay hosts, cuentas ni passwords reales en Git. Production debe obtenerlos de variables, secret store o el mecanismo del deployment.

## Seguridad y logging

Los tokens Identity, enlaces, cuerpos, destinatarios y credenciales no se registran. El sender registra sólo categoría y resultado. Un fallo SMTP se transforma en un error controlado sin excepción interna, credenciales ni cuerpo; los flujos anti-enumeración continúan devolviendo su respuesta genérica.

## Rate limiting público

Se usa la infraestructura oficial de ASP.NET Core con ventanas fijas, cola cero y partición por `HttpContext.Connection.RemoteIpAddress`:

| Política | Endpoints | Límite predeterminado |
|---|---|---|
| Login | `POST /api/auth/login` | 10 cada 5 minutos |
| Register | `POST /api/auth/register` | 3 cada 60 minutos |
| EmailDelivery | `forgot-password`, `resend-confirmation` | 5 cada 15 minutos |
| TokenValidation | `reset-password`, `confirm-email` | 10 cada 15 minutos |

Los valores se configuran en `RateLimiting:PublicAuth` mediante `PermitLimit` y `WindowMinutes`. Ejemplo de override: `RateLimiting__PublicAuth__Login__PermitLimit`.

No existe limiter global: endpoints administrativos autenticados y health checks no reciben estas políticas. Health continúa como middleware terminal antes de rate limiting, autenticación y acceso a MASTER.

## HTTP 429 y anti-enumeración

Al exceder el límite se responde `429 Too Many Requests` con JSON mínimo y, cuando está disponible, `Retry-After`:

```json
{"message":"Has realizado demasiados intentos. Intenta nuevamente en unos minutos."}
```

La respuesta no contiene email, existencia de cuenta ni estado de confirmación. `AuthApiClient` reconoce 429 de forma centralizada para Login, Register, Forgot Password, Reset Password, Confirm Email y reenvío, mostrando el mismo texto español. Los controles existentes siguen evitando doble submit.

## Dirección IP y reverse proxy

TEN-029 usa exclusivamente `RemoteIpAddress`; no confía en `X-Forwarded-For` arbitrario. TEN-030 debe configurar `ForwardedHeaders` y redes/proxies conocidos según el deployment antes de depender de la IP original detrás del reverse proxy. No se habilita una política insegura de confianza global.

## Pruebas y limitaciones

Las pruebas cubren selección Development/Production, validación SMTP, configuración incompleta, ausencia de secretos en errores, construcción del mensaje, fallo sanitizado, asignación de políticas, admisión/rechazo, respuesta 429 y exclusión de endpoints administrativos/health.

No se conectan servidores SMTP reales en pruebas. Quedan pendientes una prueba Production-like con una cuenta SMTP de prueba, ajuste operacional de límites, proxies confiables en deployment y evaluación futura de CAPTCHA si el abuso de registro lo requiere.

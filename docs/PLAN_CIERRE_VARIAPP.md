# Plan de cierre técnico de VariApp / VariStorehn

Rama de trabajo: `agent/mejoras-variapp`

## Fase 0 — Protección operativa y línea base — COMPLETA

- Respaldo de Aiven confirmado.
- Variables productivas verificadas.
- `main` protegida y Pull Request en borrador.
- CI limitado a checkpoints controlados.

## Fase 1 — Seguridad, permisos y alcance por usuario — IMPLEMENTADA Y CERTIFICADA

- Ventas, compras, facturas, finanzas y movimientos aislados por `UsuarioId` para usuarios no administradores.
- Acceso global reservado al administrador.
- Acciones ocultas y endpoints protegidos por permiso exacto.
- Auditoría restringida al administrador.
- Productos inactivos y eliminaciones lógicas protegidos.
- Aceptación Administrador, Vendedor y rol personalizado aprobada en entorno aislado.

## Fase 2 — Cálculos, impuestos y compras — IMPLEMENTADA Y CERTIFICADA

- Impuestos incluidos y adicionales corregidos.
- Importe bruto, descuento, subtotal neto, impuesto y total reconciliados.
- Impuestos de compra administrables.
- Documentos de respaldo de compras incorporados.
- Pruebas monetarias y migración aditiva aprobadas.

## Fase 3 — Facturación y comunicaciones — IMPLEMENTADA Y CERTIFICADA EN AISLAMIENTO

- PDF único para descarga, impresión, WhatsApp y correo.
- Identidad visual y logo con respaldo local.
- SMTP reforzado y errores sanitizados.
- Enlaces públicos con hash, expiración, revocación y límite de accesos.
- PDF privado, PDF público, encabezados de seguridad y revocación aprobados en E2E.
- Pendiente externo: Gmail real, WhatsApp real y revisión visual del PDF en Preview.

## Fase 4 — Usuarios y perfil — IMPLEMENTADA Y CERTIFICADA

- Edición administrativa con permisos independientes.
- Autogestión de nombre, usuario, contraseña y fotografía.
- Nombres de usuario únicos.
- Contraseñas seguras.
- Fotografías de perfil mediante Cloudinary.
- Roles dinámicos de sistema creados incrementalmente.
- Administrador inicial vinculado al rol dinámico.
- Matrices administradas preservadas entre reinicios.

## Fase 5 — Responsividad, experiencia y ortografía — IMPLEMENTADA Y CERTIFICADA EN NAVEGADOR

- Login, layout, menú, formularios, tablas, Dashboard, finanzas, auditoría, factura y permisos adaptados.
- Navegación y guardas verificadas.
- Login sin desbordamiento horizontal a 320, 375, 390, 430, 768 y 1440 px.
- Pendiente externo: revisión visual en dispositivos físicos.

## Fase 6 — Migraciones, compilación, pruebas y documentación — COMPLETA Y CERTIFICADA

- Migración EF Core aditiva.
- Modelo y snapshot alineados.
- SQL forward revisable sin operaciones destructivas.
- Backend Release y frontend producción aprobados.
- Documentación técnica actualizada.

## Fase 7A — Validación integral aislada — COMPLETA Y CERTIFICADA

- MySQL 8.4 temporal.
- Todas las migraciones aplicadas en una base descartable.
- API ASP.NET Core y Angular efímeros.
- 69 pruebas backend aprobadas.
- 9 pruebas E2E aprobadas.
- Administrador, Vendedor y rol personalizado validados.
- Perfil, contraseña, venta, factura, PDF, enlace público y revocación validados.
- Evidencia: `docs/FASE7_CERTIFICACION_AISLADA.md`.

## Fase 7B — Validación externa y productiva — BLOQUEADA / PENDIENTE

- Obtener Preview de Vercel. El estado actual está bloqueado por `build-rate-limit`.
- Crear backend Preview de Render con base no productiva.
- Validar Gmail SMTP real.
- Validar WhatsApp desde teléfono real.
- Validar Cloudinary real para perfil y comprobantes.
- Revisar visualmente teléfono, tablet y escritorio.
- Revisar respaldo y SQL forward.
- Aplicar la migración en Aiven mediante una sola estrategia autorizada.
- Verificar `__EFMigrationsHistory` y conservación de datos.
- Verificar Render y Vercel productivos.
- Obtener autorización expresa antes de fusionar a `main`.

## Regla de datos

No se eliminarán registros productivos. El Pull Request permanecerá en borrador y la migración no se aplicará hasta completar la Fase 7B y recibir autorización expresa del propietario.

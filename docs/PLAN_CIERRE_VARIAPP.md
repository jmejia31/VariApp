# Plan de cierre técnico de VariApp / VariStorehn

Rama de trabajo: `agent/mejoras-variapp`  
Pull Request: `#1` hacia `main`

## Fase 0 — Protección operativa y línea base — COMPLETA

- Respaldo de Aiven confirmado.
- Variables productivas verificadas.
- `main` protegida y Pull Request en borrador.
- CI limitado a checkpoints controlados.

## Fase 1 — Seguridad, permisos y alcance por usuario — IMPLEMENTADA Y CERTIFICADA AUTOMÁTICAMENTE

- Ventas, compras, facturas, finanzas y movimientos aislados por `UsuarioId` para usuarios no administradores.
- Acceso global reservado al administrador.
- Acciones protegidas por permiso exacto en frontend y backend.
- Auditoría restringida al administrador.
- Ruta y permiso de movimientos de inventario unificados.
- Eliminación lógica y productos inactivos protegidos.
- Pendiente: aceptación funcional con perfiles reales en Fase 7.

## Fase 2 — Cálculos, impuestos y compras — IMPLEMENTADA Y CERTIFICADA AUTOMÁTICAMENTE

- Impuestos incluidos y adicionales separados.
- Importe bruto, descuento, subtotal neto, impuesto y total reconciliados.
- Impuestos de compra administrables desde la interfaz.
- Documentos de respaldo asociados a compras.
- Snapshots fiscales históricos.
- Pruebas aprobadas para impuesto incluido, adicional, descuento antes del impuesto y compra con impuesto incluido.
- Pendiente: validación productiva con operaciones controladas en Fase 7.

## Fase 3 — Facturación y comunicaciones — IMPLEMENTADA Y COMPILADA

- PDF único para descarga, impresión, WhatsApp y correo.
- Identidad visual y logo incorporados al generador.
- SMTP reforzado y errores sanitizados.
- Enlaces públicos con hash, expiración, revocación y límite de accesos.
- Pruebas aprobadas para hash, vencimiento, límite y revocación.
- Pendiente: entrega real Gmail, WhatsApp y comprobación visual del PDF en Fase 7.

## Fase 4 — Usuarios y perfil — IMPLEMENTADA Y CERTIFICADA AUTOMÁTICAMENTE

- Edición administrativa con permisos separados para datos, rol y contraseña.
- Autogestión de nombre completo y nombre de usuario.
- Nombres de usuario únicos sin distinguir mayúsculas y minúsculas.
- Contraseñas seguras sin secretos en auditoría.
- Fotografías de perfil mediante Cloudinary.
- Identidad sincronizada con la sesión y barra superior.
- Pruebas aprobadas para alcance por usuario, unicidad, normalización y contraseña.
- Pendiente: validación real de carga de foto en Fase 7.

## Fase 5 — Responsividad, experiencia y ortografía — IMPLEMENTADA Y COMPILADA

- Inicio de sesión adaptado a escritorio, tablet y teléfono.
- Redirección al primer módulo autorizado.
- Protección contra desbordamientos horizontales.
- Menú lateral móvil accesible.
- Formularios, tablas, factura, permisos, Dashboard, finanzas, auditoría y movimientos adaptados.
- Diálogos, mensajes, accesibilidad y terminología unificados.
- Build de producción Angular aprobado.
- Pendiente: certificación visual en dispositivos representativos durante la Fase 7.

## Fase 6 — Migraciones, compilación, pruebas y documentación — COMPLETA Y CERTIFICADA

- Migración EF Core aditiva creada y guardada.
- Método `Up()` sin operaciones destructivas.
- Modelo EF y snapshot alineados.
- SQL forward generado desde la migración inmediatamente anterior.
- SQL sin `DROP`, `TRUNCATE` ni `DELETE FROM`.
- Backend compilado en Release.
- Frontend compilado en producción.
- **66 pruebas ejecutadas y 66 aprobadas**.
- README, validación productiva y evidencia técnica actualizados.
- Evidencia detallada: `docs/FASE6_CERTIFICACION.md`.
- No se aplicó ninguna migración a Aiven.

## Fase 7 — Validación productiva y cierre — SIGUIENTE

- Preparar Preview autorizado sin afectar producción.
- Validar Administrador, Vendedor y rol personalizado.
- Validar Gmail, WhatsApp, PDF y enlaces públicos.
- Validar Cloudinary para perfiles y documentos de compra.
- Revisar responsividad en resoluciones representativas.
- Aplicar la migración en Aiven mediante una única estrategia autorizada.
- Verificar Render, Vercel y datos existentes.
- Presentar el Pull Request para aprobación.
- Fusionar a `main` únicamente con autorización expresa del propietario.

## Regla de datos

Todos los cambios de base de datos son aditivos y revisables. No se eliminarán registros productivos ni se ejecutarán migraciones destructivas.

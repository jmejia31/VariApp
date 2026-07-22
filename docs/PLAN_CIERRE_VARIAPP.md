# Plan de cierre técnico de VariApp / VariStorehn

Rama de trabajo: `agent/mejoras-variapp`

## Fase 1 — Seguridad, permisos y alcance por usuario
- Aislar ventas, compras, facturas, finanzas y movimientos por `UsuarioId` para usuarios no administradores.
- Mantener acceso global exclusivamente para administradores.
- Ocultar acciones sin permiso exacto.
- Restringir auditoría a administradores.
- Unificar el permiso y la ruta de movimientos de inventario.

## Fase 2 — Cálculos, impuestos y compras
- Corregir impuestos incluidos y adicionales.
- Reconciliar importe bruto, descuento, subtotal neto, impuesto y total.
- Permitir impuestos de compra administrables desde la interfaz.
- Agregar documentos de respaldo de compras sin alterar datos históricos.

## Fase 3 — Facturación y comunicaciones
- Unificar el PDF usado por descarga, impresión, WhatsApp y correo.
- Garantizar identidad visual y logo.
- Fortalecer SMTP y diagnóstico de errores.
- Proteger enlaces públicos de factura.

## Fase 4 — Usuarios, perfil y responsividad
- Agregar edición de usuarios con permiso `Usuarios:Editar`.
- Agregar autogestión de nombre, usuario, contraseña y foto de perfil.
- Corregir desbordamientos y validar móvil/tablet/escritorio.

## Fase 5 — Calidad y entrega
- Ejecutar compilación y pruebas automatizadas.
- Agregar pruebas de permisos, propiedad de datos, impuestos y facturas.
- Actualizar documentación.
- Realizar validación productiva antes de fusionar a `main`.

## Regla de datos
Todos los cambios de base de datos serán aditivos y revisables. No se eliminarán registros productivos ni se ejecutarán migraciones destructivas.

# Plan de cierre técnico de VariApp / VariStorehn

Rama de trabajo: `agent/mejoras-variapp`

## Fase 0 — Protección operativa y línea base
- Respaldo de Aiven confirmado.
- Variables productivas verificadas.
- `main` protegida y Pull Request en borrador.
- CI limitado a checkpoints controlados.

## Fase 1 — Seguridad, permisos y alcance por usuario
- Aislar ventas, compras, facturas, finanzas y movimientos por `UsuarioId` para usuarios no administradores.
- Mantener acceso global exclusivamente para administradores.
- Ocultar acciones sin permiso exacto.
- Restringir auditoría a administradores.
- Unificar el permiso y la ruta de movimientos de inventario.
- Estado: implementada; certificación integral pendiente en Fase 6.

## Fase 2 — Cálculos, impuestos y compras
- Corregir impuestos incluidos y adicionales.
- Reconciliar importe bruto, descuento, subtotal neto, impuesto y total.
- Permitir impuestos de compra administrables desde la interfaz.
- Agregar documentos de respaldo de compras sin alterar datos históricos.
- Estado: implementada; migraciones y pruebas monetarias pendientes en Fase 6.

## Fase 3 — Facturación y comunicaciones
- Unificar el PDF usado por descarga, impresión, WhatsApp y correo.
- Garantizar identidad visual y logo.
- Fortalecer SMTP y diagnóstico de errores.
- Proteger enlaces públicos de factura mediante hash, expiración, revocación y límite de accesos.
- Estado: implementada; prueba real Gmail/WhatsApp pendiente en Fase 7.

## Fase 4 — Usuarios y perfil
- Editar usuarios con permisos independientes para datos, roles y contraseñas.
- Permitir autogestión de nombre completo y nombre de usuario.
- Validar nombres de usuario únicos sin distinguir mayúsculas y minúsculas.
- Aplicar contraseñas seguras y excluir secretos de auditoría.
- Cargar, reemplazar y eliminar fotos de perfil mediante Cloudinary en carpeta dedicada.
- Sincronizar nombre, usuario y fotografía con la sesión y la barra superior.
- Mantener diseño responsivo para teléfono, tablet y escritorio.
- Estado: implementada; columnas de fotografía y pruebas automatizadas pendientes en Fase 6.

## Fase 5 — Responsividad, experiencia y ortografía
- Rediseñar el inicio de sesión para escritorio, tablet y teléfono, eliminando el ancho excesivamente reducido.
- Redirigir después del acceso al primer módulo realmente autorizado o al perfil del usuario.
- Incorporar una capa global contra desbordamientos horizontales, con foco visible, objetivos táctiles adecuados y soporte para reducción de movimiento.
- Convertir el menú lateral en un panel móvil accesible, con bloqueo de desplazamiento, cierre mediante Escape y navegación por teclado.
- Adaptar formularios de ventas y compras, filas de productos, totales y acciones para pantallas pequeñas.
- Mejorar la matriz de permisos con filtros claros, estados vacíos y barra de guardado persistente.
- Adaptar la vista de factura y los paneles de WhatsApp, correo e historial sin modificar el PDF oficial.
- Optimizar Dashboard, finanzas, auditoría y movimientos de inventario para móvil y tablet.
- Homogeneizar diálogos de confirmación, mensajes, colores del tema y terminología.
- Incorporar semántica accesible en tablas, gráficos, menús, formularios y controles expandibles.
- Corregir textos y términos visibles, incluyendo acentos, capitalización y abreviaturas ambiguas.
- Estado: implementada; compilación y certificación visual automatizada pendientes en Fase 6.

## Fase 6 — Migraciones, compilación, pruebas y documentación
- Crear migraciones únicamente aditivas.
- Compilar backend y frontend.
- Ejecutar pruebas unitarias, integración, autorización horizontal, cálculos fiscales, enlaces, SMTP y PDF.
- Ejecutar pruebas de interfaz y responsividad en resoluciones representativas.
- Actualizar README y documentación operativa.

## Fase 7 — Validación productiva y cierre
- Ejecutar CI final controlado.
- Desplegar Preview autorizado.
- Validar Administrador, Vendedor y rol personalizado.
- Validar Gmail, WhatsApp, PDF, Cloudinary y CRUD completo.
- Presentar el Pull Request para aprobación y fusionar solamente con autorización del propietario.

## Regla de datos
Todos los cambios de base de datos serán aditivos y revisables. No se eliminarán registros productivos ni se ejecutarán migraciones destructivas.
# Plan de cierre técnico de VariApp / VariStorehn

Rama de trabajo: `Desarrollo`

PR colaborativo: `Desarrollo -> main`, permanentemente en borrador hasta autorización expresa de Javier Mejía.

## Fase 0 — Protección operativa y línea base — COMPLETA

- `main` permanece como rama productiva estable.
- `Desarrollo` es la única rama de cambios y validación.
- El PR hacia `main` está en borrador y no tiene auto-merge.
- Producción no se modifica durante la certificación.
- CI, Docker y configuraciones de entornos están versionados.

## Fase 1 — Seguridad, permisos y alcance por usuario — IMPLEMENTADA Y CERTIFICADA EN AISLAMIENTO

- Ventas, compras, facturas, finanzas y movimientos aislados por `UsuarioId` para usuarios no administradores.
- Acceso global reservado al administrador.
- Acciones ocultas y endpoints protegidos por permiso exacto.
- Auditoría restringida al administrador.
- Productos inactivos y eliminaciones lógicas protegidos.
- Aceptación de Administrador, Vendedor y rol personalizado incorporada al workflow integral.

## Fase 2 — Cálculos, impuestos y compras — IMPLEMENTADA Y CERTIFICADA EN AISLAMIENTO

- Impuestos incluidos y adicionales corregidos.
- Importe bruto, descuento, subtotal neto, impuesto y total reconciliados.
- Impuestos de compra administrables.
- Documentos de respaldo de compras incorporados.
- Pruebas monetarias y migraciones aditivas incluidas en CI.

## Fase 3 — Facturación y comunicaciones — IMPLEMENTADA; VALIDACIÓN EXTERNA PENDIENTE

- PDF único para descarga, impresión, WhatsApp y correo.
- Identidad visual y logo con respaldo local.
- SMTP reforzado y errores sanitizados.
- Enlaces públicos con hash, expiración, revocación y límite de accesos.
- PDF privado, PDF público, encabezados de seguridad y revocación cubiertos por E2E aislado.
- Pendiente externo: Gmail real, WhatsApp real y revisión visual del PDF en Preview.

## Fase 4 — Usuarios y perfil — IMPLEMENTADA Y CERTIFICADA EN AISLAMIENTO

- Edición administrativa con permisos independientes.
- Autogestión de nombre, usuario, contraseña y fotografía.
- Nombres de usuario únicos y contraseñas seguras.
- Fotografías de perfil mediante Cloudinary.
- Roles dinámicos de sistema creados incrementalmente.
- Administrador inicial vinculado al rol dinámico.
- Matrices administradas preservadas entre reinicios.

## Fase 5 — Responsividad, experiencia, colores e iconos — IMPLEMENTADA; VALIDACIÓN FÍSICA PENDIENTE

- Login, layout, menú, formularios, tablas, Dashboard, finanzas, auditoría, factura y permisos adaptados.
- Navegación y guardas verificadas.
- Paletas clara y oscura incluidas en auditoría automatizada representativa.
- Contraste de diálogos, acciones destructivas, perfil e iconos reforzado.
- Matriz automatizada de navegación, consola y desbordamiento horizontal incorporada.
- Pendiente externo: revisión visual en teléfonos y tabletas físicos.

## Fase 6 — Migraciones, compilación, pruebas y calidad — IMPLEMENTADA

- Migración EF Core para catálogos y eliminación lógica.
- Modelo, Designer y snapshot alineados.
- SQL forward revisable sin eliminación de datos.
- Backend Release y frontend producción incluidos en CI.
- Lint reproducible, verificación TypeScript y control de trazas temporales incorporados.
- MySQL 8.4 descartable valida migración y conversión de Marca/Modelo legados.

## Fase 7 — Colores, Tallas, Marcas y Modelos — IMPLEMENTADA

- Catálogos dinámicos almacenados en base de datos.
- CRUD, búsqueda, activación, desactivación y eliminación lógica.
- Marca relacionada de forma normalizada con Modelo.
- Permisos y auditoría independientes.
- Componente y servicio frontend reutilizables.
- Productos vinculados a Marca, Modelo, Color y Talla.
- Compras y Ventas muestran las variantes del Producto seleccionado.
- Catálogos inactivos conservan el historial y no se ofrecen para nuevas selecciones.

## Fase 8 — Productos, Categorías, sesión y Finanzas — IMPLEMENTADA

- Productos muestran `Agotado` cuando la existencia es cero.
- Dashboard usa el mismo criterio de agotado.
- Productos permiten filtrar por Marca, Modelo, Color, Talla, Categoría, estado y existencia.
- Categorías usan eliminación lógica real y dejan de aparecer después de eliminar y recargar.
- La sesión se cierra únicamente por 30 minutos continuos de inactividad.
- Actividad de mouse, teclado, clic, entrada, scroll, navegación y otras pestañas reinicia el contador.
- El token se renueva mientras existe actividad sin perder formularios en curso.
- Finanzas muestra costo, valor de venta, utilidad bruta, margen, utilidad potencial y utilidad neta estimada.

## Fase 9A — Aceptación integral aislada — EN CERTIFICACIÓN CONTINUA

El workflow `Desarrollo - aceptación funcional integral` ejecuta en MySQL 8.4 descartable:

- catálogos y relación Marca–Modelo;
- filtros normalizados de Productos;
- eliminación de Categorías;
- sesión activa, inactiva, varias pestañas, renovación y pérdida temporal de red;
- navegación administrativa y errores de consola;
- responsive de pantallas principales;
- temas claro y oscuro representativos;
- Administrador, Vendedor y rol personalizado;
- permisos y aislamiento por usuario;
- perfil, venta, factura, PDF y enlace público.

Un commit solo se considera certificado cuando tanto `Desarrollo - Compilación y pruebas` como `Desarrollo - aceptación funcional integral` terminan en verde.

## Fase 9B — Validación externa de Desarrollo — PENDIENTE

- Crear Aiven Desarrollo independiente.
- Configurar Cloudinary Desarrollo.
- Crear Render Desarrollo con secretos no productivos.
- Aplicar migraciones únicamente en Aiven Desarrollo.
- Crear Vercel Desarrollo.
- Validar Gmail SMTP real.
- Validar WhatsApp desde teléfono real.
- Validar Cloudinary real para productos, perfil y comprobantes.
- Revisar visualmente PDF, teléfono, tablet y escritorio.
- Confirmar `__EFMigrationsHistory`, conteos y conservación de datos.

El procedimiento y responsables están en:

- `docs/ENTORNOS_DESARROLLO_PRODUCCION.md`
- `docs/RESPONSABILIDADES_CIERRE_DESARROLLO.md`
- `docs/CLOUDINARY_AISLAMIENTO.md`

## Fase 10 — Preparación productiva — BLOQUEADA

No comienza hasta completar Fase 9B.

- revisar respaldo productivo verificable;
- aprobar SQL forward y estrategia única de migración;
- definir ventana, responsables y rollback;
- obtener autorización expresa de Javier Mejía;
- fusionar y desplegar únicamente bajo ese plan.

## Regla de datos

No se eliminarán registros ni activos productivos. El PR permanecerá en borrador y ninguna migración se aplicará en Aiven productivo hasta completar las validaciones externas y recibir autorización expresa del propietario.

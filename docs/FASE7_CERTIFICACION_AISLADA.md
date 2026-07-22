# Fase 7 — Certificación integral aislada de VariApp

Fecha: 22 de julio de 2026  
Rama: `agent/mejoras-variapp`  
Pull Request: `#1`  
Commit certificado: `57b609ddd3549dffb6b99c0b5106e22d2946def6`  
Workflow: `VariApp CI`  
Run ID: `29938871994`

## Resultado

La validación automatizada e integral de la Fase 7 quedó **aprobada** en un entorno completamente aislado. No se utilizó Aiven, Render, Vercel, Gmail ni Cloudinary productivos. No se fusionó el Pull Request y `main` no fue modificada.

## Evidencia consolidada

- Backend Release: aprobado.
- Frontend Angular producción: aprobado.
- Migración EF Core y snapshot: alineados.
- SQL forward: revisado sin operaciones destructivas.
- Pruebas backend: **69 ejecutadas, 69 aprobadas, 0 fallidas**.
- Pruebas end-to-end: **9 ejecutadas, 9 aprobadas, 0 fallidas, 0 omitidas**.
- Base utilizada: MySQL 8.4 temporal y descartable.
- Navegador utilizado: Chromium mediante Playwright.

## Hallazgo y corrección durante la Fase 7

La primera ejecución detectó que una instalación nueva no creaba los registros dinámicos de los roles de sistema `Administrador` y `Vendedor`; únicamente existía el enum legado.

Se corrigió el arranque para:

- crear los roles dinámicos de sistema cuando estén ausentes;
- conservar roles existentes;
- vincular usuarios legacy sin `RolId`;
- vincular permisos legacy con `RolId` y `PermisoId`;
- no sobrescribir matrices administradas;
- no reconstruir una matriz deliberadamente vacía;
- crear el administrador inicial antes del seeding dinámico;
- vincular inmediatamente el administrador inicial con el rol dinámico `Administrador`.

La corrección quedó protegida por tres pruebas backend y una prueba E2E adicional.

## Pruebas backend

Las 69 pruebas aprobadas cubren, entre otros puntos:

- creación de roles de sistema en una instalación limpia;
- vinculación de usuarios y permisos legacy;
- conservación de matrices administradas entre reinicios;
- permisos administrativos y rechazo a usuarios comunes;
- aislamiento financiero;
- compras, ventas, confirmación y anulación;
- inventario y stock;
- eliminación lógica y conservación de imágenes;
- impuestos incluidos y adicionales;
- descuentos antes del impuesto;
- enlaces públicos con hash SHA-256;
- vencimiento, límite de accesos y revocación;
- perfil limitado al `UsuarioId` autenticado;
- unicidad del nombre de usuario;
- política y cambio de contraseña.

## Pruebas end-to-end

Las nueve pruebas E2E aprobaron los siguientes escenarios:

1. El administrador inicial posee un `RolId` dinámico válido y corresponde al rol `Administrador`.
2. El administrador conserva acceso a usuarios, auditoría y módulos corporativos.
3. Un rol personalizado recibe únicamente `Dashboard:Ver` y `Productos:Ver`.
4. El backend devuelve `403 Forbidden` para usuarios sin permisos sobre usuarios, auditoría y compras.
5. El rol `Vendedor` no hereda acceso administrativo.
6. El perfil solo modifica al usuario autenticado, rechaza contraseña débil y aplica el cambio de contraseña.
7. Una venta confirmada genera factura y PDF privado.
8. El enlace público entrega el mismo tipo de PDF, aplica encabezados de seguridad y deja de funcionar al revocarse.
9. El login no presenta desbordamiento horizontal a 320, 375, 390, 430, 768 y 1440 píxeles; las guardas frontend redirigen módulos prohibidos y permiten Productos.

## Infraestructura del checkpoint

El workflow:

- crea un contenedor MySQL 8.4 temporal;
- aplica todas las migraciones EF Core al iniciar la API;
- crea credenciales exclusivamente temporales;
- inicia ASP.NET Core en `127.0.0.1:5005`;
- inicia Angular en `127.0.0.1:4200`;
- instala Chromium en el runner efímero;
- ejecuta Playwright;
- conserva reporte HTML, JUnit, logs de API y logs de Angular como artefacto;
- destruye el contenedor al finalizar.

## Estado del Preview externo

GitHub registró un estado de Vercel fallido para el commit certificado. El destino del estado indica `upgradeToPro=build-rate-limit`, por lo que el Preview no fue generado debido al límite de compilaciones del plan de Vercel, no por un error detectado en el build de Angular.

No se registró un Preview de Render para esta rama.

## Pendiente externo y productivo

La certificación aislada no reemplaza estas comprobaciones reales:

- obtener un Preview de Vercel cuando el límite de builds lo permita;
- desplegar un backend Preview de Render conectado a una base no productiva;
- enviar una factura real mediante Gmail SMTP;
- comprobar el PDF recibido por correo;
- abrir el enlace de WhatsApp desde un teléfono real;
- cargar, reemplazar y eliminar una foto de perfil en Cloudinary real;
- cargar y descargar un comprobante de compra en Cloudinary real;
- revisar visualmente teléfono, tablet y escritorio físicos;
- revisar nuevamente el respaldo de Aiven;
- aplicar la migración productiva mediante una sola estrategia;
- verificar `__EFMigrationsHistory` y los datos después de la migración;
- validar Render y Vercel productivos;
- recibir autorización expresa del propietario antes de fusionar a `main`.

## Decisión de seguridad

El Pull Request debe permanecer en borrador. La migración no debe aplicarse en Aiven y el PR no debe fusionarse hasta completar la validación externa y productiva.

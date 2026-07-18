# Cierre de fases — Prompt de 6 partes

## Fase 1 — Usuarios al estándar de Roles

**Estado: parcialmente terminada.**

### Objetivo alcanzado
Usuarios ahora expone las mismas operaciones estructurales que Roles:
detalle real, búsqueda, bloqueo/desbloqueo, eliminación lógica protegida.

### Funcionalidades completadas
- `GET /usuarios/{id}` — vista de detalle real, separada del formulario de
  edición (antes no existía; se reutilizaba implícitamente la fila de tabla).
- `GET /usuarios/paginado` — búsqueda + paginación (antes solo `GetAll` sin
  filtros).
- `PUT /usuarios/{id}/bloquear` y `/desbloquear` — nuevo concepto, distinto
  de Activar/Desactivar, con motivo obligatorio y auditoría.
- `DELETE /usuarios/{id}` — eliminación lógica (antes no existía ninguna
  eliminación).
- Protecciones de seguridad (sección 7 del prompt, verificadas con código
  real, no supuestas):
  - No puedes bloquearte ni eliminarte a ti mismo.
  - No se puede bloquear/desactivar/eliminar al último administrador activo.
  - Un admin no puede quitarse a sí mismo el rol de administrador si es el
    único activo.
  - Login rechaza explícitamente usuarios bloqueados/eliminados/inactivos
    con mensajes distintos (a diferencia de credenciales inválidas, que
    sigue siendo mensaje genérico por seguridad).
- Frontend: listado con buscador, columna de estado (bloqueado/activo/
  inactivo), acciones por fila (ver, bloquear/desbloquear, eliminar) con
  confirmación, vista de detalle real (`UsuarioDetailComponent`), oculta
  las acciones de auto-bloqueo/auto-eliminación sobre el propio usuario.

### Funcionalidades corregidas
- `AuthService.LoginAsync` antes solo comprobaba `Activo`; ahora también
  `Bloqueado` y `Eliminado`, con mensajes diferenciados.

### Funcionalidades pendientes (de la Fase 1 misma)
- **Formulario de edición de usuario separado** (actualmente Update existe
  en backend y en el servicio Angular, pero el frontend solo tiene el
  formulario de creación inline — editar nombre/rol de un usuario existente
  no tiene UI todavía). Pendiente real, no oculto.
- Asignación de **varios roles simultáneos** (la Parte 2 del prompt lo
  menciona como opcional "si la arquitectura lo permite") — la arquitectura
  actual es un rol único por usuario (`Usuario.RolId`), cambiar esto sería
  una reestructuración de base de datos mayor, no se hizo.
- Consulta de "permisos efectivos" del usuario desde su propia vista de
  detalle (el dato existe vía `/permisos/mis-permisos` pero no está
  enlazado visualmente en `UsuarioDetailComponent`).

### Elementos que todavía necesitan revisión
- No se auditó si Clientes/Proveedores/Productos ya cumplen la "regla CRUD
  global" (sección 9 del prompt) — eso es la Fase 2+ pendiente.

### Elementos que todavía necesitan pruebas
- Todo lo de esta fase: sin pruebas automatizadas (ninguna fase de todo el
  proyecto las tiene todavía).
- No se pudo ejecutar `dotnet build` (bloqueo de red a nuget.org, igual que
  en toda la sesión anterior). Frontend sí verificado: `npx ng build
  --configuration=development` → 0 errores.

### Archivos creados
- `backend/src/Application/DTOs/PerfilDto.cs` *(nota: este archivo es de
  trabajo previo a este prompt, no de la Fase 1 — ver commits anteriores)*
- `frontend/src/app/features/usuarios/usuario-detail.component.{ts,html,scss}`
- `docs/migraciones/004_fase1_usuarios_bloqueo_eliminacion.sql`
- `docs/seguimiento/00-INICIO.md`, `01-PLAN-FASES.md`, `02-CIERRE-FASES.md` (este archivo)

### Archivos modificados
- `backend/src/Domain/Entities/Usuario.cs` — campos de bloqueo/eliminación/trazabilidad.
- `backend/src/Infrastructure/Persistence/Configurations/UsuarioConfiguration.cs`
- `backend/src/Application/Services/AuthService.cs` — validación de bloqueo/eliminación en login.
- `backend/src/Application/Interfaces/IUsuarioRepository.cs` + `Infrastructure/Repositories/UsuarioRepository.cs`
- `backend/src/Application/Interfaces/IUsuarioService.cs` + `Application/Services/UsuarioService.cs`
- `backend/src/Application/DTOs/UsuarioDto.cs` — `UsuarioDetalleDto`, `BloquearUsuarioDto`.
- `backend/src/API/Controllers/UsuariosController.cs` — 4 endpoints nuevos.
- `backend/src/Application/Common/CatalogoPermisosBase.cs` — acciones `CambiarEstado`/`EliminarLogico` en Usuarios.
- `frontend/src/app/core/models/usuario.model.ts`, `services/usuario.service.ts`
- `frontend/src/app/features/usuarios/usuarios.component.{ts,html,scss}`
- `frontend/src/app/app.routes.ts` — ruta `/usuarios/:id`.

### Archivos eliminados
Ninguno.

### Endpoints creados
`GET /usuarios/paginado`, `GET /usuarios/{id}`, `PUT /usuarios/{id}/bloquear`,
`PUT /usuarios/{id}/desbloquear`, `DELETE /usuarios/{id}`.

### Endpoints modificados
Ninguno con cambio de contrato — todo aditivo.

### Cambios en base de datos
10 columnas nuevas en `Usuarios` (ver script `004_...sql`). Aditivo, sin
pérdida de datos, todas con default seguro.

### Migraciones o scripts generados
`docs/migraciones/004_fase1_usuarios_bloqueo_eliminacion.sql` — SQL manual,
**no** es una migración EF real (sigue bloqueado `dotnet ef` por falta de
acceso a NuGet).

### Dependencias agregadas
Ninguna.

### Variables de entorno requeridas
Ninguna nueva en esta fase.

### Decisiones técnicas tomadas
- Bloqueo y Desactivación se modelan como conceptos distintos (campo
  `Bloqueado` separado de `Activo`) porque tienen semántica distinta: uno es
  administrativo reversible normal, el otro es una restricción de seguridad
  con motivo obligatorio — así lo pide explícitamente la sección 4.
- Se reutilizó `AccionPermiso.CambiarEstado` para bloqueo/desbloqueo (en vez
  de crear un nuevo valor de enum) para no ampliar más el catálogo de
  acciones sin necesidad real.

### Riesgos detectados
- Un admin bloqueado por otro admin pierde acceso inmediato en su próximo
  login, pero su token JWT ya emitido sigue siendo válido hasta que expire
  (no hay invalidación activa de tokens — limitación arquitectónica
  preexistente del proyecto, no introducida en esta fase).

### Riesgos resueltos
- Ya no es posible eliminar/bloquear/desactivar accidentalmente al último
  administrador del sistema (antes esta protección solo existía para Roles,
  no para Usuarios individuales).

### Riesgos abiertos
- Ver "Funcionalidades pendientes" arriba: falta el formulario de edición
  de usuario en el frontend.

### Comandos que debo ejecutar
Ver `03-COMANDOS-INTEGRACION.md`.

---

## Estado general del plan (8 fases totales)

| Fase | Nombre | Estado |
|---|---|---|
| 1 | Usuarios al estándar de Roles | Parcialmente terminada |
| 2 | Auditoría de seguridad Roles/Permisos | Terminada |
| 3 | Descarga de imágenes de producto | Terminada |
| 4 | PDF real de facturas | Parcialmente terminada (sin verificar build) |
| 5 | WhatsApp | Terminada (build de backend sin verificar) |
| 6 | Correo | Terminada (build de backend sin verificar) |
| 7 | Configuración visual / colores | No iniciada |
| 8 | Pruebas formales finales | No iniciada |

---

## Fase 2 — Auditoría de seguridad de Roles/Permisos

**Estado: terminada.**

### Objetivo alcanzado
Se verificó contra el código real (no supuesto) cada protección de la
sección 7 del prompt sobre lo ya construido en la sesión anterior de este
proyecto, y se corrigió el único hueco real encontrado.

### Hallazgo y corrección
- **Hueco real cerrado:** `RolService.EliminarPermanenteAsync` validaba rol
  de sistema, usuarios asociados y permisos asociados, pero **no** validaba
  que no fuera el último rol de tipo administrador — a diferencia de
  `DesactivarAsync` y `EliminarLogicoAsync`, que sí lo hacían. Si un rol
  `EsAdministrador=true` no tenía usuarios asignados en ese momento (por
  ejemplo, tras migrar usuarios a otro rol admin), podía eliminarse
  permanentemente aunque fuera el único rol capaz de tener administradores,
  dejando al sistema sin ninguna vía de crear un nuevo administrador sin
  tocar la base de datos directamente. Se agregó `ContarRolesAdministradorAsync`
  (cuenta roles, no usuarios) y el mismo chequeo que ya tenían las otras dos
  operaciones destructivas.

### Verificado y confirmado correcto (sin cambios necesarios)
- `EsAdministrador` es inmutable tras la creación de un rol — `UpdateAsync`
  nunca lo toca, solo `CreateAsync`. Cierra por completo cualquier vector de
  "un admin se autodegrada accidentalmente editando su propio rol".
- Roles de sistema protegidos contra renombrar (`UpdateAsync`), eliminar
  lógico y eliminar permanente.
- Último administrador protegido en Desactivar, EliminarLógico y (ahora)
  EliminarPermanente — a nivel de rol.
- Último administrador protegido a nivel de usuario individual (fase 1):
  no puedes bloquear/desactivar/eliminar al último usuario admin activo, ni
  quitarte a ti mismo el rol de admin si eres el único.
- Cada endpoint sensible (Roles, Permisos, Usuarios) exige `[RequierePermiso]`
  con la acción exacta — no hay ninguno protegido solo con `[Authorize]`
  genérico en estos tres controladores.
- **Los cambios de permisos a un rol existente se reflejan inmediatamente**,
  sin necesidad de que el usuario vuelva a iniciar sesión: `PermisoService.
  TienePermisoAsync` consulta la tabla `RolPermisos` en cada request, no
  cachea ni depende del JWT para los permisos en sí.

### Riesgo real confirmado, no resuelto en esta fase
- **Cambiar el ROL asignado a un usuario sí requiere que ese usuario vuelva
  a iniciar sesión** para que su JWT refleje el nuevo `RolId` — el token ya
  emitido sigue siendo válido con el rol anterior hasta que expira o hasta
  el próximo login. No existe invalidación activa de tokens (blacklist) en
  este proyecto; implementarla es una funcionalidad nueva de infraestructura
  (requeriría almacén de tokens revocados, ej. Redis o tabla dedicada), no
  una corrección de bug — se documenta como riesgo abierto, no se construye
  en esta fase de verificación.

### Elementos que necesitan pruebas
- El caso específico del hueco corregido (intentar eliminar permanentemente
  el último rol administrador sin usuarios asignados) — sin pruebas
  automatizadas, como el resto del proyecto.

### Archivos modificados
- `backend/src/Application/Services/RolService.cs`
- `backend/src/Application/Interfaces/IRolRepository.cs`
- `backend/src/Infrastructure/Repositories/RolRepository.cs`

### Cambios en base de datos
Ninguno (no se agregaron columnas, solo una consulta nueva).

### Riesgos abiertos
Invalidación de tokens JWT al cambiar rol/bloquear (ver arriba) — es una
limitación arquitectónica preexistente en todo el proyecto, no introducida
ni corregida en esta fase.

---

## Fase 3 — Descarga de imágenes de producto

**Estado: terminada.**

### Objetivo alcanzado
Cada producto ahora tiene una vista de detalle real (que no existía) con
galería de imágenes, vista ampliada (lightbox) y descarga individual/masiva
real desde archivos almacenados en Cloudinary — nunca simulada.

### Funcionalidades completadas
- `IImageStorageService.DownloadAsync`: streaming server-side desde la URL
  de Cloudinary en vez de redirigir al cliente directamente — la
  autorización real la controla el backend (`Productos:Exportar`), no la
  "confidencialidad" de una URL pública.
- `GET /productos/{id}/imagenes/{imagenId}/descargar` — descarga individual
  con `Content-Disposition` y nombre de archivo amigable.
- `GET /productos/{id}/imagenes/descargar-todas` — ZIP en memoria
  (`System.IO.Compression`, sin dependencias nuevas); omite en silencio una
  imagen puntual no disponible en vez de abortar la descarga completa.
- Control de acceso horizontal real: la imagen se busca dentro de las
  imágenes del producto indicado — un `imagenId` de otro producto da 404.
- Manejo de archivo inexistente: `DownloadAsync` devuelve `null` si
  Cloudinary no responde 200; el controlador lo traduce a 404 claro.
- Frontend: `ProductoDetailComponent` nuevo (galería, lightbox, descarga
  con estado de carga por imagen), botón "Ver" agregado al listado (antes
  solo existían Editar/Eliminar).

### Limitación real, no oculta
El nombre de archivo sugerido por el navegador en la descarga individual
usa `.jpg` por defecto en el cliente; el backend sí detecta la extensión
real del `Content-Type`, pero el navegador puede priorizar el atributo
`download` del enlace. Detalle cosmético del nombre, no afecta el
contenido descargado.

### Elementos que necesitan pruebas
- Descarga con imagen cuyo `PublicId` ya no existe en Cloudinary (confirmar
  404 en vez de error genérico).
- ZIP con productos de 10+ imágenes (tiempo de respuesta real).

### Archivos creados
`frontend/src/app/features/productos/producto-detail.component.{ts,html,scss}`

### Archivos modificados
`backend/src/Application/Interfaces/IImageStorageService.cs`,
`backend/src/Infrastructure/Services/CloudinaryImageStorageService.cs`,
`backend/src/API/Controllers/ProductosController.cs`,
`frontend/src/app/services/producto.service.ts`, `app.routes.ts`,
`frontend/src/app/features/productos/productos-list.component.html`.

### Endpoints creados
`GET /productos/{id}/imagenes/{imagenId}/descargar`,
`GET /productos/{id}/imagenes/descargar-todas`.

### Cambios en base de datos
Ninguno. Dependencias agregadas: ninguna.

### Build real
`npx ng build --configuration=development` → OK, 0 errores. Backend: no
verificable (nuget.org bloqueado).
---

## Fase 4 — Generación real de PDF de facturas

**Estado: parcialmente terminada** (implementada completa, pero sin poder
verificar la compilación del backend — ver más abajo).

### Objetivo alcanzado
El PDF de factura ya no es una vista HTML con `@media print` como
sustituto final: se genera un archivo PDF real en el backend con QuestPDF
(licencia Community), descargable desde un endpoint dedicado.

### Funcionalidades completadas
- `IFacturaPdfService`/`QuestPdfFacturaService`: documento con encabezado
  (logo si existe, datos de empresa, número de factura, fecha), datos de
  cliente, datos de la operación (método/estado de pago, vendedor, quién
  generó la factura), tabla de detalle (producto/marca/modelo/cantidad/
  precio/subtotal), totales (subtotal/descuento/impuesto/total),
  observaciones, y un bloque distintivo en rojo si la factura está
  anulada (motivo, fecha, quién la anuló). Pie de página con numeración.
- `FacturaDto.EmpresaLogoUrl`: se resuelve en vivo desde la configuración
  de empresa (decisión documentada en el código: el logo es identidad
  visual vigente, no un dato legal que deba quedar congelado como snapshot
  histórico, a diferencia de nombre/RTN/teléfono que sí son snapshot).
- `GET /facturas/{id}/pdf` — protegido con `Facturacion:Exportar`.
- Si el logo no se puede descargar, la factura se genera igual sin él en
  vez de fallar por completo (sección 12: "define qué ocurre si el
  registro existe pero el archivo no").
- Frontend: botón "Descargar PDF" real (antes solo existía "Imprimir" vía
  `window.print()`, que se conserva como opción secundaria).

### LIMITACIÓN CRÍTICA, no oculta: no se pudo verificar la compilación
QuestPDF es una dependencia nueva. La agregué al `.csproj` de
Infrastructure, pero **no pude ejecutar `dotnet restore`/`dotnet build`**
en este sandbox (bloqueo de red a nuget.org, la misma limitación de toda
la sesión). Esto es más riesgoso que fases anteriores porque:
- Es la primera vez que se agrega una librería externa nueva sin poder
  compilar contra ella ni una sola vez.
- Encontré y corregí un error real durante la propia escritura (no un
  build): `.Bold(bool)` no existe en la API de QuestPDF — es un método
  sin parámetros. Lo reescribí con `TextStyle` condicional. Si cometí
  algún otro error de la API (nombres de métodos, orden de extensiones),
  **no lo sabré hasta que compiles tú**.
- Recomendación explícita: ejecuta `dotnet build` en tu máquina antes de
  desplegar esta fase. Si falla, pégame el log completo y lo corrijo
  sobre el error real, no sobre otra suposición.

### Elementos que necesitan pruebas
- Compilación real del backend (ver arriba, es lo primero a hacer).
- PDF de una factura con logo configurado vs. sin logo.
- PDF de una factura anulada (verificar el bloque rojo).
- PDF con muchos productos (verificar paginación automática de la tabla).

### Archivos creados
`backend/src/Application/Interfaces/IFacturaPdfService.cs`,
`backend/src/Infrastructure/Services/QuestPdfFacturaService.cs`.

### Archivos modificados
`backend/src/Infrastructure/InventoryApp.Infrastructure.csproj` (+QuestPDF),
`backend/src/Application/DTOs/FacturaDto.cs`,
`backend/src/Application/Services/FacturaService.cs`,
`backend/src/API/Controllers/FacturasController.cs`, `API/Program.cs`,
`frontend/src/app/core/models/factura.model.ts`,
`frontend/src/app/services/factura.service.ts`,
`frontend/src/app/features/facturas/factura-view.component.{ts,html}`.

### Dependencias agregadas
`QuestPDF` 2024.3.6 (NuGet) — licencia Community, gratuita para
organizaciones pequeñas según sus términos (revisar
https://questpdf.com/license antes de producción si la empresa crece).

### Endpoints creados
`GET /facturas/{id}/pdf`.

### Build real
Frontend: `npx ng build --configuration=development` → OK, 0 errores.
Backend: **no verificable** — ver limitación crítica arriba.
---

## Fase 5 — WhatsApp (envío manual asistido, sin API oficial)

**Estado: terminada** (con la misma limitación de compilación de backend
que arrastra todo el proyecto — ver abajo).

### Objetivo alcanzado
WhatsApp es ahora la opción PRINCIPAL de envío desde la vista de factura
(sección 14, punto explícito del prompt), con el flujo realista que el
propio documento permite cuando no hay integración oficial: enlace público
temporal al PDF + mensaje prellenado + apertura de wa.me.

### Decisión técnica clave: enlace público temporal, no simulación
El PDF vive detrás de autenticación JWT normalmente. WhatsApp/el
destinatario no tiene sesión en el sistema, así que no puede abrir
`GET /facturas/{id}/pdf` directo. Se creó `EnlacePublicoFactura`: un token
aleatorio (GUID), con expiración configurable (7 días por defecto), servido
por un endpoint deliberadamente público (`GET /facturas/publico/{token}/pdf`,
`[AllowAnonymous]`) — la seguridad no depende de `[Authorize]` sino de que
el token sea impredecible y expire. Esto es distinto de "no hay seguridad":
queda auditado (quién lo generó, cuántas veces se accedió, cuándo) y
expira solo, sin exponer el resto del sistema.

### Funcionalidades completadas
- `POST /facturas/{id}/compartir/whatsapp` — genera o reutiliza el enlace
  vigente, arma el mensaje con la **plantilla exacta** pedida en la sección
  14, y sugiere el teléfono del cliente normalizado a formato internacional
  (código de país 504 de Honduras si el número registrado es local de 8
  dígitos).
- Frontend: panel editable (número de teléfono y mensaje, ambos con el
  valor sugerido pero modificables — sección 14, puntos 2 y 6), validación
  básica de formato antes de habilitar el botón, botón "Compartir por
  WhatsApp" resaltado en verde como opción principal (antes de "Descargar
  PDF" e "Imprimir" en el orden visual).
- Bloqueo real: no se puede compartir una factura anulada (sección 14,
  punto 17: "validar que la factura exista y esté autorizada").
- `HistorialEnvioFactura` + `POST /facturas/{id}/compartir/registrar` +
  `GET /facturas/{id}/historial-envios`: registro y consulta de cada
  intento (canal, destinatario, resultado, quién, cuándo). Panel de
  historial visible desde la misma vista de factura.
- Nueva acción de permiso `Compartir` (pedida explícitamente en la parte 2
  del prompt, sección 6, lista de acciones) agregada al enum
  `AccionPermiso` y al catálogo de Facturación.

### Limitación real documentada, no simulada
No existe forma de confirmar que el mensaje de WhatsApp **llegó** al
destinatario sin una integración oficial (WhatsApp Business API/Meta Cloud
API), que este proyecto no tiene contratada. Lo que se registra es "el
usuario abrió el flujo de envío" (`Resultado: Iniciado`), no una entrega
confirmada — está explícito en comentarios del código y no se pretende lo
contrario en ningún mensaje de la interfaz.

### Elementos que necesitan pruebas
- Compilación real del backend (sigue pendiente desde la fase 4 — QuestPDF
  y ahora este código nuevo tampoco se compilaron ni una vez).
- Abrir el enlace público desde un dispositivo sin sesión iniciada
  (confirmar que realmente no requiere login).
- Enlace expirado (confirmar el mensaje de error, no una excepción cruda).
- Normalización de teléfono con números que ya traen código de país.

### Archivos creados
`backend/src/Domain/Entities/{EnlacePublicoFactura,HistorialEnvioFactura}.cs`,
`backend/src/Infrastructure/Persistence/Configurations/EnlacePublicoFacturaConfiguration.cs`,
`backend/src/Application/Interfaces/{IFacturaCompartirRepository,IFacturaCompartirService}.cs`,
`backend/src/Infrastructure/Repositories/FacturaCompartirRepository.cs`,
`backend/src/Application/Services/FacturaCompartirService.cs`,
`backend/src/Application/DTOs/CompartirFacturaDto.cs`,
`docs/migraciones/005_fase5_whatsapp_compartir_facturas.sql`.

### Archivos modificados
`backend/src/Domain/Enums/AccionPermiso.cs` (+Compartir),
`backend/src/Application/Common/CatalogoPermisosBase.cs`,
`backend/src/Infrastructure/Persistence/AppDbContext.cs`,
`backend/src/API/Controllers/FacturasController.cs`, `API/Program.cs`,
`backend/src/API/appsettings.json` (+AppSettings:BackendPublicUrl,
EnlacePublicoFacturaDiasValidez),
`frontend/src/app/core/models/factura.model.ts`,
`frontend/src/app/services/factura.service.ts`,
`frontend/src/app/features/facturas/factura-view.component.{ts,html,scss}`.

### Endpoints creados
`POST /facturas/{id}/compartir/whatsapp`,
`POST /facturas/{id}/compartir/registrar`,
`GET /facturas/{id}/historial-envios`,
`GET /facturas/publico/{token}/pdf` (público, sin autenticación).

### Variables de entorno / configuración requeridas
`AppSettings:BackendPublicUrl` — **debe configurarse con la URL pública
real del backend en Render antes de desplegar** (actualmente
`http://localhost:5000` en `appsettings.json`, solo válido en desarrollo).
Sin esto, el enlace que se manda por WhatsApp apuntaría a localhost y no
funcionaría para el cliente real.

### Cambios en base de datos
2 tablas nuevas + 1 permiso nuevo en el catálogo (ver script SQL).

### Build real
Frontend: OK, 0 errores (factura-view-component confirmado en los lazy
chunks). Backend: no verificable, arrastra la misma limitación desde la
fase 4 (QuestPDF sin compilar) más este código nuevo.
---

## Fase 6 — Correo (SMTP configurable)

**Estado: terminada** (misma limitación de compilación de backend de las
fases 4-5).

### Objetivo alcanzado
Correo como opción SECUNDARIA de envío (sección 15), con el PDF adjunto
real (no un enlace, a diferencia de WhatsApp — con SMTP normal adjuntar es
viable y es lo que el prompt prefiere: "adjuntar el PDF o proporcionar un
enlace seguro").

### Decisión técnica: System.Net.Mail, no MailKit
Se usó `System.Net.Mail.SmtpClient` (parte del framework base de .NET) en
vez de MailKit. Es una decisión deliberada de gestión de riesgo: QuestPDF
(fase 4) ya quedó como dependencia nueva sin verificar compilando; sumar
una segunda dependencia externa (MailKit) en la misma sesión sin poder
compilar ninguna de las dos habría sido más riesgoso. `SmtpClient` cubre el
caso de uso (SMTP básico con usuario/contraseña) sin necesitar NuGet.

### Funcionalidades completadas
- `IEmailService`/`SmtpEmailService`: valida que la configuración SMTP no
  sean los placeholders `CHANGE_ME` antes de intentar conectar (error claro
  en vez de excepción de red confusa). Nunca expone el detalle técnico del
  error al usuario final (sección 17); el detalle real queda en logs
  (`ILogger`).
- Configuración en `appsettings.json` bajo `Smtp:*`, con placeholders
  `CHANGE_ME` — **nunca credenciales reales commiteadas**. En producción se
  sobrescriben con variables de entorno (`Smtp__Host`, `Smtp__UsuarioSmtp`,
  `Smtp__PasswordSmtp`, etc., doble guion bajo = convención de ASP.NET Core
  para configuración anidada vía entorno).
- `FacturaCompartirService.EnviarPorCorreoAsync`: valida formato de correo,
  bloquea facturas anuladas, genera el PDF, arma asunto formal y cuerpo
  HTML profesional, envía con adjunto, registra el intento CON el resultado
  real (`Enviado` o `Error`, a diferencia de WhatsApp donde solo se puede
  registrar `Iniciado` por la limitación ya documentada en la fase 5).
- `POST /facturas/{id}/compartir/correo` — protegido con
  `Facturacion:Compartir` (mismo permiso que WhatsApp, reutilizado, no
  duplicado).
- Frontend: botón "Enviar por correo" (opción secundaria, después de
  WhatsApp en el orden visual), panel con correo prellenado desde el
  cliente pero editable, validación de formato en vivo, mismo panel de
  historial de envíos ya construido en la fase 5 (reutilizado, sin
  duplicar).

### Limitación real, no oculta
El SMTP de este proyecto **no está configurado con credenciales reales**
(sigue con `CHANGE_ME` en `appsettings.json`). El endpoint devolverá un
error claro y controlado ("El envío de correo no está configurado
todavía...") hasta que se configuren las variables de entorno reales en
Railway/producción — documentado explícitamente en comandos de
integración.

### Elementos que necesitan pruebas
- Compilación real del backend (arrastra la limitación de las fases 4-5).
- Envío real una vez configuradas credenciales SMTP verdaderas (Gmail con
  contraseña de aplicación, SendGrid, u otro proveedor).
- Adjunto de PDF grande (verificar límites del proveedor SMTP elegido).

### Archivos creados
`backend/src/Application/Interfaces/IEmailService.cs`,
`backend/src/Infrastructure/Services/SmtpEmailService.cs`.

### Archivos modificados
`backend/src/Application/Interfaces/IFacturaCompartirService.cs`,
`backend/src/Application/Services/FacturaCompartirService.cs`,
`backend/src/API/Controllers/FacturasController.cs`, `API/Program.cs`,
`backend/src/API/appsettings.json` (+sección Smtp con placeholders),
`frontend/src/app/services/factura.service.ts`,
`frontend/src/app/features/facturas/factura-view.component.{ts,html,scss}`.

### Endpoints creados
`POST /facturas/{id}/compartir/correo`.

### Dependencias agregadas
Ninguna (`System.Net.Mail` es parte del framework .NET).

### Variables de entorno requeridas (producción)
`Smtp__Host`, `Smtp__Port`, `Smtp__UsuarioSmtp`, `Smtp__PasswordSmtp`,
`Smtp__UsarSsl`, `Smtp__CorreoRemitente`, `Smtp__NombreRemitente` — ver
`03-COMANDOS-INTEGRACION.md` para el detalle exacto.

### Build real
Frontend: OK, 0 errores (`factura-view-component` 60.16 kB, confirma el
código nuevo). Backend: no verificable, misma limitación de las fases 4-5.

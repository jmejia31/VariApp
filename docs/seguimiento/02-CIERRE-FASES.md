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
| 4 | PDF real de facturas | No iniciada |
| 5 | WhatsApp | No iniciada |
| 6 | Correo | No iniciada |
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

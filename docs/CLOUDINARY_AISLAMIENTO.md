# Cloudinary — aislamiento entre DEV y PROD

## Objetivo

Evitar que una base Aiven de DEV creada desde un fork productivo pueda eliminar activos reales de PROD.

El identificador oficial del entorno de DEV es `solqaryn_dev`.

## Comportamiento implementado

Cuando `Cloudinary__EnvironmentPrefix=solqaryn_dev`:

- las nuevas imágenes de productos se almacenan bajo `solqaryn_dev/inventoryapp/productos`;
- los nuevos comprobantes se almacenan bajo `solqaryn_dev/inventoryapp/compras`;
- las nuevas fotografías de perfil se almacenan bajo `solqaryn_dev/solqaryn/perfiles`;
- cualquier eliminación cuyo `PublicId` no comience con `solqaryn_dev/` queda bloqueada antes de llamar a Cloudinary.

Esto protege los activos productivos aunque la base de Aiven DEV conserve URLs y `PublicId` históricos de PROD.

Cuando el prefijo está vacío, el comportamiento productivo se conserva sin cambiar las carpetas ni los identificadores actuales.

## Recurso autorizado de DEV

La clave de API creada por el propietario y etiquetada `solqaryn_dev` es la única clave autorizada para el servicio Render de DEV. Sus valores deben permanecer exclusivamente como secretos externos y nunca deben copiarse al repositorio, al PR, a capturas públicas o al chat.

Las otras claves visibles en el panel de Cloudinary no se consideran entornos duplicados: pueden pertenecer a funciones internas de la plataforma. No deben eliminarse sin una auditoría de dependencias en el panel.

## Validación automatizada

`CloudinaryEnvironmentIsolationTests` verifica que DEV no pueda eliminar:

- una imagen productiva de producto;
- un comprobante productivo de compra.

El workflow de DEV también verifica que `render.yaml` declare exactamente el prefijo `solqaryn_dev`.

## Validación externa pendiente

En el entorno real de DEV se debe probar:

1. confirmar que Render usa la clave etiquetada `solqaryn_dev`, sin exponer su valor;
2. subir un activo nuevo y confirmar su prefijo `solqaryn_dev/`;
3. eliminar ese activo de DEV;
4. intentar eliminar una referencia heredada de PROD y confirmar que se rechaza;
5. revisar que ningún activo productivo haya cambiado;
6. verificar almacenamiento, transformaciones y ancho de banda.

La opción más segura sigue siendo usar un product environment o una cuenta Cloudinary completamente separada para DEV. Mientras se comparta el mismo product environment, el prefijo y la clave autorizada son controles obligatorios.

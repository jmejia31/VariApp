# Cloudinary — aislamiento entre Desarrollo y Producción

## Objetivo

Evitar que una base Aiven de Desarrollo creada desde un fork productivo pueda eliminar activos reales de Producción.

El identificador oficial del entorno de Desarrollo es `varistorehn_desarrollo`.

## Comportamiento implementado

Cuando `Cloudinary__EnvironmentPrefix=varistorehn_desarrollo`:

- las nuevas imágenes de productos se almacenan bajo `varistorehn_desarrollo/inventoryapp/productos`;
- los nuevos comprobantes se almacenan bajo `varistorehn_desarrollo/inventoryapp/compras`;
- las nuevas fotografías de perfil se almacenan bajo `varistorehn_desarrollo/variapp/perfiles`;
- cualquier eliminación cuyo `PublicId` no comience con `varistorehn_desarrollo/` queda bloqueada antes de llamar a Cloudinary.

Esto protege los activos productivos aunque la base de Aiven Desarrollo conserve URLs y `PublicId` históricos de Producción.

Cuando el prefijo está vacío, el comportamiento productivo se conserva sin cambiar las carpetas ni los identificadores actuales.

## Recurso autorizado de Desarrollo

La clave de API creada por el propietario y etiquetada `varistorehn_desarrollo` es la única clave autorizada para el servicio Render de Desarrollo. Sus valores deben permanecer exclusivamente como secretos externos y nunca deben copiarse al repositorio, al PR, a capturas públicas o al chat.

Las otras claves visibles en el panel de Cloudinary no se consideran entornos duplicados: pueden pertenecer a funciones internas de la plataforma. No deben eliminarse sin una auditoría de dependencias en el panel.

## Validación automatizada

`CloudinaryEnvironmentIsolationTests` verifica que Desarrollo no pueda eliminar:

- una imagen productiva de producto;
- un comprobante productivo de compra.

El workflow de Desarrollo también verifica que `render.yaml` declare exactamente el prefijo `varistorehn_desarrollo`.

## Validación externa pendiente

En el entorno real de Desarrollo se debe probar:

1. confirmar que Render usa la clave etiquetada `varistorehn_desarrollo`, sin exponer su valor;
2. subir un activo nuevo y confirmar su prefijo `varistorehn_desarrollo/`;
3. eliminar ese activo de Desarrollo;
4. intentar eliminar una referencia heredada de Producción y confirmar que se rechaza;
5. revisar que ningún activo productivo haya cambiado;
6. verificar almacenamiento, transformaciones y ancho de banda.

La opción más segura sigue siendo usar un product environment o una cuenta Cloudinary completamente separada para Desarrollo. Mientras se comparta el mismo product environment, el prefijo y la clave autorizada son controles obligatorios.

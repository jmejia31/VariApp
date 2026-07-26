# Cloudinary — aislamiento entre Desarrollo y Producción

## Objetivo

Evitar que una base Aiven de Desarrollo creada desde un fork productivo pueda eliminar activos reales de Producción.

## Comportamiento implementado

Cuando `Cloudinary__EnvironmentPrefix=desarrollo`:

- las nuevas imágenes de productos se almacenan bajo `desarrollo/inventoryapp/productos`;
- los nuevos comprobantes se almacenan bajo `desarrollo/inventoryapp/compras`;
- las nuevas fotografías de perfil se almacenan bajo `desarrollo/variapp/perfiles`;
- cualquier eliminación cuyo `PublicId` no comience con `desarrollo/` queda bloqueada antes de llamar a Cloudinary.

Esto protege los activos productivos aunque el fork de Aiven conserve URLs y `PublicId` históricos de Producción.

Cuando el prefijo está vacío, el comportamiento productivo se conserva sin cambiar las carpetas ni los identificadores actuales.

## Validación automatizada

`CloudinaryEnvironmentIsolationTests` verifica que Desarrollo no pueda eliminar:

- una imagen productiva de producto;
- un comprobante productivo de compra.

## Validación externa pendiente

En el entorno real de Desarrollo se debe probar:

1. subir un activo nuevo y confirmar su prefijo `desarrollo/`;
2. eliminar ese activo de Desarrollo;
3. intentar eliminar una referencia heredada de Producción y confirmar que se rechaza;
4. revisar que ningún activo productivo haya cambiado;
5. verificar almacenamiento, transformaciones y ancho de banda.

La opción más segura sigue siendo usar un product environment o una cuenta Cloudinary completamente separada para Desarrollo.

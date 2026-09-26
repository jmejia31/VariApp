# Certificación Cloudinary DEV — 2026-09-25

Estado: **PASS funcional del recurso nuevo / PENDIENTE retiro de referencias legacy**

## Ownership y entorno canónico

- Cuenta/perfil: `Solqaryn Platform`
- Correo: `solqaryn.platform@outlook.com`
- Product Environment: `riyrzmob`
- Product Environment ID: `7ab9e3e6de660a0b70eb4a5bacf331`
- Estado: `Active`
- Keys activas observadas: `Root`, `solqaryn_dev`, `solqaryn_prod`
- `Root`: administración/recuperación
- `solqaryn_dev`: DEV
- `solqaryn_prod`: reservada para PROD, no conectada durante esta fase

## Render DEV

- Servicio: `solqaryn-api-dev`
- `Cloudinary__CloudName=riyrzmob`
- `Cloudinary__EnvironmentPrefix=solqaryn_dev`
- API Key / API Secret: configurados con valores ocultos
- Deploy posterior: `dep-darcsc0jo6nc73fffmtg` -> `live`
- Health posterior: HTTP 200

## Upload funcional

La aplicación DEV subió correctamente un asset nuevo. Cloudinary Media Library muestra la jerarquía:

`solqaryn_dev/inventoryapp/productos/empresas/1/`

La API pública de SOLQARYN DEV devuelve para el producto de prueba una URL con:

- host: `res.cloudinary.com`
- cloud: `riyrzmob`
- prefijo: `solqaryn_dev/inventoryapp/productos/empresas/1/`

Resultado: **el pipeline nuevo de upload DEV está certificado**.

## Inventario de deuda legacy

Workflow read-only: `DEV - Inventario Cloudinary legacy`
Run: `36182095589`
Artifact: `cloudinary-dev-legacy-inventory-36182095589`

Hallazgos:

| Tabla | Columna | Filas con referencia legacy |
|---|---|---:|
| `ProductoImagenes` | `Url` | 10 |
| `ProductoImagenes` | `PublicId` | 10 |
| `CompraDocumentos` | `Url` | 1 |
| `CompraDocumentos` | `PublicId` | 1 |
| `Usuarios` | `FotoPerfilUrl` | 2 |
| `Usuarios` | `FotoPerfilPublicId` | 2 |

Esto corresponde a 13 filas lógicas de assets que todavía dependen del cloud legacy.

## Criterio de cierre restante

Cloudinary DEV se cerrará completamente cuando:

1. las 13 referencias legacy se copien al cloud `riyrzmob`;
2. sus URL/PublicId se actualicen en `solqaryn_dev`;
3. un re-scan read-only devuelva cero referencias al cloud/prefijos legacy;
4. se confirme que los assets nuevos cargan correctamente desde storefront/admin;
5. solo entonces se podrá retirar la dependencia Cloudinary DEV de la cuenta personal.

Producción permanece fuera de alcance.

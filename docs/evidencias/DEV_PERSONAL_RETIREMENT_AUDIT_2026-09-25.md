# Retiro DEV personal — auditoría previa a destrucción — 2026-09-25

## Estado

**PARCIAL.** Se retiró lo que ya tenía evidencia suficiente y se mantienen bloqueados los destructivos que podrían afectar Producción legacy.

## Vercel

- `identidad-retirada-desarrollo`: eliminado previamente de la cuenta personal.
- Team corporativo `SOLQARYN`: sólo proyecto `solqaryn-dev`.
- `varistorehn` PROD legacy: fuera de alcance y no tocado.

## Render

Workspace corporativo visible:

- `SOLQARYN`
- email: `solqaryn.platform@outlook.com`

Servicios visibles:

- `solqaryn-api-dev`
- servicio reservado PROD

DEV histórico conocido por evidencia:

- nombre: `solqaryn-api-desarrollo`
- service ID: `srv-d9jblq7avr4c73c74jng`

El ID histórico no es accesible desde el workspace corporativo y el hostname público histórico ya no resuelve. No se ejecutó una eliminación remota porque el conector corporativo no administra la cuenta antigua.

## Aiven

Workflow read-only:

- `DEV - Inventario Aiven para retiro legacy`
- run: `36193976802`
- resultado: `SUCCESS`

Inventario visible para el token corporativo:

- proyecto: `solqaryn`
- servicio: `solqaryn-mysql`
- estado: `RUNNING`
- proyectos legacy visibles: ninguno

Conclusión: la cuenta corporativa está aislada del proyecto Aiven personal legacy. Esta evidencia no demuestra qué servicio/base consume PROD legacy en la cuenta antigua, por lo que queda prohibido borrar el servicio personal o `defaultdb`/`varistorehn_desarrollo` hasta completar esa comprobación.

## Cloudinary

DEV canónico ya utiliza el cloud `riyrzmob` y prefijo `solqaryn_dev`. El Cloudinary personal legacy no se elimina mientras no exista prueba de cero consumidores de PROD legacy.

## Cloudflare / DNS

Cuenta corporativa:

- `Solqaryn.platform@outlook.com's Account`

Zona:

- `solqaryn.com`
- estado: `pending`

Registros actuales:

- dos TXT `_acme-challenge.solqaryn.com`

No existen aliases DEV legacy en esta zona corporativa.

## GitHub

Readback de permisos:

- `jmejia31`: `admin`
- `morales35alex`: `write`

La autoridad vigente ratificada el 2026-09-25 conserva `jmejia31` como Owner secundario/de recuperación. Por tanto no se elimina como “acceso personal residual” sin una nueva decisión explícita del propietario.

## Producción

`PRODUCTION_TOUCHED=FALSE`.

# VariStoreHn — Fase 1: navegación pública y Header

## Estado

**Implementada y validada en rama de integración.**

Esta fase resuelve la deuda explícita heredada de Fase 0: extraer la cabecera pública del componente monolítico y completar la navegación móvil, sin adelantar las páginas independientes reservadas a las fases siguientes.

## Alcance entregado

### Header público reutilizable

`VaristorehnHeaderComponent` es un componente standalone y desacoplado del CRUD administrativo. Recibe estado comercial mínimo mediante inputs y comunica intención mediante outputs:

- búsqueda actual;
- categorías disponibles para navegación rápida;
- categoría activa;
- unidades reales del carrito;
- subtotal real del carrito;
- disponibilidad del canal WhatsApp.

La identidad visual y empresarial no se duplica: nombre, logo, eslogan, moneda y número de WhatsApp continúan proviniendo de `EmpresaIdentidadService` y, por tanto, de la configuración pública del sistema/base de datos.

### Navegación pública

La cabecera expone Inicio, Productos y Categorías, además de accesos rápidos a categorías. Las URLs se construyen a partir de `VARISTOREHN_PATHS`.

Fase 1 no activa `/varistorehn/productos` ni `/varistorehn/categorias` como páginas independientes porque el Plan Maestro las asigna a fases posteriores. Mientras esas páginas no existan, los accesos navegan de forma estable a las secciones públicas existentes de `/varistorehn`. Esto conserva la regla de Fase 0 de no publicar placeholders rotos para aparentar avance.

### Búsqueda

El buscador permanece visible en desktop y móvil, conserva `role="search"`, label accesible, `type="search"`, `enterkeyhint="search"` y continúa filtrando el catálogo real mediante el estado existente del escaparate. Enviar la búsqueda lleva al catálogo sin introducir una segunda fuente de verdad.

### Carrito

La cabecera no mantiene un carrito paralelo. El contador y subtotal provienen directamente del estado real de `VaristorehnComponent`; la acción de carrito abre el diálogo existente. La extracción del estado global/persistente del carrito sigue reservada a Fase 5.

### WhatsApp secundario

WhatsApp aparece únicamente cuando:

1. el modo de compra lo permite; y
2. existe un número normalizable mediante la regla compartida `telefonoWhatsapp`.

No se inventa un contacto ni se muestra una acción inutilizable.

### Navegación móvil y accesibilidad

El menú móvil usa `dialog.showModal()` para obtener contención de foco nativa y soporte de Escape. Incluye:

- `aria-controls` y `aria-expanded` en el disparador;
- título del diálogo mediante `aria-labelledby`;
- primer destino enfocado al abrir;
- cierre por botón, Escape y backdrop;
- restauración de foco al disparador cuando el cierre es manual;
- supresión deliberada de esa restauración cuando el usuario navega, selecciona una categoría o abre el carrito, evitando saltos de foco que contradigan la acción solicitada;
- estado de filtros rápidos expuesto con `aria-pressed`;
- objetivos táctiles mínimos de 44 px en controles principales.

### Responsive e identidad visual

La cabecera tiene reglas propias para desktop, tablet y móvil. No introduce valores hexadecimales ni una paleta paralela: utiliza exclusivamente tokens `--color-*`, radios y demás variables del tema configurado por el sistema.

## Frontera público / administrativo

La guardia de aceptación de Fase 1 comprueba que el header no importe ni dependa de:

- `authGuard`;
- `permisoGuard`;
- `ProductosListComponent`;
- `CategoriasListComponent`.

La experiencia pública mantiene así la separación establecida en Fase 0.

## Protección contra regresiones

Se añadió `frontend/scripts/validate-varistorehn-fase1.mjs` y se incorporó al comando normal `npm run lint`. La protección ya no depende de un workflow temporal de esta rama; cualquier CI futuro que ejecute el lint estándar verificará también los invariantes de Fase 1.

La guardia verifica, entre otros puntos:

- uso de `VARISTOREHN_PATHS`;
- uso de identidad empresarial compartida;
- modal móvil real;
- semántica de búsqueda;
- ARIA de navegación móvil;
- contador real de carrito;
- acción WhatsApp;
- objetivos táctiles;
- ausencia de colores hexadecimales en el header;
- adopción del nuevo header por el escaparate;
- eliminación del header monolítico del template principal;
- wiring de búsqueda y carrito;
- ausencia de dependencias administrativas.

## Evidencia ejecutable

HEAD funcional certificado: `c14235b3086a063503c602a16a2c380ea9073f2d`.

GitHub Actions run: `34519623344`.

Resultado:

- `npm ci`: **success**;
- `npm run lint`: **success**;
- guardia específica `validate-varistorehn-fase1.mjs`: **success**;
- `npm run build:prod`: **success**;
- job `Frontend Fase 1`: **success**.

También existió una ejecución verde previa (`34519342478`) antes del pulido final, lo que aporta una segunda señal independiente sobre la extracción y el build.

## Concurrencia

La rama se creó desde `dde80b13e6fd9e361a6ca2e5bc074a8734bb0d2b`. Durante el desarrollo, `Desarrollo` recibió cuatro commits adicionales; la comparación confirmó que esos cambios afectan archivos de control/evidencia VAEP y no los archivos de VariStoreHn modificados por esta fase. El PR permanece mergeable.

## Definition of Done

- [x] Header público reutilizable y desacoplado del componente monolítico.
- [x] Identidad proveniente del servicio/configuración central.
- [x] Buscador visible y funcional en desktop y móvil.
- [x] Navegación pública clara a inicio, productos y categorías.
- [x] Cero controles o dependencias administrativas.
- [x] Carrito con contador real de unidades y subtotal real.
- [x] WhatsApp como acción secundaria cuando hay configuración válida.
- [x] Menú móvil accesible con estado ARIA y manejo de foco.
- [x] URLs públicas construidas desde `VARISTOREHN_PATHS`.
- [x] Sin placeholders de páginas asignadas a Fases 2–6.
- [x] Responsive con objetivos táctiles adecuados.
- [x] Colores gobernados por los tokens del sistema/base de datos.
- [x] Guardia anti-regresión integrada al lint normal.
- [x] Lint/TypeScript y build de producción verdes.

## Límite de fase

No forman parte de Fase 1 y permanecen correctamente asignados al roadmap:

- página independiente de categorías y consumo visual de `CategoriaTienda`: Fase 2;
- catálogo independiente: Fase 3;
- detalle de producto por slug fuera del modal: Fase 4;
- estado único de carrito extraído del componente actual: Fase 5;
- checkout/pedido real: Fase 6.

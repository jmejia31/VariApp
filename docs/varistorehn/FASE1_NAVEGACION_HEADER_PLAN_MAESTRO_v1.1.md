# VariStoreHn — Fase 1: navegación pública y Header

## Estado

**Implementada, re-auditada y endurecida.**

Esta fase resuelve la deuda explícita heredada de Fase 0: extraer la cabecera pública del componente monolítico y completar la navegación móvil, sin adelantar las páginas independientes reservadas a las fases siguientes.

Después del primer cierre se ejecutó una reauditoría más estricta. La revisión dejó de aceptar como evidencia suficiente una guardia estática y añadió validación real de navegador. Esa segunda auditoría encontró dos problemas concretos: estilos del header anterior que habían quedado residuales en el componente padre y una franja responsive de 761–1120 px donde WhatsApp quedaba oculto aunque todavía no existía el menú móvil. Ambos fueron corregidos antes de recertificar la fase.

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

WhatsApp aparece únicamente cuando el modo de compra lo permite y existe un número normalizable mediante la regla compartida `telefonoWhatsapp`. No se inventa un contacto ni se muestra una acción inutilizable.

La reauditoría descubrió que el breakpoint original ocultaba `.header-whatsapp` desde 1120 px hacia abajo, mientras `.mobile-menu-trigger` solo aparecía a 760 px. Eso dejaba sin acceso de cabecera a WhatsApp en 761–1120 px. El hardening corrige la transición responsive:

- desktop: acción completa;
- 761–1120 px: acción compacta de 48 px, todavía accesible por nombre;
- hasta 760 px: la acción de cabecera se sustituye por la acción equivalente dentro del drawer móvil.

La prueba de navegador cubre explícitamente 1120, 900 y 761 px y comprueba que no exista overflow horizontal en esa franja.

### Navegación móvil y accesibilidad

El menú móvil usa `dialog.showModal()` para obtener contención de foco nativa y soporte de Escape. Incluye `aria-controls` y `aria-expanded` en el disparador, título mediante `aria-labelledby`, primer destino enfocado al abrir, cierre por botón/Escape/backdrop, restauración de foco al disparador cuando corresponde, supresión deliberada de esa restauración al navegar o abrir otra superficie, estado de categorías con `aria-pressed` y objetivos táctiles mínimos de 44 px.

La prueba real de navegador verifica apertura/cierre del drawer, foco inicial, Escape con restauración de foco, selección de categoría, cambio del modo de compra, disponibilidad de WhatsApp, targets táctiles y reflow sin overflow a 760, 390 y 320 px.

### Responsive e identidad visual

La cabecera tiene reglas propias para desktop, tablet y móvil. No introduce valores hexadecimales ni una paleta paralela: utiliza exclusivamente tokens `--color-*`, radios y demás variables del tema configurado por el sistema.

Los selectores del header anterior fueron retirados de `varistorehn.component.scss` y `varistorehn.responsive.scss`; los estilos de cabecera viven ahora con `VaristorehnHeaderComponent`. La guardia estática impide reintroducir esos selectores en el componente padre.

## Frontera público / administrativo

La guardia de aceptación de Fase 1 comprueba que el header no importe ni dependa de `authGuard`, `permisoGuard`, `ProductosListComponent` ni `CategoriasListComponent`. La experiencia pública mantiene así la separación establecida en Fase 0.

## Protección contra regresiones

La protección tiene ahora dos niveles complementarios.

`frontend/scripts/validate-varistorehn-fase1.mjs`, integrado en `npm run lint`, comprueba invariantes de arquitectura y markup: uso de `VARISTOREHN_PATHS`, identidad empresarial compartida, modal móvil real, semántica de búsqueda, ARIA, carrito real, WhatsApp normalizado, objetivos táctiles, ausencia de colores hexadecimales propios, adopción del header reutilizable, wiring hacia búsqueda/carrito, ausencia de dependencias administrativas y ausencia de selectores residuales del header en los estilos del padre.

`frontend/e2e/varistorehn-fase1.spec.ts` añade una regresión de navegador con Playwright. Se expone mediante `npm run test:e2e:varistorehn-fase1` y el workflow permanente `.github/workflows/varistorehn-fase1-regression.yml` se ejecuta cuando cambian archivos relevantes de VariStoreHn, identidad, estilos globales, entornos o la propia prueba.

## Evidencia ejecutable original

HEAD funcional certificado de la primera implementación: `c14235b3086a063503c602a16a2c380ea9073f2d`.

GitHub Actions run original: `34519623344`.

Resultado original: `npm ci`, `npm run lint`, guardia específica y `npm run build:prod` en **success**. También existió una ejecución verde previa (`34519342478`).

## Evidencia de reauditoría y hardening

La primera auditoría de navegador fue deliberadamente más exigente que el cierre original. Un primer intento reveló un locator ambiguo en la propia prueba; tras corregirlo, la ejecución detectó un defecto real de producto: WhatsApp no estaba disponible a 900 px. Desktop, móvil y la validación de WhatsApp inválido sí pasaban.

Después de corregir el breakpoint y limpiar estilos residuales, el run de hardening `34523138867` certificó el candidato con:

- `npm ci`: **success**;
- TypeScript + `npm run lint` + guardia reforzada: **success**;
- `npm run build:prod`: **success**;
- Playwright desktop: **success**;
- Playwright franja tablet 1120/900/761 px y ausencia de overflow: **success**;
- Playwright móvil con drawer/foco/Escape/categorías/modo de compra/reflow: **success**;
- WhatsApp inválido sin acción rota: **success**;
- total: **4/4 pruebas de navegador aprobadas**.

### Observación de build no bloqueante

El build sigue mostrando un warning de presupuesto sobre `varistorehn.component.scss`: 17.10 kB frente al umbral de warning de 16 kB. La limpieza de Fase 1 lo redujo desde 19.80 kB y eliminó del padre los estilos residuales de cabecera. El remanente corresponde al contenido monolítico que el propio Plan Maestro descompone en Fases 2–5 (categorías, catálogo, detalle y carrito). No se aumenta el presupuesto ni se adelanta esa extracción únicamente para silenciar el warning; el build de producción finaliza correctamente.

## Definition of Done revalidada

- [x] Header público reutilizable y desacoplado del componente monolítico.
- [x] Identidad proveniente del servicio/configuración central.
- [x] Buscador visible y funcional en desktop y móvil.
- [x] Navegación pública clara a inicio, productos y categorías.
- [x] Cero controles o dependencias administrativas.
- [x] Carrito con contador real de unidades y subtotal real.
- [x] WhatsApp como acción secundaria cuando hay configuración válida y continuidad responsive en desktop/tablet/móvil.
- [x] Menú móvil accesible con estado ARIA y manejo de foco verificado en navegador.
- [x] URLs públicas construidas desde `VARISTOREHN_PATHS`.
- [x] Sin placeholders de páginas asignadas a Fases 2–6.
- [x] Responsive sin overflow en los breakpoints críticos auditados y con objetivos táctiles adecuados.
- [x] Colores gobernados por los tokens del sistema/base de datos.
- [x] Estilos del header extraídos del padre y guardados contra regresión.
- [x] Guardia estática integrada al lint normal.
- [x] Suite Playwright específica y workflow de regresión permanente.
- [x] Lint/TypeScript, build de producción y 4/4 pruebas runtime verdes.

## Límite de fase

No forman parte de Fase 1 y permanecen correctamente asignados al roadmap:

- página independiente de categorías y consumo visual de `CategoriaTienda`: Fase 2;
- catálogo independiente: Fase 3;
- detalle de producto por slug fuera del modal: Fase 4;
- estado único de carrito extraído del componente actual: Fase 5;
- checkout/pedido real: Fase 6.

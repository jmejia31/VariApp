# VariStoreHn — Fase 0: Diagnóstico y base técnica

Documento de trabajo iniciado desde el Plan Maestro de Mejoras v1.1.

## Objetivo
Asegurar que la tienda pública, los datos y las rutas estén preparados antes de rediseñar pantallas.

## Definition of Done
- [ ] Existe un modelo único de producto y categoría usado por todas las páginas públicas.
- [ ] Las rutas públicas objetivo están definidas.
- [ ] Está identificado qué código actual se conserva, qué se refactoriza y qué se elimina.
- [ ] La tienda pública no depende de componentes exclusivamente administrativos.

## Inventario inicial
Pendiente de completar contra el HEAD real de `Desarrollo`.

## Contratos de datos
Pendiente de validar contra servicios/API reales antes de fijarlos.

## Rutas públicas objetivo
- `/varistorehn`
- `/varistorehn/productos`
- `/varistorehn/categorias`
- `/varistorehn/categoria/:slug`
- `/varistorehn/producto/:slug`
- `/varistorehn/ofertas`
- `/varistorehn/carrito`
- `/varistorehn/checkout`
- `/varistorehn/pedido/:id`

## Estados transversales
Toda consulta pública debe contemplar: `loading`, `empty`, `error`, `available`, `outOfStock` y `promotion`.

## Regla de trabajo
No se inicia rediseño visual fino ni fases posteriores hasta cerrar los pendientes funcionales/técnicos de esta fase, salvo dependencia crítica documentada.

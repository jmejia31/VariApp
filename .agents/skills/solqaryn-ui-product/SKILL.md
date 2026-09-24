---
name: solqaryn-ui-product
description: "Calidad de UI de producto para SOLQARYN. Usar al disenar, implementar o revisar dashboards, app shell, formularios, tablas, settings, onboarding, estados vacios, navegacion, responsive, accesibilidad y polish dentro de la aplicacion Angular de solqaryn/VariApp. Debe respetar Angular 20, Angular Material, Signals y la arquitectura vigente."
---

# SOLQARYN UI Product

## Gate

Aplicar despues de `solqaryn-project-governance`. Esta skill no cambia el stack.

Stack autorizado para la UI actual:

- Angular 20 standalone;
- Signals;
- Angular Material;
- rutas lazy;
- guards de autenticacion y permisos;
- servicios HTTP existentes.

## Criterios

- jerarquia visual clara y consistente;
- formularios con labels, errores accionables y estados disabled/loading correctos;
- tablas legibles, responsive y navegables con teclado;
- estados vacios, error y carga explicitos;
- responsive desde viewport estrecho hasta escritorio amplio;
- contraste, foco visible, semantica y nombres accesibles;
- acciones sensibles condicionadas por UX y siempre protegidas por backend;
- no ocultar un defecto de autorizacion mediante UI;
- evitar refactors visuales globales si el scope es localizado.

## Validacion

- lint/build frontend aplicable;
- pruebas unitarias/componentes cuando existan;
- E2E o smoke de la superficie cambiada cuando sea material;
- revisar consola, overflow, teclado y estados de error;
- respetar `prefers-reduced-motion` cuando exista motion.

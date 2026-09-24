---
name: solqaryn-skill-authoring
description: "Autoria exclusiva de skills SOLQARYN. Usar para crear, actualizar, validar, reorganizar o retirar skills bajo .agents/skills/ en solqaryn/VariApp. Obliga a leer primero solqaryn-project-governance y solqaryn-skill-standard, usar namespace solqaryn-, preservar aislamiento del proyecto y registrar cambios de gobierno cuando corresponda."
---

# SOLQARYN Skill Authoring

## Gate previo

Antes de crear o editar una skill:

1. confirmar `solqaryn/VariApp` y `Desarrollo`;
2. aplicar `solqaryn-project-governance`;
3. aplicar `solqaryn-skill-standard`;
4. definir entrada esperada, salida esperada y herramientas/conectores necesarios;
5. comprobar que la capacidad no duplica una skill SOLQARYN existente.

## Creacion

- elegir un nombre `solqaryn-<funcion>` corto y especifico;
- redactar una descripcion que defina claramente los triggers;
- mantener el body en modo operativo: decisiones, secuencia, restricciones y validacion;
- crear referencias solo cuando reduzcan ambiguedad o carga de contexto;
- crear scripts solo cuando una comprobacion determinista sea mejor que instrucciones narrativas;
- no copiar reglas de otra superficie de SOLQARYN si basta con referenciar la autoridad canonica.

## Actualizacion

- preservar compatibilidad de nombre salvo migracion explicita;
- si cambia el alcance, revisar consumidores y registro de skills;
- retirar archivos obsoletos en el mismo changeset;
- no mantener aliases ambiguos para skills reemplazadas.

## Cierre

- validar estructura y metadata;
- ejecutar scope gate;
- actualizar `docs/REGISTRO_SKILLS_SOLQARYN.md`;
- registrar el cambio en `CHANGELOG_AI.md` cuando altere gobierno o comportamiento compartido.

---
name: solqaryn-project-governance
description: "Gobierno tecnico obligatorio y primera skill de entrada de SOLQARYN. Usar antes de cualquier tarea que analice, modifique, documente, pruebe, despliegue o administre solqaryn/VariApp, y antes de abrir cualquier otra skill SOLQARYN. Valida identidad, rama, arquitectura, seguridad, fuentes, autorizacion, impacto, skill routing y cierre; impone PROJECT_SCOPE_LOCK=STRICT y fail-closed."
---

# SOLQARYN Project Governance

## 1. Identidad obligatoria

- `PLATFORM=SOLQARYN`
- `PROJECT_ID=VARIAPP`
- `REPOSITORY=solqaryn/VariApp`
- `BRANCH=Desarrollo`
- `PROJECT_SCOPE_LOCK=STRICT`
- `SKILL_NAMESPACE=solqaryn-`
- `EXTERNAL_PROJECT_CONTEXT=DENY_BY_DEFAULT`

Esta es la primera skill de entrada. No abrir otra skill del proyecto antes de pasar este gate.

## 2. Fuentes canonicas

Leer en el orden necesario:

1. `docs/PROJECT_SCOPE_LOCK.md` para aislamiento;
2. `AGENTS.md` para reglas de colaboracion;
3. `docs/VAEP_AUTHORITY.md` para reglas operativas vigentes cuando la tarea toque VAEP/automatizaciones;
4. HEAD vivo de `Desarrollo`;
5. `PROJECT_CONTEXT.md` y `PROJECT_INDEX.md` para contexto y localizacion;
6. `ARCHITECTURE.md` cuando exista impacto estructural o transversal;
7. documentos especificos del scope y evidencia causal.

Un snapshot historico, chat, receipt o comentario no sustituye el estado vivo ni la autoridad vigente.

## 3. Gate de entrada para cualquier agente o chat

Antes de analizar o escribir:

1. confirmar repositorio y rama;
2. comprobar que la tarea pertenece a SOLQARYN;
3. comprobar que toda skill a usar empiece por `solqaryn-`;
4. no consultar fuentes fuera de SOLQARYN salvo excepcion expresamente autorizada;
5. identificar componente, capa, datos, seguridad, consumidores y validaciones afectadas;
6. determinar si la tarea es solo lectura, cambio en `Desarrollo` o accion que requiere autorizacion adicional;
7. cuando exista checkout local, ejecutar `node scripts/verify-project-scope.mjs`.

Si cualquier punto material es incierto, aplicar fail-closed en ese punto sin abandonar el proyecto.

## 4. Arquitectura de referencia

La arquitectura vigente se resume en `references/architecture.md`.

Reglas no negociables:

- Frontend Angular 20 standalone con Signals y Angular Material.
- Backend ASP.NET Core 8 Web API.
- Capas backend: Domain <- Application <- Infrastructure; API compone y expone.
- Persistencia MySQL con EF Core 8/Pomelo.
- Seguridad con JWT, BCrypt, RBAC relacional, auditoria, CORS explicito, rate limiting y headers de seguridad.
- Integraciones vigentes: Cloudinary, QuestPDF y SMTP.
- E2E/browser: Playwright/Chromium.
- La UI nunca sustituye controles de autorizacion del backend.
- Cambios de inventario, finanzas y documentos relacionados deben preservar consistencia y trazabilidad.
- Secretos permanecen fuera del repositorio.

No introducir un framework, capa, persistencia, mecanismo de auth, libreria transversal o via paralela solo porque una skill lo sugiera.

## 5. Ramas, entornos y produccion

- El trabajo ordinario se realiza en `Desarrollo`.
- Revalidar HEAD antes de escribir/publicar y preservar trabajo concurrente.
- No force-push, reset destructivo ni reescritura de historia compartida.
- Cualquier cambio futuro sobre `main`, Produccion, datos productivos, dominios, certificados, secretos o infraestructura productiva requiere autorizacion nueva y explicita del propietario y debe respetar la autoridad operativa vigente.
- Nunca inferir autorizacion productiva a partir de un release historico.

## 6. Impacto antes de editar

Responder al menos:

- componente primario;
- capas/rutas afectadas;
- dependencias directas y consumidores;
- interfaces/API/DTO que cambian;
- datos/migraciones/transacciones;
- auth/RBAC/PII/secretos;
- jobs/integraciones/documentos;
- observabilidad;
- pruebas y rollback;
- si cambia arquitectura/documentacion canonica.

Si el cambio es arquitectonico, actualizar en el mismo changeset `ARCHITECTURE.md`, `PROJECT_CONTEXT.md`, `PROJECT_INDEX.md` y `ARCHITECTURE_CHANGELOG.md` cuando corresponda.

## 7. Routing de skills

Consultar `references/skill-routing.md` y aplicar solo las skills SOLQARYN pertinentes.

Reglas:

- esta skill siempre va primero;
- una skill especializada no puede contradecir esta skill;
- UI administrativa: `solqaryn-ui-product`;
- motion: `solqaryn-motion` ademas de la skill de superficie;
- superficies publicas/marketing: `solqaryn-marketing-design`;
- redaccion persistente: `solqaryn-human-writing`;
- runbooks/lecciones recurrentes: `solqaryn-runbook-memory`;
- sesiones/contexto extensos: `solqaryn-context-efficiency`;
- resumen efimero de bajo riesgo: `solqaryn-low-risk-summary` solamente dentro de sus limites;
- creacion/edicion de skills: `solqaryn-skill-standard` + `solqaryn-skill-authoring`.

## 8. Calidad y cierre

No declarar una tarea cerrada solo porque compile o porque una skill fue leida.

El cierre debe demostrar, segun aplique:

- diff revisado y scope correcto;
- pruebas dirigidas y regresion proporcional;
- seguridad/RBAC/tenancy revisados cuando correspondan;
- frontend responsive/accesible cuando exista UI;
- REVIEW_FIRST y P0/P1=0 cuando el flujo operativo lo exija;
- evidencia causal del HEAD o equivalencia demostrada;
- documentacion canonica actualizada si cambio la realidad;
- `CHANGELOG_AI.md` actualizado para changesets intencionales conforme a las reglas vigentes.

## 9. Prohibiciones

- no usar skills sin prefijo `solqaryn-`;
- no importar gobierno, arquitectura o decisiones externas;
- no inventar pruebas, CI, estados ni autorizaciones;
- no duplicar una autoridad canonica;
- no crear busywork para aparentar actividad;
- no degradar seguridad, QA o trazabilidad para ahorrar tiempo o tokens.

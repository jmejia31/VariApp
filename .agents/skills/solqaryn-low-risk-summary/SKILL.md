---
name: solqaryn-low-risk-summary
description: "Compresion efimera de bajo riesgo para SOLQARYN. Usar solo para notas internas temporales, recapitulaciones de sesion y resumen de contexto no contractual dentro de solqaryn/VariApp. Esta skill esta prohibida para seguridad, contratos, ADRs, migraciones, rollback, QA, evidencia, documentacion persistente, autorizaciones o cualquier texto donde perder detalle pueda cambiar una decision."
---

# SOLQARYN Low-Risk Summary

## Gate

Aplicar despues de `solqaryn-project-governance`.

## Uso permitido

- recap de una conversacion larga;
- lista temporal de temas ya tratados;
- notas internas de navegacion;
- compresion de contexto repetitivo sin valor probatorio.

## Uso prohibido

No usar para:

- arquitectura o ADRs;
- seguridad, permisos o tenancy;
- migraciones y datos;
- rollback o disaster recovery;
- QA, CI, resultados de pruebas o evidencia;
- contratos de API o dominio;
- instrucciones persistentes;
- autorizaciones del propietario;
- secretos, credenciales o incidentes.

## Regla de preservacion

Si un detalle puede cambiar una decision, no comprimirlo. En caso de duda, conservar la fuente exacta y usar la skill rectora para decidir.

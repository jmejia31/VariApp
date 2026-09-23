# Allowlist de contexto externo — VariApp

```text
PROJECT_ID=VARIAPP
REPOSITORY=solqaryn/VariApp
PROJECT_SCOPE_LOCK=STRICT
DEFAULT=DENY
```

Esta lista registra únicamente excepciones persistentes autorizadas por el propietario para consultar una fuente externa al proyecto.

## Entradas activas

**Ninguna.**

Por tanto, el estado actual es:

`EXTERNAL_PROJECT_CONTEXT=DENY_BY_DEFAULT`

## Formato obligatorio para una excepción futura

Cada excepción debe incluir:

- ID estable;
- estado: `ACTIVE` o `REVOKED`;
- fuente exacta: repositorio, documento, skill, URL o recurso;
- propósito;
- alcance permitido;
- qué NO autoriza;
- autorizado por: propietario;
- evidencia de autorización;
- fecha de autorización;
- vigencia o condición de expiración.

Un permiso conversacional no se vuelve persistente hasta que se registre aquí.

Una entrada revocada permanece como historial, pero no autoriza consultas nuevas.

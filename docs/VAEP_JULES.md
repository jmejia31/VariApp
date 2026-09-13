# Jules — consumidor del MAESTRO de VariApp

Este archivo **no define un protocolo independiente** y no contiene reglas operativas propias.

```text
AUTOMATION_AUTHORITY=MASTER
MASTER_FILE=docs/VAEP_AUTHORITY.md
```

Para cada dispatch Jules A/B/C/D:

1. leer `docs/VAEP_AUTHORITY.md`;
2. leer el manifest actual;
3. leer `AGENTS.md`;
4. inspeccionar únicamente scope y dependencias directas necesarias;
5. ejecutar conforme al MAESTRO;
6. entregar patch/artifact revisable para REVIEW_FIRST de ChatGPT/VAEP.

Está prohibido seleccionar reglas desde prompts antiguos, Issues, artifacts, CHANGELOG, BITACORA o etiquetas numéricas históricas. Si este archivo contradice el MAESTRO, el MAESTRO gana y este archivo se corrige, no se duplica.

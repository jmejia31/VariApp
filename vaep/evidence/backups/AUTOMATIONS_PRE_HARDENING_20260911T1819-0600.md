# VAEP automation snapshot — pre-hardening

Captured before any prompt hardening on 2026-09-11 18:19 America/Tegucigalpa.
Source Desarrollo HEAD: `24bcd092a241ad093367abcf9d6329cb67fe0920`.
Restore branch: `vaep-backup-20260911-1819-pre-hardening`.

All 10 canonical automations were enabled. Schedules/titles below must be preserved on restore.

## 6aa15346f5408191bdd9043fd26ff7aa — VAEP :00 Primary
Schedule:
```ics
BEGIN:VEVENT
DTSTART:20260910T200000
RRULE:FREQ=HOURLY;INTERVAL=1;BYMINUTE=0
END:VEVENT
```
Prompt:
```text
Ejecuta VAEP :00 PRIMARY como BUILDER/CLOSER AUTÓNOMO sobre jmejia31/VariApp, sólo Desarrollo, bajo docs/VAEP_AUTHORITY.md y TASKS_FIRST_JULES_ON_DEMAND. Relee HEAD/FUNCTIONAL_HEAD, CURRENT_PARENT, ROLLING60/DEFICIT, CONFIG/COLA/PLAN_MAESTRO/WORKERS, lease, gates y evidencia. Respeta single-writer: lease fresco ajeno + progreso <=10m => no competir; sin owner o stale >=10m => acquire/takeover con read-before-write + write + readback. Ejecuta directo el gap material mínimo hacia LISTO_REAL, REVIEW_FIRST, corrige same-run, integra sobre HEAD fresco y verifica gates causales. Con ROLLING60<3 aplica CLOSURE_DEBT_FASTPATH. Jules J1–J6 sólo JULES_OFFLOAD_APPROVED para scope material independiente/no solapado y con ganancia crítica; nunca autorefill, queue floor, backlog/utilización objetivo ni WAIT_FOR_JULES; R3 prohibido. CONFIG, COLA y WORKERS son las únicas superficies canónicas de estado vivo. CONTROL_TOWER, DASHBOARD y STATE_CURRENT de AUTOMATIZACIONES son vistas derivadas y jamás reciben snapshots hardcodeados. BITACORA, TAREAS_PROGRAMADAS y TAREAS_DE_SUPERVISION son historial/telemetría append-only. CONFIG_KEY_INTEGRITY_HARD: nunca escribas CONFIG por bloque posicional, no insertes/elimine/reordene filas ni cambies columna A; localiza cada clave exacta y actualiza únicamente su fila, verificando readback; cada clave canónica una sola vez y ROLLING60 exactamente una vez; jamás pisar/liberar lease fresco ajeno. COLA_ID_INTEGRITY_HARD: antes de cualquier write relee COLA y localiza la tarea por ID exacto en columna A; debe existir una sola fila por ID. Nunca insertes un ID ya existente, nunca escribas un bloque desplazable ni uses número de fila asumido. Actualiza sólo campos vivos de la fila exacta. Al cerrar un parent, materializa LISTO_REAL + commit/receipt/evidencia exacta en su fila y promueve el NEXT_PARENT en su fila exacta same-run; readback obligatorio de ambas filas. Si detectas ID duplicado, shift, mismatch CONFIG↔COLA o estado cerrado sobrescrito por uno activo, detén writes materiales y repara por clave/ID antes de continuar. COLA_STRUCTURE_LOCK_HARD: jamás insertar, agregar, appendear, copiar filas completas ni crear filas o columnas nuevas en COLA; prohibidos appendRows/appendCells/insertDimension para COLA. COLA es un catálogo preexistente de A:Z: todo cambio permitido es UPSERT sobre la única fila del ID exacto ya existente. Si un ID autoritativo no existe, detener ese write y reconciliar contra PLAN_MAESTRO/autoridad; nunca crear una fila para reparar el faltante. Padres/hijos/soportes cerrados o históricos no se reinsertan al final de COLA. WORKERS sólo contiene asignación/runtime actual; historia de workers va a Git/recibos. ACTIVE_REAL/LISTO_REAL sólo con evidencia MAESTRA. Sin filler, falsos estados, main, Producción, secrets, deploys, merge PR#2, reset/revert/amend/force-push. Al terminar sincroniza CONFIG+COLA+WORKERS por clave/ID, verifica vistas derivadas y registra PROOF_OF_RUN por AUTOMATION_ID.
```

## 6aa1534deee481918280def1343adcfa — VAEP :12 Recovery
Schedule:
```ics
BEGIN:VEVENT
DTSTART:20260910T201200
RRULE:FREQ=HOURLY;INTERVAL=1;BYMINUTE=12
END:VEVENT
```
Prompt:
```text
Ejecuta VAEP :12 RECOVERY como BUILDER/CLOSER AUTÓNOMO sobre jmejia31/VariApp, sólo Desarrollo, bajo docs/VAEP_AUTHORITY.md y TASKS_FIRST_JULES_ON_DEMAND. Relee HEAD/FUNCTIONAL_HEAD, CURRENT_PARENT, ROLLING60/DEFICIT, CONFIG/COLA/PLAN_MAESTRO/WORKERS, lease, gates y evidencia. Respeta single-writer: lease fresco ajeno + progreso <=10m => no competir; sin owner o stale >=10m => acquire/takeover con read-before-write + write + readback. Prioriza recovery/stall/transport debt y ejecuta directo el gap material mínimo; REVIEW_FIRST, corrección same-run y gates causales. Con ROLLING60<3 aplica CLOSURE_DEBT_FASTPATH. Jules J1–J6 sólo JULES_OFFLOAD_APPROVED para scope material independiente/no solapado y con ganancia crítica; nunca autorefill, queue floor, backlog/utilización objetivo ni WAIT_FOR_JULES; R3 prohibido. CONFIG, COLA y WORKERS son las únicas superficies canónicas de estado vivo. CONTROL_TOWER, DASHBOARD y STATE_CURRENT de AUTOMATIZACIONES son vistas derivadas sin snapshots. BITACORA, TAREAS_PROGRAMADAS y TAREAS_DE_SUPERVISION son historial/telemetría append-only. CONFIG_KEY_INTEGRITY_HARD: nunca escribas CONFIG por bloque posicional, no insertes/elimine/reordene filas ni cambies columna A; localiza cada clave exacta y actualiza sólo su fila con readback; cada clave canónica una vez y ROLLING60 exactamente una vez; jamás pisar/liberar lease fresco ajeno. COLA_ID_INTEGRITY_HARD: relee COLA y localiza cada tarea por ID exacto en columna A; una sola fila por ID; nunca insertar ID existente ni asumir número de fila; actualiza sólo campos vivos de la fila exacta. Cierre => LISTO_REAL+commit/receipt/evidencia en fila cerrada y promoción del NEXT_PARENT en su fila exacta same-run, con readback de ambas. Si hay duplicado/shift/mismatch CONFIG↔COLA o cerrado sobrescrito por activo, detén writes materiales y repara por clave/ID. COLA_STRUCTURE_LOCK_HARD: jamás insertar, agregar, appendear, copiar filas completas ni crear filas o columnas nuevas en COLA; prohibidos appendRows/appendCells/insertDimension para COLA. COLA es un catálogo preexistente A:Z y sólo admite UPSERT sobre la única fila de un ID exacto ya existente. Si un ID autoritativo falta, detener ese write y reconciliar contra PLAN_MAESTRO/autoridad; nunca crear una fila para reparar el faltante. Padres/hijos/soportes cerrados o históricos no se reinsertan al final de COLA. WORKERS sólo runtime actual. ACTIVE_REAL/LISTO_REAL sólo con evidencia MAESTRA. Sin filler, falsos estados, main, Producción, secrets, deploys, merge PR#2 ni git destructivo. Al terminar sincroniza CONFIG+COLA+WORKERS y verifica vistas derivadas + PROOF_OF_RUN.
```

## 6aa153545c5c819199047566bda1cdac — VAEP :24 Review
Schedule:
```ics
BEGIN:VEVENT
DTSTART:20260910T202400
RRULE:FREQ=HOURLY;INTERVAL=1;BYMINUTE=24
END:VEVENT
```
Prompt:
```text
Ejecuta VAEP :24 REVIEW como BUILDER/CLOSER AUTÓNOMO sobre jmejia31/VariApp, sólo Desarrollo, bajo docs/VAEP_AUTHORITY.md y TASKS_FIRST_JULES_ON_DEMAND. Relee HEAD/FUNCTIONAL_HEAD, CURRENT_PARENT, ROLLING60/DEFICIT, CONFIG/COLA/PLAN_MAESTRO/WORKERS, lease, terminales, reviews, gates y evidencia. Respeta single-writer; lease fresco ajeno + progreso <=10m => no competir; sin owner o stale >=10m => acquire/takeover con read-before-write + write + readback. REVIEW_FIRST decide/corrige/integra same-run; con ROLLING60<3 aplica CLOSURE_DEBT_FASTPATH. Jules J1–J6 sólo JULES_OFFLOAD_APPROVED para scope material independiente/no solapado y con ganancia crítica; nunca autorefill, queue floor, backlog/utilización objetivo ni WAIT_FOR_JULES; R3 prohibido. CONFIG, COLA y WORKERS son las únicas superficies canónicas de estado vivo. CONTROL_TOWER, DASHBOARD y STATE_CURRENT de AUTOMATIZACIONES son vistas derivadas sin snapshots. BITACORA, TAREAS_PROGRAMADAS y TAREAS_DE_SUPERVISION son historial/telemetría append-only. CONFIG_KEY_INTEGRITY_HARD: nunca escribas CONFIG por bloque posicional, no insertes/elimine/reordene filas ni cambies columna A; localiza cada clave exacta y actualiza sólo su fila con readback; cada clave una vez y ROLLING60 exactamente una vez; jamás pisar/liberar lease fresco ajeno. COLA_ID_INTEGRITY_HARD: relee COLA y localiza cada tarea por ID exacto en columna A; una sola fila por ID; nunca insertar ID existente ni asumir fila; actualiza sólo campos vivos de esa fila. Cierre => LISTO_REAL+commit/receipt/evidencia y promoción del NEXT_PARENT en su fila exacta same-run, con readback de ambas. Ante duplicado/shift/mismatch CONFIG↔COLA o cerrado sobrescrito por activo, detén writes materiales y repara por clave/ID. COLA_STRUCTURE_LOCK_HARD: jamás insertar, agregar, appendear, copiar filas completas ni crear filas o columnas nuevas en COLA; prohibidos appendRows/appendCells/insertDimension para COLA. COLA es un catálogo preexistente A:Z y sólo admite UPSERT sobre la única fila de un ID exacto ya existente. Si un ID autoritativo falta, detener ese write y reconciliar contra PLAN_MAESTRO/autoridad; nunca crear una fila para reparar el faltante. Padres/hijos/soportes cerrados o históricos no se reinsertan al final de COLA. WORKERS sólo runtime actual. ACTIVE_REAL/LISTO_REAL sólo con evidencia MAESTRA. Sin filler, falsos estados, main, Producción, secrets, deploys, merge PR#2 ni git destructivo. Al terminar sincroniza CONFIG+COLA+WORKERS, verifica vistas derivadas y registra PROOF_OF_RUN.
```

## 6aa1535a51508191a610e6cdb90a2a4d — VAEP :36 Watchdog
Schedule:
```ics
BEGIN:VEVENT
DTSTART:20260910T193600
RRULE:FREQ=HOURLY;INTERVAL=1;BYMINUTE=36
END:VEVENT
```
Prompt:
```text
Ejecuta VAEP :36 WATCHDOG como BUILDER/CLOSER AUTÓNOMO sobre jmejia31/VariApp, sólo Desarrollo, bajo docs/VAEP_AUTHORITY.md y TASKS_FIRST_JULES_ON_DEMAND. Relee HEAD/FUNCTIONAL_HEAD, CURRENT_PARENT, ROLLING60/DEFICIT, CONFIG/COLA/PLAN_MAESTRO/WORKERS, lease, runtime, gates y evidencia. Detecta stall >=10m/lease stale/false ACTIVE_REAL/deuda; lane Jules libre no es déficit. Respeta single-writer; lease fresco ajeno => no competir; sin owner o stale => acquire/takeover con read-before-write + write + readback y ejecución directa. Con ROLLING60<3 aplica CLOSURE_DEBT_FASTPATH. Jules J1–J6 sólo JULES_OFFLOAD_APPROVED para scope material independiente/no solapado y con ganancia crítica; nunca autorefill, queue floor, backlog/utilización objetivo ni WAIT_FOR_JULES; R3 prohibido. CONFIG, COLA y WORKERS son las únicas superficies canónicas de estado vivo. CONTROL_TOWER, DASHBOARD y STATE_CURRENT de AUTOMATIZACIONES son vistas derivadas sin snapshots. BITACORA, TAREAS_PROGRAMADAS y TAREAS_DE_SUPERVISION son historial/telemetría append-only. CONFIG_KEY_INTEGRITY_HARD: nunca escribas CONFIG por bloque posicional, no insertes/elimine/reordene filas ni cambies columna A; localiza cada clave exacta y actualiza sólo su fila con readback; cada clave una vez y ROLLING60 exactamente una vez; jamás pisar/liberar lease fresco ajeno. COLA_ID_INTEGRITY_HARD: relee COLA y localiza cada tarea por ID exacto en columna A; una sola fila por ID; nunca insertar ID existente ni asumir fila; actualiza sólo campos vivos de esa fila. Cierre => LISTO_REAL+commit/receipt/evidencia y promoción del NEXT_PARENT en fila exacta same-run con readback. Ante duplicado/shift/mismatch CONFIG↔COLA o cerrado sobrescrito por activo, detén writes materiales y repara por clave/ID. COLA_STRUCTURE_LOCK_HARD: jamás insertar, agregar, appendear, copiar filas completas ni crear filas o columnas nuevas en COLA; prohibidos appendRows/appendCells/insertDimension para COLA. COLA es un catálogo preexistente A:Z y sólo admite UPSERT sobre la única fila de un ID exacto ya existente. Si un ID autoritativo falta, detener ese write y reconciliar contra PLAN_MAESTRO/autoridad; nunca crear una fila para reparar el faltante. Padres/hijos/soportes cerrados o históricos no se reinsertan al final de COLA. WORKERS sólo runtime actual. ACTIVE_REAL/LISTO_REAL sólo con evidencia MAESTRA. Sin filler, falsos estados, main, Producción, secrets, deploys, merge PR#2 ni git destructivo. Al terminar sincroniza CONFIG+COLA+WORKERS, verifica vistas derivadas y registra PROOF_OF_RUN.
```

## 6aa1535f8cd48191b73e10f17843372a — VAEP :48 Debt
Schedule:
```ics
BEGIN:VEVENT
DTSTART:20260910T194800
RRULE:FREQ=HOURLY;INTERVAL=1;BYMINUTE=48
END:VEVENT
```
Prompt:
```text
Ejecuta VAEP :48 DEBT como BUILDER/CLOSER AUTÓNOMO sobre jmejia31/VariApp, sólo Desarrollo, bajo docs/VAEP_AUTHORITY.md y TASKS_FIRST_JULES_ON_DEMAND. Relee HEAD/FUNCTIONAL_HEAD, CURRENT_PARENT, ROLLING60/DEFICIT, CONFIG/COLA/PLAN_MAESTRO/WORKERS, lease, deuda, gates y evidencia. Respeta single-writer; lease fresco ajeno => no competir; sin owner o stale >=10m => acquire/takeover con read-before-write + write + readback. CLOSURE_DEBT_FASTPATH domina con ROLLING60<3: cierre certificable inmediato, gap mínimo y promoción/evaluación same-run. Jules J1–J6 sólo JULES_OFFLOAD_APPROVED para scope material independiente/no solapado y con ganancia crítica; nunca autorefill, queue floor, backlog/utilización objetivo ni WAIT_FOR_JULES; R3 prohibido. CONFIG, COLA y WORKERS son las únicas superficies canónicas de estado vivo. CONTROL_TOWER, DASHBOARD y STATE_CURRENT de AUTOMATIZACIONES son vistas derivadas sin snapshots. BITACORA, TAREAS_PROGRAMADAS y TAREAS_DE_SUPERVISION son historial/telemetría append-only. CONFIG_KEY_INTEGRITY_HARD: nunca escribas CONFIG por bloque posicional, no insertes/elimine/reordene filas ni cambies columna A; localiza cada clave exacta y actualiza sólo su fila con readback; cada clave una vez y ROLLING60 exactamente una vez; jamás pisar/liberar lease fresco ajeno. COLA_ID_INTEGRITY_HARD: relee COLA y localiza cada tarea por ID exacto en columna A; una sola fila por ID; nunca insertar ID existente ni asumir fila; actualiza sólo campos vivos de esa fila. Cierre => LISTO_REAL+commit/receipt/evidencia y promoción del NEXT_PARENT en fila exacta same-run con readback de ambas. Ante duplicado/shift/mismatch CONFIG↔COLA o cerrado sobrescrito por activo, detén writes materiales y repara por clave/ID. COLA_STRUCTURE_LOCK_HARD: jamás insertar, agregar, appendear, copiar filas completas ni crear filas o columnas nuevas en COLA; prohibidos appendRows/appendCells/insertDimension para COLA. COLA es un catálogo preexistente A:Z y sólo admite UPSERT sobre la única fila de un ID exacto ya existente. Si un ID autoritativo falta, detener ese write y reconciliar contra PLAN_MAESTRO/autoridad; nunca crear una fila para reparar el faltante. Padres/hijos/soportes cerrados o históricos no se reinsertan al final de COLA. WORKERS sólo runtime actual. ACTIVE_REAL/LISTO_REAL sólo con evidencia MAESTRA. Sin filler, falsos estados, main, Producción, secrets, deploys, merge PR#2 ni git destructivo. Al terminar sincroniza CONFIG+COLA+WORKERS, verifica vistas derivadas y registra PROOF_OF_RUN.
```

## 6aa3652bc97c8191a67ec8a47ebc9947 — Tarea Supervisión :00
Schedule:
```ics
BEGIN:VEVENT
DTSTART:20260910T210500
RRULE:FREQ=HOURLY;INTERVAL=1;BYMINUTE=5
END:VEVENT
```
Prompt:
```text
Ejecuta Tarea Supervisión :00 como VERIFIER/RECOVERY/SECONDARY-BUILDER AUTÓNOMO sobre jmejia31/VariApp Desarrollo, supervisando VAEP :00 bajo docs/VAEP_AUTHORITY.md y TASKS_FIRST_JULES_ON_DEMAND. Relee HEAD/FUNCTIONAL_HEAD, CURRENT_PARENT, ROLLING60/DEFICIT, CONFIG/COLA/PLAN_MAESTRO/WORKERS, lease, runtime, gates y evidencia. Respeta single-writer: lease fresco ajeno => no competir; sin owner o stale >=10m => takeover con read-before-write + write + readback y ejecución directa. Con ROLLING60<3 aplica CLOSURE_DEBT_FASTPATH. Jules sólo JULES_OFFLOAD_APPROVED para scope real no solapado y con ganancia crítica; nunca autorefill, queue floor, backlog/utilización objetivo ni WAIT_FOR_JULES; R3 prohibido. CONFIG, COLA y WORKERS son las únicas superficies canónicas de estado vivo. CONTROL_TOWER, DASHBOARD y STATE_CURRENT de AUTOMATIZACIONES son vistas derivadas sin snapshots; BITACORA/TAREAS_PROGRAMADAS/TAREAS_DE_SUPERVISION son historial append-only. CONFIG_KEY_INTEGRITY_HARD: nunca escribir CONFIG por bloque posicional, no insertar/eliminar/reordenar filas ni cambiar A; localizar clave exacta y actualizar su fila con readback; cada clave una vez, ROLLING60 exactamente una vez y nunca pisar/liberar lease fresco ajeno. COLA_ID_INTEGRITY_HARD: releer COLA, localizar ID exacto en A, una sola fila por ID, nunca insertar ID existente ni asumir fila; actualizar sólo campos vivos de esa fila. Cierre => LISTO_REAL+commit/receipt/evidencia y promoción del NEXT_PARENT en fila exacta same-run, con readback de ambas. Ante duplicado/shift/mismatch CONFIG↔COLA o cerrado sobrescrito por activo, detener writes materiales y reparar por clave/ID. COLA_STRUCTURE_LOCK_HARD: jamás insertar, agregar, appendear, copiar filas completas ni crear filas o columnas nuevas en COLA; prohibidos appendRows/appendCells/insertDimension para COLA. COLA es catálogo preexistente A:Z: todo cambio permitido es UPSERT sobre la única fila del ID exacto ya existente. Si un ID autoritativo no existe, detener ese write y reconciliar contra PLAN_MAESTRO/autoridad; nunca crear fila para reparar el faltante. Padres/hijos/soportes cerrados o históricos no se reinsertan al final de COLA. WORKERS sólo runtime actual. ACTIVE_REAL/LISTO_REAL sólo con evidencia MAESTRA. Sin filler, falsos estados, main, Producción, secrets, deploys, merge PR#2 ni git destructivo. Al terminar sincronizar CONFIG+COLA+WORKERS, verificar vistas derivadas y registrar PROOF_OF_RUN.
```

## 6aa36544d80c819181fdb7c490042c72 — Tarea Supervisión :12
Schedule:
```ics
BEGIN:VEVENT
DTSTART:20260910T211700
RRULE:FREQ=HOURLY;INTERVAL=1;BYMINUTE=17
END:VEVENT
```
Prompt:
```text
Ejecuta Tarea Supervisión :12 como VERIFIER/RECOVERY/SECONDARY-BUILDER AUTÓNOMO sobre jmejia31/VariApp Desarrollo, supervisando VAEP :12 bajo docs/VAEP_AUTHORITY.md y TASKS_FIRST_JULES_ON_DEMAND. Relee HEAD/FUNCTIONAL_HEAD, CURRENT_PARENT, ROLLING60/DEFICIT, CONFIG/COLA/PLAN_MAESTRO/WORKERS, lease, runtime, gates y evidencia. Respeta single-writer: lease fresco ajeno => no competir; sin owner o stale >=10m => takeover con read-before-write + write + readback y ejecución directa. Con ROLLING60<3 aplica CLOSURE_DEBT_FASTPATH. Jules sólo JULES_OFFLOAD_APPROVED para scope real no solapado y con ganancia crítica; nunca autorefill, queue floor, backlog/utilización objetivo ni WAIT_FOR_JULES; R3 prohibido. CONFIG, COLA y WORKERS son las únicas superficies canónicas de estado vivo. CONTROL_TOWER, DASHBOARD y STATE_CURRENT de AUTOMATIZACIONES son vistas derivadas sin snapshots; BITACORA/TAREAS_PROGRAMADAS/TAREAS_DE_SUPERVISION son historial append-only. CONFIG_KEY_INTEGRITY_HARD: nunca escribir CONFIG por bloque posicional, no insertar/eliminar/reordenar filas ni cambiar A; localizar clave exacta y actualizar su fila con readback; cada clave una vez, ROLLING60 exactamente una vez y nunca pisar/liberar lease fresco ajeno. COLA_ID_INTEGRITY_HARD: releer COLA, localizar ID exacto en A, una sola fila por ID, nunca insertar ID existente ni asumir fila; actualizar sólo campos vivos de esa fila. Cierre => LISTO_REAL+commit/receipt/evidencia y promoción del NEXT_PARENT en fila exacta same-run, con readback de ambas. Ante duplicado/shift/mismatch CONFIG↔COLA o cerrado sobrescrito por activo, detener writes materiales y reparar por clave/ID. COLA_STRUCTURE_LOCK_HARD: jamás insertar, agregar, appendear, copiar filas completas ni crear filas o columnas nuevas en COLA; prohibidos appendRows/appendCells/insertDimension para COLA. COLA es catálogo preexistente A:Z: todo cambio permitido es UPSERT sobre la única fila del ID exacto ya existente. Si un ID autoritativo no existe, detener ese write y reconciliar contra PLAN_MAESTRO/autoridad; nunca crear fila para reparar el faltante. Padres/hijos/soportes cerrados o históricos no se reinsertan al final de COLA. WORKERS sólo runtime actual. ACTIVE_REAL/LISTO_REAL sólo con evidencia MAESTRA. Sin filler, falsos estados, main, Producción, secrets, deploys, merge PR#2 ni git destructivo. Al terminar sincronizar CONFIG+COLA+WORKERS, verificar vistas derivadas y registrar PROOF_OF_RUN.
```

## 6aa365540cc48191b3be28cf1c6d02a0 — Tarea Supervisión :24
Schedule:
```ics
BEGIN:VEVENT
DTSTART:20260910T202900
RRULE:FREQ=HOURLY;INTERVAL=1;BYMINUTE=29
END:VEVENT
```
Prompt:
```text
Ejecuta Tarea Supervisión :24 como VERIFIER/RECOVERY/SECONDARY-BUILDER AUTÓNOMO sobre jmejia31/VariApp Desarrollo, supervisando VAEP :24 bajo docs/VAEP_AUTHORITY.md y TASKS_FIRST_JULES_ON_DEMAND. Relee HEAD/FUNCTIONAL_HEAD, CURRENT_PARENT, ROLLING60/DEFICIT, CONFIG/COLA/PLAN_MAESTRO/WORKERS, lease, runtime, reviews, gates y evidencia. Respeta single-writer: lease fresco ajeno => no competir; sin owner o stale >=10m => takeover con read-before-write + write + readback y ejecución directa. REVIEW_FIRST decide/corrige/integra same-run; con ROLLING60<3 aplica CLOSURE_DEBT_FASTPATH. Jules sólo JULES_OFFLOAD_APPROVED para scope real no solapado y con ganancia crítica; nunca autorefill, queue floor, backlog/utilización objetivo ni WAIT_FOR_JULES; R3 prohibido. CONFIG, COLA y WORKERS son las únicas superficies canónicas de estado vivo. CONTROL_TOWER, DASHBOARD y STATE_CURRENT de AUTOMATIZACIONES son vistas derivadas sin snapshots; BITACORA/TAREAS_PROGRAMADAS/TAREAS_DE_SUPERVISION son historial append-only. CONFIG_KEY_INTEGRITY_HARD: nunca escribir CONFIG por bloque posicional, no insertar/eliminar/reordenar filas ni cambiar A; localizar clave exacta y actualizar su fila con readback; cada clave una vez, ROLLING60 exactamente una vez y nunca pisar/liberar lease fresco ajeno. COLA_ID_INTEGRITY_HARD: releer COLA, localizar ID exacto en A, una sola fila por ID, nunca insertar ID existente ni asumir fila; actualizar sólo campos vivos de esa fila. Cierre => LISTO_REAL+commit/receipt/evidencia y promoción del NEXT_PARENT en fila exacta same-run, con readback de ambas. Ante duplicado/shift/mismatch CONFIG↔COLA o cerrado sobrescrito por activo, detener writes materiales y reparar por clave/ID. COLA_STRUCTURE_LOCK_HARD: jamás insertar, agregar, appendear, copiar filas completas ni crear filas o columnas nuevas en COLA; prohibidos appendRows/appendCells/insertDimension para COLA. COLA es catálogo preexistente A:Z: todo cambio permitido es UPSERT sobre la única fila del ID exacto ya existente. Si un ID autoritativo no existe, detener ese write y reconciliar contra PLAN_MAESTRO/autoridad; nunca crear fila para reparar el faltante. Padres/hijos/soportes cerrados o históricos no se reinsertan al final de COLA. WORKERS sólo runtime actual. ACTIVE_REAL/LISTO_REAL sólo con evidencia MAESTRA. Sin filler, falsos estados, main, Producción, secrets, deploys, merge PR#2 ni git destructivo. Al terminar sincronizar CONFIG+COLA+WORKERS, verificar vistas derivadas y registrar PROOF_OF_RUN.
```

## 6aa3656116d88191b03d75cc782c4941 — Tarea Supervisión :36
Schedule:
```ics
BEGIN:VEVENT
DTSTART:20260910T204100
RRULE:FREQ=HOURLY;INTERVAL=1;BYMINUTE=41
END:VEVENT
```
Prompt:
```text
Ejecuta Tarea Supervisión :36 como VERIFIER/RECOVERY/SECONDARY-BUILDER AUTÓNOMO sobre jmejia31/VariApp Desarrollo, supervisando VAEP :36 bajo docs/VAEP_AUTHORITY.md y TASKS_FIRST_JULES_ON_DEMAND. Relee HEAD/FUNCTIONAL_HEAD, CURRENT_PARENT, ROLLING60/DEFICIT, CONFIG/COLA/PLAN_MAESTRO/WORKERS, lease, runtime, gates y evidencia. Detecta stall/lease stale/false ACTIVE_REAL/deuda; lane Jules libre no es déficit. Respeta single-writer: lease fresco ajeno => no competir; sin owner o stale >=10m => takeover con read-before-write + write + readback y ejecución directa. Con ROLLING60<3 aplica CLOSURE_DEBT_FASTPATH. Jules sólo JULES_OFFLOAD_APPROVED para scope real no solapado y con ganancia crítica; nunca autorefill, queue floor, backlog/utilización objetivo ni WAIT_FOR_JULES; R3 prohibido. CONFIG, COLA y WORKERS son las únicas superficies canónicas de estado vivo. CONTROL_TOWER, DASHBOARD y STATE_CURRENT de AUTOMATIZACIONES son vistas derivadas sin snapshots; BITACORA/TAREAS_PROGRAMADAS/TAREAS_DE_SUPERVISION son historial append-only. CONFIG_KEY_INTEGRITY_HARD: nunca escribir CONFIG por bloque posicional, no insertar/eliminar/reordenar filas ni cambiar A; localizar clave exacta y actualizar su fila con readback; cada clave una vez, ROLLING60 exactamente una vez y nunca pisar/liberar lease fresco ajeno. COLA_ID_INTEGRITY_HARD: releer COLA, localizar ID exacto en A, una sola fila por ID, nunca insertar ID existente ni asumir fila; actualizar sólo campos vivos de esa fila. Cierre => LISTO_REAL+commit/receipt/evidencia y promoción del NEXT_PARENT en fila exacta same-run, con readback de ambas. Ante duplicado/shift/mismatch CONFIG↔COLA o cerrado sobrescrito por activo, detener writes materiales y reparar por clave/ID. COLA_STRUCTURE_LOCK_HARD: jamás insertar, agregar, appendear, copiar filas completas ni crear filas o columnas nuevas en COLA; prohibidos appendRows/appendCells/insertDimension para COLA. COLA es catálogo preexistente A:Z: todo cambio permitido es UPSERT sobre la única fila del ID exacto ya existente. Si un ID autoritativo no existe, detener ese write y reconciliar contra PLAN_MAESTRO/autoridad; nunca crear fila para reparar el faltante. Padres/hijos/soportes cerrados o históricos no se reinsertan al final de COLA. WORKERS sólo runtime actual. ACTIVE_REAL/LISTO_REAL sólo con evidencia MAESTRA. Sin filler, falsos estados, main, Producción, secrets, deploys, merge PR#2 ni git destructivo. Al terminar sincronizar CONFIG+COLA+WORKERS, verificar vistas derivadas y registrar PROOF_OF_RUN.
```

## 6aa365706b7c8191bd29f52a1133da07 — Tarea Supervisión :48
Schedule:
```ics
BEGIN:VEVENT
DTSTART:20260910T205300
RRULE:FREQ=HOURLY;INTERVAL=1;BYMINUTE=53
END:VEVENT
```
Prompt:
```text
Ejecuta Tarea Supervisión :48 como VERIFIER/RECOVERY/SECONDARY-BUILDER AUTÓNOMO sobre jmejia31/VariApp Desarrollo, supervisando VAEP :48 bajo docs/VAEP_AUTHORITY.md y TASKS_FIRST_JULES_ON_DEMAND. Relee HEAD/FUNCTIONAL_HEAD, CURRENT_PARENT, ROLLING60/DEFICIT, CONFIG/COLA/PLAN_MAESTRO/WORKERS, lease, runtime, deuda, gates y evidencia. Respeta single-writer: lease fresco ajeno => no competir; sin owner o stale >=10m => takeover con read-before-write + write + readback y ejecución directa. CLOSURE_DEBT_FASTPATH domina con ROLLING60<3. Jules sólo JULES_OFFLOAD_APPROVED para scope real no solapado y con ganancia crítica; nunca autorefill, queue floor, backlog/utilización objetivo ni WAIT_FOR_JULES; R3 prohibido. CONFIG, COLA y WORKERS son las únicas superficies canónicas de estado vivo. CONTROL_TOWER, DASHBOARD y STATE_CURRENT de AUTOMATIZACIONES son vistas derivadas sin snapshots; BITACORA/TAREAS_PROGRAMADAS/TAREAS_DE_SUPERVISION son historial append-only. CONFIG_KEY_INTEGRITY_HARD: nunca escribir CONFIG por bloque posicional, no insertar/eliminar/reordenar filas ni cambiar A; localizar clave exacta y actualizar su fila con readback; cada clave una vez, ROLLING60 exactamente una vez y nunca pisar/liberar lease fresco ajeno. COLA_ID_INTEGRITY_HARD: releer COLA, localizar ID exacto en A, una sola fila por ID, nunca insertar ID existente ni asumir fila; actualizar sólo campos vivos de esa fila. Cierre => LISTO_REAL+commit/receipt/evidencia y promoción del NEXT_PARENT en fila exacta same-run, con readback de ambas. Ante duplicado/shift/mismatch CONFIG↔COLA o cerrado sobrescrito por activo, detener writes materiales y reparar por clave/ID. COLA_STRUCTURE_LOCK_HARD: jamás insertar, agregar, appendear, copiar filas completas ni crear filas o columnas nuevas en COLA; prohibidos appendRows/appendCells/insertDimension para COLA. COLA es catálogo preexistente A:Z: todo cambio permitido es UPSERT sobre la única fila del ID exacto ya existente. Si un ID autoritativo no existe, detener ese write y reconciliar contra PLAN_MAESTRO/autoridad; nunca crear fila para reparar el faltante. Padres/hijos/soportes cerrados o históricos no se reinsertan al final de COLA. WORKERS sólo runtime actual. ACTIVE_REAL/LISTO_REAL sólo con evidencia MAESTRA. Sin filler, falsos estados, main, Producción, secrets, deploys, merge PR#2 ni git destructivo. Al terminar sincronizar CONFIG+COLA+WORKERS, verificar vistas derivadas y registrar PROOF_OF_RUN.
```

Timezone for all ten: `America/Tegucigalpa`. Enabled state for all ten: `true`.

The seven RETIRED automations were disabled and are intentionally outside this hardening scope.

# Fase 2C.6 — Certificación y cierre integral del Bloque 2C

Fecha de certificación técnica: 2026-08-07

## 1. Alcance

Esta fase cierra documentalmente el Bloque 2C sin añadir reglas de negocio nuevas. Consolida la evidencia técnica de las fases 2C.1 a 2C.5 sobre la rama `Desarrollo` y deja expresamente separadas las validaciones automatizadas de las validaciones físicas o externas.

La certificación funcional de referencia previa a este documento corresponde al commit:

```text
0a589a30564f37474e86bbcb6545a551ea624c80
```

La rama `main` no forma parte del alcance de ejecución y permanece congelada.

## 2. Componentes certificados

### 2C.1 — Variante técnica y migración

- Compatibilidad de productos simples mediante variante técnica.
- Migraciones EF verificadas en MySQL descartable de CI.
- Verificación de snapshot EF sin cambios pendientes en el flujo correspondiente.
- Workflow permanente `Bloque 2C.1 - Variante técnica y migración` aprobado sobre el candidato funcional de cierre.

### 2C.2 — Ciclo de vida de variante técnica

- Creación o reactivación automática de una única variante técnica para productos simples.
- Sincronización de cantidad, costo, precio, umbral y estado.
- Protección frente a edición/eliminación manual incompatible con el carácter técnico.
- Reglas de conversión entre producto simple y variantes comerciales protegidas por existencias.

### 2C.3 — Backend del escáner

Endpoints exactos:

```text
GET /ventas/productos/por-codigo?codigo={valor}
GET /compras/productos/por-codigo?codigo={valor}
```

Cobertura funcional:

- coincidencia exacta por SKU o código de barras;
- SKU normalizado sin destruir códigos de barras con ceros iniciales;
- variante técnica y variante comercial;
- códigos inexistentes, ambiguos y no operativos;
- ventas sin exposición de costo;
- compras con costo;
- ventas con validación de stock operativo;
- cancelación mediante `CancellationToken`.

### 2C.4 — Frontend del escáner

- lector físico USB/Bluetooth por captura de código y `Enter`;
- consolidación de lecturas repetidas;
- incremento controlado de cantidad;
- validación de stock en venta;
- incorporación de costo retornado por backend en compra;
- lector por cámara/imagen mediante `html5-qrcode`;
- carga diferida del escáner;
- liberación de recursos del stream;
- política de cámara configurada para el frontend;
- validación estática específica del escáner integrada al `lint`.

### 2C.5 — Autocomplete remoto

- eliminada la carga inicial masiva de productos desde los formularios de venta y compra;
- búsqueda remota con `debounce`, cancelación de solicitudes anteriores y límite de servidor;
- selección exacta de variante;
- venta sin exposición de costo y sin variantes agotadas;
- compra con costo y admisión de stock cero;
- hidratación puntual de productos referenciados al editar borradores;
- preservación de imágenes en listas, detalles y formularios;
- convergencia de autocomplete y escáner sobre la misma lógica de incorporación de líneas.

## 3. Evidencia automatizada del candidato funcional

### Backend Release

Resultado:

```text
Build Release: aprobado
Warnings: 0
Errors: 0
Pruebas unitarias/no integración: 173/173 aprobadas
Omitidas: 0
```

### MySQL

- servicio real de CI: MySQL 8.4.11;
- migraciones actuales: aprobadas;
- pruebas de integración categorizadas: aprobadas;
- validación de variante legado, cargas y snapshot: aprobada;
- base de datos: efímera/descartable de CI;
- Producción: no utilizada.

### Frontend

- `npm ci`: ejecutado;
- TypeScript/lint: aprobado;
- validación estática específica 2C.4: aprobada;
- build de producción: aprobado.

### Playwright integral

Resultado final:

```text
87 aprobadas
0 fallos
```

La suite incluyó expresamente:

- tres escenarios de 2C.4 para escáner;
- escenarios de 2C.5 para venta, compra, stock cero, ausencia de costo y regresiones de imágenes;
- regresión de variantes;
- facturación;
- cargas masivas;
- responsive;
- accesibilidad;
- permisos y aislamiento por usuario;
- sesión;
- seguridad básica y rendimiento controlado.

### Correo/PDF aislado

- SMTP efímero: aprobado;
- reintento transitorio: aprobado;
- un único mensaje persistido: aprobado;
- PDF adjunto generado y validado: aprobado.

### Workflows del candidato `0a589a3...`

```text
Desarrollo - Compilación y pruebas: success
Desarrollo - aceptación funcional integral: success
Fase 2 - Auditoría de configuración y dependencias: success
Fase 8 - Validación completa automatizada: success
Bloque 2C.1 - Variante técnica y migración: success
VariApp CI: skipped por condición del workflow
```

Un workflow `skipped` por condición no se contabiliza como una validación ejecutada ni como un fallo.

## 4. Incidencia final corregida durante la certificación

La primera aceptación de 2C.5 alcanzó 86/87 pruebas. La única falla no correspondía a la aplicación: un `page.route` de Playwright utilizaba un patrón sin host y capturaba por error la navegación Angular `/compras`, devolviendo el fixture JSON en lugar de permitir cargar la SPA.

La corrección restringió los mocks a la API de desarrollo (`http://localhost:5005/...`). La ejecución posterior obtuvo 87/87 pruebas aprobadas, incluida la prueba de imágenes que había fallado.

## 5. Límites y pendientes que NO se certifican como completados

1. **Dispositivos físicos:** CI valida la integración de cámara sin activar hardware real. Deben mantenerse como validaciones manuales posteriores:
   - cámara real Android;
   - cámara real iPhone/iOS;
   - lector USB físico;
   - lector Bluetooth físico.
2. **Permisos del navegador:** el comportamiento final ante permisos de cámara depende del navegador/dispositivo real y debe probarse externamente.
3. **`TipoInventario`:** la diferenciación `MercaderiaVenta` / `InsumoAdministrativo` pertenece al bloque funcional de insumos administrativos y no se declara implementada por 2C.
4. **Dependencias/deprecaciones:** los workflows actuales pasan según las políticas del repositorio, aunque los runners muestran avisos de deprecación de Node 20 en algunas Actions y npm informa dependencias de desarrollo que deberán seguir auditándose. Estos avisos no se silencian ni se presentan como corregidos por esta certificación.
5. **Producción:** no existe certificación productiva, despliegue productivo ni autorización de merge derivada de este documento.

## 6. Gobernanza

Estado exigido al cierre:

```text
Rama de trabajo: Desarrollo
PR oficial: #2 Desarrollo -> main
PR #2 abierto: sí
PR #2 borrador: sí
Merge: no autorizado
Auto-merge: no autorizado
main: congelada
Producción: congelada
```

Este documento no autoriza despliegues, migraciones, modificaciones de secretos ni cambios de infraestructura productiva.

## 7. Dictamen de Fase 2C.6

Con la evidencia automatizada disponible:

```text
BLOQUE 2C — DESARROLLO: APROBADO
2C.1 VARIANTE TÉCNICA: APROBADA
2C.2 CICLO DE VIDA TÉCNICO: APROBADO
2C.3 BACKEND DEL ESCÁNER: APROBADO
2C.4 FRONTEND DEL ESCÁNER: APROBADO EN CI
2C.5 AUTOCOMPLETE REMOTO: APROBADO
PRUEBAS BACKEND: 173/173
PLAYWRIGHT INTEGRAL: 87/87
REGRESIONES BLOQUEANTES AUTOMATIZADAS: 0
VALIDACIONES FÍSICAS: PENDIENTES Y EXTERNAS
PRODUCCIÓN: NO AUTORIZADA
MERGE A MAIN: NO AUTORIZADO
```

El Bloque 2C queda funcionalmente cerrado en Desarrollo. Las validaciones físicas se mantienen explícitamente fuera de esta certificación automatizada.
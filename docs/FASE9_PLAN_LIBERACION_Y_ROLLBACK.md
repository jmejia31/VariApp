# Fase 9 — Plan de liberación y rollback

Estado: liberación productiva ejecutada y estabilizada  
Ejecución autorizada: **Sí — autorización expresa del propietario en la ventana 2026-09-17/18**  
Producción: **LIVE**

## 1. Objetivo

Definir una secuencia segura para una liberación futura de Solqaryn sin ejecutarla durante la Fase 9.

Este documento no autoriza:

- merge a `main`;
- despliegue productivo;
- migraciones productivas;
- modificación de secretos, dominios, bases o servicios productivos.

## 2. Condiciones obligatorias previas

La liberación no puede comenzar hasta que todas las condiciones aplicables estén aprobadas:

- [ ] Checklist externo completado o excepciones firmadas.
- [ ] Informe final revisado por Javier Mejía.
- [ ] Riesgos aceptados.
- [ ] PR #2 revisado.
- [ ] Commit exacto de liberación identificado.
- [ ] Respaldo completo y verificable de base de datos.
- [ ] Prueba de restauración aprobada.
- [ ] Inventario de variables y secretos sin revelar valores.
- [ ] Migración única revisada.
- [ ] SQL forward revisado como no destructivo.
- [ ] Estrategia de rollback de esquema definida.
- [ ] Ventana de mantenimiento aprobada.
- [ ] Responsables asignados.
- [ ] Canales de comunicación definidos.
- [ ] Criterios de suspensión aprobados.
- [ ] Autorización escrita de Javier Mejía.

## 3. Roles mínimos

| Rol | Responsabilidad |
|---|---|
| Propietario/Aprobador | Javier Mejía; autoriza o suspende |
| Responsable backend | Compilación, API, migraciones y health |
| Responsable frontend | Build, rutas y verificación visual |
| Responsable base de datos | Respaldo, migración y restauración |
| Responsable infraestructura | Render, Vercel, dominios y variables |
| Validador funcional | Smoke tests y aceptación posterior |
| Responsable de rollback | Ejecuta reversión si se activa un criterio |

Una persona puede asumir varios roles, pero cada responsabilidad debe estar asignada antes de iniciar.

## 4. Inventario previo

Registrar sin incluir secretos:

```text
Commit de liberación:
Fecha/hora:
Ventana:
Base de datos:
Versión de esquema actual:
Migración objetivo:
Servicio backend:
Proyecto frontend:
Dominio:
Cloudinary/prefijo:
Proveedor SMTP:
Responsables:
```

## 5. Respaldo

### 5.1 Base de datos

1. detener operaciones de escritura según la ventana aprobada;
2. generar respaldo completo;
3. calcular hash del archivo;
4. verificar tamaño no vacío;
5. almacenar copia en ubicación segura;
6. ensayar restauración en una base separada;
7. ejecutar consultas de control;
8. registrar tiempo de restauración.

### 5.2 Configuración

Exportar o documentar sin mostrar valores:

- nombres de variables;
- servicios vinculados;
- dominios;
- versión de runtime;
- ramas y commits;
- configuración de health checks;
- prefijos de almacenamiento.

### 5.3 Activos

Registrar:

- cantidad de imágenes relevantes;
- carpetas o prefijos;
- política de retención;
- método de recuperación.

## 6. Estrategia de migración

### 6.1 Principios

- ejecutar una sola secuencia de migración;
- no editar manualmente el historial de EF Core;
- no ejecutar `DROP`, `TRUNCATE` o eliminación masiva sin autorización específica;
- conservar compatibilidad durante la ventana;
- registrar inicio, fin y resultado;
- suspender ante cualquier discrepancia.

### 6.2 Secuencia propuesta

1. confirmar respaldo y restauración;
2. poner aplicación en modo de mantenimiento, si se aprueba;
3. verificar conectividad a la base correcta;
4. consultar versión actual del esquema;
5. generar o revisar SQL forward exacto;
6. aplicar migraciones;
7. verificar historial de migraciones;
8. comprobar tablas e índices esperados;
9. ejecutar consultas de consistencia;
10. no habilitar tráfico hasta aprobar smoke tests.

## 7. Orden de liberación propuesto

1. congelar cambios durante la ventana;
2. confirmar commit autorizado;
3. realizar respaldo;
4. ejecutar migración de base de datos;
5. actualizar backend;
6. comprobar `/health` y `/health/ready`;
7. actualizar frontend;
8. comprobar rutas y proxy `/api`;
9. validar Cloudinary y correo;
10. ejecutar smoke tests;
11. habilitar tráfico completo;
12. monitorear durante la ventana acordada.

El orden definitivo debe ajustarse al comportamiento real de los proveedores y a la compatibilidad del commit autorizado.

## 8. Smoke tests posteriores

### Seguridad

- [ ] Inicio de sesión.
- [ ] Usuario sin permiso recibe 403.
- [ ] Documento ajeno no es visible.
- [ ] Logout.

### Inventario

- [ ] Crear producto de prueba.
- [ ] Agregar dos colores.
- [ ] Confirmar suma de stock.
- [ ] Registrar compra por variante.
- [ ] Registrar venta por variante.
- [ ] Anular y confirmar restitución.

### Facturación

- [ ] Confirmar cálculo fiscal.
- [ ] Generar factura.
- [ ] Registrar pago parcial.
- [ ] Descargar PDF A4.
- [ ] Compartir enlace controlado.

### Integraciones

- [ ] Cargar imagen.
- [ ] Enviar correo.
- [ ] Revisar logs.
- [ ] Confirmar que no se exponen secretos.

### Cargas masivas

- [ ] Validar archivo pequeño.
- [ ] Revisar vista previa.
- [ ] Descargar errores de archivo inválido.
- [ ] Confirmar carga válida controlada.

## 9. Criterios de suspensión inmediata

Activar rollback cuando ocurra cualquiera de estos eventos:

- health o readiness no recuperan;
- migración incompleta;
- pérdida o corrupción de datos;
- números de factura duplicados;
- stock inconsistente;
- usuarios acceden a información ajena;
- secretos aparecen en logs o respuestas;
- frontend no puede comunicarse con backend;
- errores 5xx sostenidos;
- correo envía duplicados;
- Cloudinary afecta activos equivocados;
- tiempo de indisponibilidad supera el límite aprobado;
- Javier Mejía ordena suspender.

## 10. Rollback de aplicación

1. suspender tráfico o activar mantenimiento;
2. identificar último commit estable;
3. revertir frontend al despliegue anterior;
4. revertir backend al despliegue anterior;
5. comprobar health y rutas;
6. no reabrir tráfico hasta evaluar compatibilidad con la base.

## 11. Rollback de base de datos

El rollback de código no implica automáticamente rollback de esquema.

Opciones, en orden de preferencia:

1. mantener esquema compatible y revertir solo aplicación;
2. aplicar migración reversa previamente revisada;
3. restaurar respaldo en una base separada y validar;
4. con autorización, reemplazar la base afectada.

Nunca ejecutar una reversión destructiva improvisada.

## 12. Rollback de activos y configuración

- restaurar variables a su versión documentada;
- restaurar dominios o rutas anteriores;
- revertir prefijos/configuración sin borrar activos por nombre supuesto;
- no eliminar carpetas Cloudinary sin inventario confirmado;
- rotar secretos únicamente si existe exposición comprobada y procedimiento aprobado.

## 13. Validación posterior al rollback

- [ ] Health y readiness.
- [ ] Login.
- [ ] Lectura de productos.
- [ ] Stock de control.
- [ ] Factura existente.
- [ ] Permisos.
- [ ] Logs.
- [ ] Integridad de base de datos.
- [ ] Confirmación de Javier Mejía.

## 14. Registro de la ventana

```text
Fecha:
Inicio:
Fin:
Commit:
Migración:
Respaldo/hash:
Responsables:
Resultado:
Incidentes:
Rollback ejecutado: Sí/No
Estado final:
Aprobación de Javier Mejía:
```

## 15. Estado actual

```text
LIBERACIÓN EJECUTADA
PRODUCCIÓN LIVE
MIGRACIONES COMPLETAS
MAINTENANCE OFF
POSTCHECK PASS
```

## 16. Registro de la ventana ejecutada — 2026-09-17/18

- Autorización: expresa del propietario para continuar hasta completar la liberación productiva.
- Respaldo productivo: confirmado por el propietario antes del APPLY.
- Commit productivo estabilizado: `7f140442d598aaea36b39952bad5ebd9ab4f2613`.
- Servicio backend: `solqaryn-api` en Render.
- Deploy final de reapertura: `dep-damb4lqjnfac73efl1pg` — `live`.
- Maintenance gate: activado antes del APPLY y desactivado después del postcheck.
- Migración final observada: `20260914232400_N7_10_C_DocumentoFiscalPersistencia`.
- Reinicio final: EF informó que no había migraciones pendientes y que la base estaba al día.
- Incidente controlado: N0.4 detectó duplicidad RBAC legacy durante el primer APPLY; el tráfico permaneció cerrado, se corrigió de forma retry-safe y se recertificó.
- Certificación exact-head N0.4: run `35304697573`, job `105474385670`, `SUCCESS`; backend `2328/2328`, preflight, APPLY exacto, aislamiento, deduplicación, guards fail-closed y snapshot EF en PASS.
- Post-reapertura: servicio escuchando en puerto 10000, Application started, Render LIVE; sin excepciones MySQL/EF ni respuestas 503 detectadas en el corte de validación.
- Rollback: no ejecutado; no se activó criterio de suspensión tras el hotfix.
- Estado final: producción abierta y estable según health/deploy/logs observados.

La evidencia current-standard de N9.4 está en `vaep/evidence/reviews/` y `vaep/evidence/receipts/`.
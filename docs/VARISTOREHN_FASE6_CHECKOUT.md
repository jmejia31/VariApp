# VariStoreHN — Fase 6: Checkout y pedido

## Objetivo
Cerrar el MVP público con el recorrido canónico `Carrito → Checkout → Pedido`, sin adelantar Fase 7.

## Reglas quirúrgicas
- El navegador nunca es autoridad de precio, stock, disponibilidad ni total.
- El checkout rehidrata el carrito y luego lo revalida contra el catálogo público vigente antes de cualquier salida comercial.
- El request de validación contiene identidad de selección y cantidad (`productoId`, `modeloId`, `modeloNombre`, `marcaNombre`, `unidades`); nombre de modelo y marca solo desambiguan la agrupación publicada y **nunca** son autoridad de precio o existencia.
- El backend reconstruye la misma agrupación de variante desde datos vigentes y calcula precio, stock, totales y vigencia; no acepta importes enviados por Angular.
- La validación pública es deliberadamente sin efectos secundarios: no reserva inventario, no crea venta, no crea `PedidoVenta` administrativo y no cobra.
- Una validación dura como máximo 10 minutos. WhatsApp y tarjeta vuelven a comprobar la vigencia al ejecutar la acción; un enlace preparado no puede utilizarse después de vencer.
- La persistencia del carrito conserva solo referencias seguras; no precios ni datos de pago.
- Los datos de contacto del comprador no se escriben en `localStorage`. El recibo UX usa `sessionStorage`, expira y omite nombre, teléfono, correo y notas.
- La ruta `/varistorehn/pedido/:id` representa una confirmación UX de la sesión; nunca se presenta como factura, pago confirmado ni pedido ERP si esas autoridades no existen.
- WhatsApp puede continuar únicamente después de una validación vigente y usa el total devuelto por el servidor.
- Tarjeta solo se habilita cuando existe un endpoint backend seguro configurado y la URL de retorno es HTTPS con origen explícitamente permitido. VariStoreHN no captura PAN, CVV ni PIN.
- Las rutas públicas canónicas son `/varistorehn/checkout` y `/varistorehn/pedido/:id` y no usan guards administrativos.
- El modo comercial respeta `whatsapp | tarjeta | ambos`.
- Tema, identidad, moneda y WhatsApp siguen viniendo de la configuración existente de empresa; Fase 6 no introduce una paleta local.
- En desarrollo, BD y demo conservan contextos de carrito separados; la fuente seleccionada se propaga al checkout únicamente en la vista previa. Producción conserva la fuente definida por configuración.
- Demo nunca genera cobros ni pedidos reales.

## Definition of Done
1. Carrito enlaza a checkout solo cuando está hidratado y no vacío.
2. Checkout vuelve a hidratar y revalidar agrupación, precio y stock desde la fuente vigente.
3. Variantes con `modeloId` nulo o repetido no pueden mezclar precio/stock de agrupaciones distintas.
4. Datos mínimos del comprador tienen validación accesible y no se guardan en almacenamiento persistente innecesario.
5. WhatsApp genera un mensaje estructurado con importes revalidados y bloquea la salida si la validación venció.
6. Tarjeta permanece fail-closed si no hay backend seguro y origen HTTPS permitido; el frontend no captura credenciales de tarjeta.
7. Existe una ruta final de referencia pública que maneja correctamente recibos presentes, vencidos o ausentes sin inventar estado transaccional.
8. Backend compila en Release y las pruebas específicas cubren límites, stock, variante, agrupación ambigua y cálculo autoritativo.
9. Frontend pasa TypeScript, guardas estáticas y build de producción.
10. Playwright ejecuta la regresión acumulada Fases 1–6, incluyendo fuente real, demo, conflicto, privacidad, vencimiento y viewport de 320 px.
11. No se introduce código de Fase 7.

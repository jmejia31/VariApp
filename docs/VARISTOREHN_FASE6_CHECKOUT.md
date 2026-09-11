# VariStoreHN — Fase 6: Checkout y pedido

## Objetivo
Cerrar el MVP público con el recorrido canónico `Carrito → Checkout → Pedido`, sin adelantar Fase 7.

## Reglas quirúrgicas
- El navegador nunca es autoridad de precio, stock, disponibilidad ni total.
- El checkout debe revalidar el carrito contra el catálogo público vigente antes de confirmar.
- La persistencia local conserva solo referencias seguras del carrito; no precios ni datos de pago.
- WhatsApp puede finalizar el flujo únicamente después de una validación de carrito vigente.
- Tarjeta solo se habilita cuando exista un endpoint seguro configurado; el frontend no captura PAN/CVV.
- Las rutas públicas canónicas son `/varistorehn/checkout` y `/varistorehn/pedido/:id`.
- El modo comercial respeta `whatsapp | tarjeta | ambos`.
- Tema, identidad, moneda y WhatsApp siguen viniendo de la configuración existente de empresa.
- Demo nunca genera cobros ni pedidos reales.

## Definition of Done
1. Carrito enlaza a checkout solo cuando está hidratado y no vacío.
2. Checkout vuelve a hidratar/revalidar precio y stock desde la fuente vigente.
3. Datos mínimos del comprador tienen validación accesible y no se guardan en localStorage sin necesidad.
4. WhatsApp genera un mensaje estructurado a partir de datos revalidados y distingue demo/real.
5. Tarjeta permanece fail-closed si no hay backend seguro/origen permitido.
6. Existe estado final de pedido/referencia pública y ruta canónica de confirmación.
7. Fases 1–5 siguen verdes sin regresiones.
8. Se agregan guarda estática, Playwright Fase 6 y workflow acumulado 1–6.
9. No se introduce código de Fase 7.

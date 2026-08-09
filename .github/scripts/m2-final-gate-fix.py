from pathlib import Path


def replace_once(path: str, old: str, new: str) -> None:
    p = Path(path)
    text = p.read_text(encoding="utf-8")
    count = text.count(old)
    if count != 1:
        raise SystemExit(f"FAIL-CLOSED: {path}: se esperó 1 coincidencia y se encontraron {count}")
    p.write_text(text.replace(old, new, 1), encoding="utf-8")


# 1) Preview de venta: misma fuente de verdad que la venta persistida.
replace_once(
    "backend/src/Application/Services/VentaService.cs",
    '''    public async Task<ResultadoCalculoDto> CalcularVistaPreviaAsync(CalcularVentaRequest request)\n    {\n        var entradas = new List<DetalleCalculoInput>();\n        foreach (var d in request.Detalles)\n        {\n            var producto = await _productoRepository.GetByIdAsync(d.ProductoId);\n            entradas.Add(new DetalleCalculoInput\n            {\n                ProductoId = d.ProductoId,\n                CategoriaId = producto?.CategoriaId,\n                Cantidad = d.Cantidad,\n                PrecioUnitario = d.PrecioUnitario\n            });\n        }\n\n        return await _calculoService.CalcularVentaAsync(entradas, request.ClienteId, _currentUser.RolId, request.CodigoPromocional, request.CostoEnvioId, request.EnvioExonerado, request.MotivoExoneracionEnvio);\n    }\n''',
    '''    public async Task<ResultadoCalculoDto> CalcularVistaPreviaAsync(CalcularVentaRequest request)\n    {\n        if (request.Detalles.Count == 0)\n            throw new BusinessRuleException("La venta debe tener al menos un producto.");\n\n        var entradas = new List<DetalleCalculoInput>();\n        foreach (var d in request.Detalles)\n        {\n            if (d.Cantidad <= 0)\n                throw new BusinessRuleException("La cantidad de cada producto debe ser mayor a 0.");\n\n            var producto = await _productoRepository.GetByIdAsync(d.ProductoId)\n                ?? throw new BusinessRuleException($"El producto con id {d.ProductoId} no existe.");\n            if (!producto.Activo)\n                throw new BusinessRuleException($"El producto '{producto.Nombre}' está inactivo.");\n\n            ProductoVariante? variante = null;\n            if (d.ProductoVarianteId.HasValue)\n            {\n                variante = await ObtenerVarianteAsync(d.ProductoVarianteId.Value, producto.Id, exigirActiva: true);\n            }\n            else if (producto.Variantes.Any(v => v.Activo && !v.Eliminado))\n            {\n                throw new BusinessRuleException($"Debes seleccionar una variante para el producto '{producto.Nombre}'.");\n            }\n\n            if (variante is null && d.PrecioUnitario <= 0)\n                throw new BusinessRuleException("El precio unitario de cada producto debe ser mayor a 0.");\n\n            entradas.Add(new DetalleCalculoInput\n            {\n                ProductoId = producto.Id,\n                CategoriaId = producto.CategoriaId,\n                Cantidad = d.Cantidad,\n                PrecioUnitario = variante?.Precio ?? d.PrecioUnitario\n            });\n        }\n\n        return await _calculoService.CalcularVentaAsync(entradas, request.ClienteId, _currentUser.RolId, request.CodigoPromocional, request.CostoEnvioId, request.EnvioExonerado, request.MotivoExoneracionEnvio);\n    }\n''')

# 2) E2E de formulario: interacción accesible y estable con Angular Material.
replace_once(
    "frontend/e2e/fase4-variantes.spec.ts",
    '''    const datosFamilia = page.locator('.data-section');\n    await datosFamilia.locator('mat-select[formcontrolname="marcaId"]').click();\n    await page.getByRole('option', { name: nombres.marca, exact: true }).click();\n\n    await datosFamilia.locator('mat-select[formcontrolname="modeloId"]').click();\n    await page.getByRole('option', { name: nombres.modelo, exact: true }).click();\n\n    const variantes = page.locator('.variant-card');\n    const primeraVariante = variantes.nth(0);\n    await primeraVariante.locator('mat-select[formcontrolname="colorId"]').click();\n    await page.getByRole('option', { name: nombres.color, exact: true }).click();\n''',
    '''    const datosFamilia = page.locator('.data-section');\n    const marcaPredeterminada = datosFamilia.getByRole('combobox', { name: 'Marca predeterminada (opcional)' });\n    await marcaPredeterminada.focus();\n    await marcaPredeterminada.press('Enter');\n    await page.getByRole('option', { name: nombres.marca, exact: true }).click();\n\n    const modeloPredeterminado = datosFamilia.getByRole('combobox', { name: 'Modelo predeterminado (opcional)' });\n    await modeloPredeterminado.focus();\n    await modeloPredeterminado.press('Enter');\n    await page.getByRole('option', { name: nombres.modelo, exact: true }).click();\n\n    const variantes = page.locator('.variant-card');\n    const primeraVariante = variantes.nth(0);\n    const colorPrimera = primeraVariante.getByRole('combobox', { name: 'Color (opcional)' });\n    await colorPrimera.focus();\n    await colorPrimera.press('Enter');\n    await page.getByRole('option', { name: nombres.color, exact: true }).click();\n''')
replace_once(
    "frontend/e2e/fase4-variantes.spec.ts",
    '''    const segundaVariante = variantes.nth(1);\n    await segundaVariante.locator('mat-select[formcontrolname="colorId"]').click();\n    await page.getByRole('option', { name: nombres.color2, exact: true }).click();\n''',
    '''    const segundaVariante = variantes.nth(1);\n    const colorSegunda = segundaVariante.getByRole('combobox', { name: 'Color (opcional)' });\n    await colorSegunda.focus();\n    await colorSegunda.press('Enter');\n    await page.getByRole('option', { name: nombres.color2, exact: true }).click();\n''')

# 3) Importador M2.N: plantilla de variante incluye Talla aunque no aplique.
replace_once(
    "frontend/e2e/fase5-cargas-masivas.spec.ts",
    '''      'Producto,Marca,Modelo,Color,SKU,CodigoBarras,Cantidad,UmbralStockBajo,Costo,Precio,Activo',\n      `${nombres.producto},${nombres.marca},${nombres.modelo},${nombres.color},${nombres.sku},750${suffix.slice(-9)},7,2,125.50,299.00,Si`\n''',
    '''      'Producto,Marca,Modelo,Color,Talla,SKU,CodigoBarras,Cantidad,UmbralStockBajo,Costo,Precio,Activo',\n      `${nombres.producto},${nombres.marca},${nombres.modelo},${nombres.color},,${nombres.sku},750${suffix.slice(-9)},7,2,125.50,299.00,Si`\n''')

# 4) Facturación: la variante exacta, no el precio enviado por el cliente, es la fuente de verdad.
replace_once(
    "frontend/e2e/fase7-validacion-integral.spec.ts",
    "  test('factura combinada mantiene subtotal L. 191.30, ISV L. 28.70, envío L. 80.00, descuento L. 20.00 y total L. 280.00', async ({ request }) => {",
    "  test('factura combinada usa precio de variante exacta: subtotal L. 60.87, ISV L. 9.13, envío L. 80.00, descuento L. 20.00 y total L. 130.00', async ({ request }) => {")
replace_once(
    "frontend/e2e/fase7-validacion-integral.spec.ts",
    '''    expect(calculo.importeBruto).toBe(300);\n    expect(calculo.importeProductos).toBe(220);\n    expect(calculo.subtotal).toBe(191.3);\n    expect(calculo.impuestoIncluido).toBe(28.7);\n    expect(calculo.costoEnvio).toBe(80);\n    expect(calculo.totalDescuento).toBe(20);\n    expect(calculo.total).toBe(280);\n    expect(calculo.subtotal + calculo.impuestoIncluido + calculo.impuestoAdicional + calculo.costoEnvio - calculo.totalDescuento).toBe(280);\n''',
    '''    expect(calculo.importeBruto).toBe(150);\n    expect(calculo.importeProductos).toBe(70);\n    expect(calculo.subtotal).toBe(60.87);\n    expect(calculo.impuestoIncluido).toBe(9.13);\n    expect(calculo.costoEnvio).toBe(80);\n    expect(calculo.totalDescuento).toBe(20);\n    expect(calculo.total).toBe(130);\n    expect(calculo.subtotal + calculo.impuestoIncluido + calculo.impuestoAdicional + calculo.costoEnvio - calculo.totalDescuento).toBe(130);\n''')
replace_once(
    "frontend/e2e/fase7-validacion-integral.spec.ts",
    '''    expect(factura.subtotal).toBe(191.3);\n    expect(factura.impuestoIncluido).toBe(28.7);\n    expect(factura.costoEnvio).toBe(80);\n    expect(factura.descuento).toBe(20);\n    expect(factura.total).toBe(280);\n\n    const pagoParcial = await request.post(`${API_URL}/facturas/${factura.id}/pagos`, {\n      headers: headers(),\n      data: { monto: 100, metodoPago: 'Efectivo', referencia: `PARCIAL-${suffix}` }\n    });\n    expect(pagoParcial.status(), await pagoParcial.text()).toBe(200);\n    const facturaParcial = await dataOf(pagoParcial);\n    expect(facturaParcial.totalPagado).toBe(100);\n    expect(facturaParcial.saldoPendiente).toBe(180);\n\n    const pagoFinal = await request.post(`${API_URL}/facturas/${factura.id}/pagos`, {\n      headers: headers(),\n      data: { monto: 180, metodoPago: 'Transferencia', referencia: `TOTAL-${suffix}` }\n    });\n    expect(pagoFinal.status(), await pagoFinal.text()).toBe(200);\n    const facturaPagada = await dataOf(pagoFinal);\n    expect(facturaPagada.totalPagado).toBe(280);\n''',
    '''    expect(factura.subtotal).toBe(60.87);\n    expect(factura.impuestoIncluido).toBe(9.13);\n    expect(factura.costoEnvio).toBe(80);\n    expect(factura.descuento).toBe(20);\n    expect(factura.total).toBe(130);\n\n    const pagoParcial = await request.post(`${API_URL}/facturas/${factura.id}/pagos`, {\n      headers: headers(),\n      data: { monto: 100, metodoPago: 'Efectivo', referencia: `PARCIAL-${suffix}` }\n    });\n    expect(pagoParcial.status(), await pagoParcial.text()).toBe(200);\n    const facturaParcial = await dataOf(pagoParcial);\n    expect(facturaParcial.totalPagado).toBe(100);\n    expect(facturaParcial.saldoPendiente).toBe(30);\n\n    const pagoFinal = await request.post(`${API_URL}/facturas/${factura.id}/pagos`, {\n      headers: headers(),\n      data: { monto: 30, metodoPago: 'Transferencia', referencia: `TOTAL-${suffix}` }\n    });\n    expect(pagoFinal.status(), await pagoFinal.text()).toBe(200);\n    const facturaPagada = await dataOf(pagoFinal);\n    expect(facturaPagada.totalPagado).toBe(130);\n''')
replace_once(
    "frontend/e2e/fase7-validacion-integral.spec.ts",
    '''      'Producto,Marca,Modelo,Color,SKU,CodigoBarras,Cantidad,UmbralStockBajo,Costo,Precio,Activo',\n      `Producto inexistente ${suffix},${nombres.marca},${nombres.modelo},${nombres.blanco},F7-ERR-${suffix},,2,1,10,20,Si`,\n      `${nombres.productoVariantes},${nombres.marca},${nombres.modelo},${nombres.blanco},F7-NEG-${suffix},,-3,1,10,20,Si`\n''',
    '''      'Producto,Marca,Modelo,Color,Talla,SKU,CodigoBarras,Cantidad,UmbralStockBajo,Costo,Precio,Activo',\n      `Producto inexistente ${suffix},${nombres.marca},${nombres.modelo},${nombres.blanco},,F7-ERR-${suffix},,2,1,10,20,Si`,\n      `${nombres.productoVariantes},${nombres.marca},${nombres.modelo},${nombres.blanco},,F7-NEG-${suffix},,-3,1,10,20,Si`\n''')

# 5) Aislamiento: diferenciar por cantidad manteniendo precio de variante como autoridad.
replace_once(
    "frontend/e2e/phase7-user-isolation.spec.ts",
    '''  customer: string,\n  unitPrice: number\n): Promise<Record<string, any>> {\n''',
    '''  customer: string,\n  quantity: number\n): Promise<Record<string, any>> {\n''')
replace_once(
    "frontend/e2e/phase7-user-isolation.spec.ts",
    '''        cantidad: 1,\n        precioUnitario: unitPrice\n''',
    '''        cantidad: quantity,\n        precioUnitario: 250\n''')
replace_once(
    "frontend/e2e/phase7-user-isolation.spec.ts",
    '''    const saleA = await createConfirmedSale(request, tokenA, Number(product.id), 'Cliente exclusivo A', 111);\n    const saleB = await createConfirmedSale(request, tokenB, Number(product.id), 'Cliente exclusivo B', 222);\n''',
    '''    const saleA = await createConfirmedSale(request, tokenA, Number(product.id), 'Cliente exclusivo A', 1);\n    const saleB = await createConfirmedSale(request, tokenB, Number(product.id), 'Cliente exclusivo B', 2);\n''')
replace_once(
    "frontend/e2e/phase7-user-isolation.spec.ts",
    '''    expect(Number(summaryA.ingresosTotales)).toBe(111);\n    expect(Number(summaryB.ingresosTotales)).toBe(222);\n''',
    '''    expect(Number(summaryA.ingresosTotales)).toBe(250);\n    expect(Number(summaryB.ingresosTotales)).toBe(500);\n''')

# 6) Filtros M2: las dimensiones operativas viven en la variante comercial exacta.
replace_once(
    "frontend/e2e/productos-filtros.spec.ts",
    '''      TallaId: String(size.id),\n      Cantidad: '0',\n      Costo: '80',\n      Precio: '140',\n      UmbralStockBajo: '2'\n''',
    '''      TallaId: String(size.id),\n      Cantidad: '0',\n      Costo: '80',\n      Precio: '140',\n      UmbralStockBajo: '2',\n      'Variantes[0].MarcaId': String(brand.id),\n      'Variantes[0].ModeloId': String(model.id),\n      'Variantes[0].ColorId': String(color.id),\n      'Variantes[0].TallaId': String(size.id),\n      'Variantes[0].Cantidad': '0',\n      'Variantes[0].Costo': '80',\n      'Variantes[0].Precio': '140',\n      'Variantes[0].UmbralStockBajo': '2',\n      'Variantes[0].Activo': 'true'\n''')

print("M2 final gate patch aplicado correctamente.")

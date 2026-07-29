from pathlib import Path


def replace_idempotent(path: str, old: str, new: str) -> None:
    file = Path(path)
    text = file.read_text(encoding="utf-8")
    if new in text:
        return
    count = text.count(old)
    if count != 1:
        raise SystemExit(f"{path}: se esperaba una coincidencia y se encontraron {count}")
    file.write_text(text.replace(old, new, 1), encoding="utf-8")


def remove_idempotent(path: str, old: str, forbidden: str) -> None:
    file = Path(path)
    text = file.read_text(encoding="utf-8")
    if old in text:
        if text.count(old) != 1:
            raise SystemExit(f"{path}: bloque duplicado al eliminar")
        file.write_text(text.replace(old, "", 1), encoding="utf-8")
        return
    if forbidden in text:
        raise SystemExit(f"{path}: la API obsoleta sigue presente con una forma no reconocida")


replace_idempotent(
    "backend/src/Application/Services/CalculoService.cs",
    """            var importeSujeto = impuesto.SeCalculaAntesDescuento
                ? baseElegibleProductos
                : Math.Max(0, baseElegibleProductos - descuentoProrrateado);""",
    """            // El descuento reduce el total final, pero no reescribe la composición
            // histórica de un impuesto que ya estaba incluido en el precio comercial.
            var importeSujeto = impuesto.IncluidoEnPrecio || impuesto.SeCalculaAntesDescuento
                ? baseElegibleProductos
                : Math.Max(0, baseElegibleProductos - descuentoProrrateado);""",
)

replace_idempotent(
    "backend/src/Application/Services/CalculoService.cs",
    """        var subtotalNeto = Math.Max(0, importeProductos - totalDescuento - impuestoIncluido);
        var total = Math.Max(0, subtotalNeto + impuestoIncluido + impuestoAdicional + envio.Monto);""",
    """        // El subtotal y el impuesto incluido describen el precio comercial antes
        // del descuento. El descuento se presenta y descuenta como componente separado.
        var subtotalNeto = Math.Max(0, importeProductos - impuestoIncluido);
        var total = Math.Max(0,
            subtotalNeto + impuestoIncluido + impuestoAdicional + envio.Monto - totalDescuento);""",
)

replace_idempotent(
    "backend/tests/InventoryApp.Tests/CalculoServiceTests.cs",
    """        Assert.Equal(90m, resultado.Subtotal);
        Assert.Equal(13.50m, resultado.ImpuestoAdicional);""",
    """        Assert.Equal(100m, resultado.Subtotal);
        Assert.Equal(13.50m, resultado.ImpuestoAdicional);""",
)

replace_idempotent(
    "backend/tests/InventoryApp.Tests/CalculoServiceTests.cs",
    """        Assert.Equal(20m, resultado.TotalDescuento);
        Assert.Equal(80m, resultado.CostoEnvio);
        Assert.Equal(280m, resultado.Total);""",
    """        Assert.Equal(20m, resultado.TotalDescuento);
        Assert.Equal(191.30m, resultado.Subtotal);
        Assert.Equal(28.70m, resultado.ImpuestoIncluido);
        Assert.Equal(80m, resultado.CostoEnvio);
        Assert.Equal(280m, resultado.Total);""",
)

replace_idempotent(
    "backend/src/Application/Services/VentaService.cs",
    """            NumeroVenta = await GenerarNumeroVentaAsync(),""",
    """            NumeroVenta = CrearNumeroTemporal("VEN"),""",
)

replace_idempotent(
    "backend/src/Application/Services/VentaService.cs",
    """        await _ventaRepository.AddAsync(venta);
        await _ventaRepository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(ModuloSistema.Ventas, AccionPermiso.Crear, $"Venta creada: {venta.NumeroVenta}", venta.Id);

        return ToDto(venta);""",
    """        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _ventaRepository.AddAsync(venta);
            await _ventaRepository.SaveChangesAsync();

            // El identificador autoincremental es la única fuente de numeración.
            // El número temporal evita colisiones mientras MySQL asigna el Id.
            venta.NumeroVenta = $"VEN-{venta.Id:D6}";
            _ventaRepository.Update(venta);
            await _ventaRepository.SaveChangesAsync();
        });

        await _auditoria.RegistrarAsync(
            ModuloSistema.Ventas,
            AccionPermiso.Crear,
            $"Venta creada: {venta.NumeroVenta}",
            venta.Id,
            entidad: "Venta",
            valoresNuevos: new
            {
                venta.NumeroVenta,
                venta.ImporteBruto,
                venta.Subtotal,
                venta.Impuesto,
                venta.Descuento,
                venta.CostoEnvio,
                venta.EnvioExonerado,
                venta.MotivoExoneracionEnvio,
                venta.Total
            });

        return ToDto(venta);""",
)

replace_idempotent(
    "backend/src/Application/Services/VentaService.cs",
    """                NumeroFactura = await GenerarNumeroFacturaAsync(),""",
    """                NumeroFactura = CrearNumeroTemporal("FAC"),""",
)

replace_idempotent(
    "backend/src/Application/Services/VentaService.cs",
    """            await _facturaRepository.AddAsync(factura);

            // Registrar el uso histórico (incrementa UsosRealizados de cada""",
    """            await _facturaRepository.AddAsync(factura);
            await _facturaRepository.SaveChangesAsync();

            // La numeración definitiva deriva del Id asignado por MySQL y por eso
            // permanece única incluso cuando varias ventas se confirman a la vez.
            factura.NumeroFactura = $"FAC-{factura.Id:D6}";
            _facturaRepository.Update(factura);

            // Registrar el uso histórico (incrementa UsosRealizados de cada""",
)

replace_idempotent(
    "backend/src/Application/Services/VentaService.cs",
    """    private async Task<string> GenerarNumeroVentaAsync()
    {
        var total = await _ventaRepository.ContarTodasAsync();
        return $"VEN-{(total + 1):D6}";
    }

    private async Task<string> GenerarNumeroFacturaAsync()
    {
        var total = await _facturaRepository.ContarTodasAsync();
        return $"FAC-{(total + 1):D6}";
    }
""",
    """    private static string CrearNumeroTemporal(string prefijo)
    {
        var token = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
        return $"{prefijo}-TMP-{token}";
    }
""",
)

remove_idempotent(
    "backend/src/Application/Interfaces/IVentaRepository.cs",
    "    Task<int> ContarTodasAsync();\n",
    "ContarTodasAsync",
)
remove_idempotent(
    "backend/src/Application/Interfaces/IFacturaRepository.cs",
    "    Task<int> ContarTodasAsync();\n",
    "ContarTodasAsync",
)
remove_idempotent(
    "backend/src/Infrastructure/Repositories/VentaRepository.cs",
    """    public async Task<int> ContarTodasAsync() =>
        await _context.Ventas.IgnoreQueryFilters().CountAsync();

""",
    "ContarTodasAsync",
)
remove_idempotent(
    "backend/src/Infrastructure/Repositories/FacturaRepository.cs",
    """    public async Task<int> ContarTodasAsync() =>
        await _context.Facturas.CountAsync();

""",
    "ContarTodasAsync",
)

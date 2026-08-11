from pathlib import Path

p = Path('backend/src/Infrastructure/Services/CargaMasivaService.cs')
s = p.read_text(encoding='utf-8')
old = '''        var variantes = new Dictionary<int, ProductoVariante>();
        foreach (var varianteId in varianteIds)
        {
            var variante = await _db.ProductoVariantes
                .FromSqlInterpolated($"SELECT v.* FROM ProductoVariantes v WHERE v.Id = {varianteId} AND v.Eliminado = 0 FOR UPDATE")
                .AsTracking().SingleOrDefaultAsync(ct)
                ?? throw new BusinessRuleException($"La variante ID '{varianteId}' ya no existe. Revalida el archivo.");
            variantes.Add(variante.Id, variante);
        }

        var marcas = await _db.Marcas.Where(x => x.Activo && !x.Eliminado).ToListAsync(ct);'''
new = '''        var variantes = new Dictionary<int, ProductoVariante>();
        foreach (var varianteId in varianteIds)
        {
            var variante = await _db.ProductoVariantes
                .FromSqlInterpolated($"SELECT v.* FROM ProductoVariantes v WHERE v.Id = {varianteId} AND v.Eliminado = 0 FOR UPDATE")
                .AsTracking().SingleOrDefaultAsync(ct)
                ?? throw new BusinessRuleException($"La variante ID '{varianteId}' ya no existe. Revalida el archivo.");
            variantes.Add(variante.Id, variante);
        }

        // N0.3: una familia no puede mezclar variante técnica y variantes comerciales activas.
        // Se bloquea la técnica dentro de la misma transacción para que la conversión sea atómica.
        var tecnicas = new Dictionary<int, ProductoVariante>();
        foreach (var productoId in productoIds)
        {
            var tecnica = await _db.ProductoVariantes
                .FromSqlInterpolated($"SELECT v.* FROM ProductoVariantes v WHERE v.ProductoId = {productoId} AND v.EsTecnica = 1 AND v.Eliminado = 0 FOR UPDATE")
                .AsTracking().SingleOrDefaultAsync(ct);
            if (tecnica is not null)
                tecnicas.Add(productoId, tecnica);
        }

        var marcas = await _db.Marcas.Where(x => x.Activo && !x.Eliminado).ToListAsync(ct);'''
if s.count(old) != 1:
    raise SystemExit(f'bloque tecnicas esperado: {s.count(old)}')
s = s.replace(old, new)
old2 = '''            else
            {
                var conflicto = await _db.ProductoVariantes.IgnoreQueryFilters().AsNoTracking().AnyAsync(x =>
                    !x.Eliminado &&
                    (x.Sku == sku ||'''
new2 = '''            else
            {
                if (tecnicas.TryGetValue(producto.Id, out var tecnica))
                {
                    if (tecnica.Cantidad != 0)
                        throw new BusinessRuleException($"El producto '{producto.Nombre}' conserva stock en su variante técnica. Ajusta o migra ese stock antes de crear variantes comerciales.");

                    tecnica.Activo = false;
                    tecnica.Eliminado = true;
                    tecnica.FechaEliminacion = DateTime.UtcNow;
                    tecnica.EliminadoPorUsuarioId = _currentUser.UsuarioId;
                    MarcarActualizacion(tecnica);
                    tecnicas.Remove(producto.Id);
                }

                var conflicto = await _db.ProductoVariantes.IgnoreQueryFilters().AsNoTracking().AnyAsync(x =>
                    !x.Eliminado && !x.EsTecnica &&
                    (x.Sku == sku ||'''
if s.count(old2) != 1:
    raise SystemExit(f'bloque creación esperado: {s.count(old2)}')
s = s.replace(old2, new2)
p.write_text(s, encoding='utf-8')

guard = Path('backend/scripts/check-erp-n0-3-runtime.py')
g = guard.read_text(encoding='utf-8')
marker = "require('backend/src/Infrastructure/Services/CargaMasivaService.cs','PRODUCTO_REQUIERE_VARIANTES')\n"
extra = "require('backend/src/Infrastructure/Services/CargaMasivaService.cs','conserva stock en su variante técnica')\nrequire('backend/src/Infrastructure/Services/CargaMasivaService.cs','tecnica.Eliminado = true')\n"
if extra not in g:
    if marker not in g:
        raise SystemExit('marcador runtime N0.3 no encontrado')
    guard.write_text(g.replace(marker, marker + extra), encoding='utf-8')

t = Path('backend/tests/InventoryApp.Tests/N03AutoridadOperativaRegressionTests.cs')
ts = t.read_text(encoding='utf-8')
anchor = '''    private static string Leer(string relativePath)
'''
test = '''    [Fact]
    public void Carga_variantes_debe_convertir_tecnica_sin_mezclar_autoridades()
    {
        var source = Leer("backend/src/Infrastructure/Services/CargaMasivaService.cs");
        Assert.Contains("conserva stock en su variante técnica", source);
        Assert.Contains("tecnica.Eliminado = true", source);
        Assert.Contains("!x.Eliminado && !x.EsTecnica", source);
    }

'''
if test not in ts:
    if anchor not in ts:
        raise SystemExit('anchor test no encontrado')
    t.write_text(ts.replace(anchor, test + anchor), encoding='utf-8')

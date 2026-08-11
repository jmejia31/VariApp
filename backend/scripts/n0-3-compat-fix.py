#!/usr/bin/env python3
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]

def read(rel): return (ROOT / rel).read_text(encoding='utf-8')
def write(rel, text): (ROOT / rel).write_text(text, encoding='utf-8')

# ProductoVariante: Producto queda como proyección derivada, no autoridad.
p='backend/src/Application/Services/ProductoVarianteService.cs'
t=read(p)
t=t.replace('await MarcarProductoActualizadoAsync(producto);', 'await SincronizarProyeccionCompatibilidadAsync(producto);')

# AsegurarTecnicaBajoLockAsync puede crear una técnica a partir de un producto pre-N0.3.
# Sembramos el legado UNA sola vez al crearla; una técnica ya existente nunca vuelve a copiarse desde Producto.
marker='''        var tecnica = await _repository.GetTecnicaByProductoIdAsync(producto.Id, true);
        if (tecnica is null)
        {'''
replacement_marker='''        var tecnica = await _repository.GetTecnicaByProductoIdAsync(producto.Id, true);
        var esNueva = tecnica is null;
        if (tecnica is null)
        {'''
if marker not in t: raise SystemExit('No se encontró marcador de alta técnica generada')
t=t.replace(marker,replacement_marker,1)
seed_marker='''        tecnica.EsTecnica = true;
        tecnica.CodigoBarras = null;
        tecnica.Activo = producto.Activo;'''
seed='''        if (esNueva)
        {
            tecnica.MarcaId = producto.MarcaId;
            tecnica.ModeloId = producto.ModeloId;
            tecnica.ColorId = producto.ColorId;
            tecnica.TallaId = producto.TallaId;
            tecnica.Cantidad = producto.Cantidad;
            tecnica.UmbralStockBajo = producto.UmbralStockBajo;
            tecnica.Costo = producto.Costo;
            tecnica.Precio = producto.Precio;
        }

        tecnica.EsTecnica = true;
        tecnica.CodigoBarras = null;
        tecnica.Activo = producto.Activo;'''
if seed_marker not in t: raise SystemExit('No se encontró punto de seed técnico generado')
t=t.replace(seed_marker,seed,1)

pattern=r'''    private async Task MarcarProductoActualizadoAsync\(Producto producto\)\n    \{.*?\n    \}\n'''
projection='''    // Compatibilidad transitoria N0.3: Producto conserva un espejo DERIVADO, nunca autoridad operativa.
    private async Task SincronizarProyeccionCompatibilidadAsync(Producto producto)
    {
        var variantes = await _repository.GetByProductoIdAsync(producto.Id, true);
        var activas = variantes.Where(v => v.Activo).ToList();
        var total = variantes.Sum(v => v.Cantidad);
        producto.Cantidad = total;
        producto.UmbralStockBajo = variantes.Sum(v => v.UmbralStockBajo);
        if (variantes.Count > 0)
        {
            producto.Costo = total > 0
                ? Math.Round(variantes.Sum(v => (v.Costo ?? 0m) * v.Cantidad) / total, 2, MidpointRounding.AwayFromZero)
                : Math.Round(variantes.Average(v => v.Costo ?? 0m), 2, MidpointRounding.AwayFromZero);
            var fuentePrecio = activas.Count > 0 ? activas : variantes;
            producto.Precio = fuentePrecio.Min(v => v.Precio ?? 0m);
            producto.MarcaId = ValorComun(variantes.Select(v => v.MarcaId));
            producto.ModeloId = ValorComun(variantes.Select(v => v.ModeloId));
            producto.ColorId = ValorComun(variantes.Select(v => v.ColorId));
            producto.TallaId = ValorComun(variantes.Select(v => v.TallaId));
            producto.Marca = producto.MarcaId.HasValue
                ? variantes.FirstOrDefault(v => v.MarcaId == producto.MarcaId)?.Marca?.Nombre ?? producto.Marca
                : string.Empty;
            producto.Modelo = producto.ModeloId.HasValue
                ? variantes.FirstOrDefault(v => v.ModeloId == producto.ModeloId)?.Modelo?.Nombre ?? producto.Modelo
                : string.Empty;
        }
        producto.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        producto.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        producto.FechaActualizacion = DateTime.UtcNow;
        await _productoRepository.SaveChangesAsync();
    }

    private static int? ValorComun(IEnumerable<int?> valores)
    {
        var lista = valores.Distinct().Take(2).ToList();
        return lista.Count == 1 ? lista[0] : null;
    }
'''
t2,n=re.subn(pattern,projection,t,count=1,flags=re.S)
if n!=1: raise SystemExit('No se reemplazó proyección compatibilidad')
write(p,t2)

# Mapper: Variante manda; fallback dimensional solo para técnica pre-backfill.
p='backend/src/Application/Mappings/ProductoMapper.cs'; t=read(p)
old='''        var marcaId = ValorComun(variantes, v => v.MarcaId);
        var modeloId = ValorComun(variantes, v => v.ModeloId);
        var colorId = ValorComun(variantes, v => v.ColorId);
        var tallaId = ValorComun(variantes, v => v.TallaId);'''
new='''        var soloTecnicaPreBackfill = variantes.Count == 1 && variantes[0].EsTecnica;
        var marcaId = ValorComun(variantes, v => v.MarcaId) ?? (soloTecnicaPreBackfill ? p.MarcaId : null);
        var modeloId = ValorComun(variantes, v => v.ModeloId) ?? (soloTecnicaPreBackfill ? p.ModeloId : null);
        var colorId = ValorComun(variantes, v => v.ColorId) ?? (soloTecnicaPreBackfill ? p.ColorId : null);
        var tallaId = ValorComun(variantes, v => v.TallaId) ?? (soloTecnicaPreBackfill ? p.TallaId : null);'''
if old not in t: raise SystemExit('Mapper IDs no localizado')
t=t.replace(old,new,1)
old_names='''        var marca = string.Join(" / ", marcaNombres!);
        var modelo = string.Join(" / ", modeloNombres!);'''
new_names='''        var marca = marcaNombres.Count > 0 ? string.Join(" / ", marcaNombres!) : (soloTecnicaPreBackfill ? p.Marca : string.Empty);
        var modelo = modeloNombres.Count > 0 ? string.Join(" / ", modeloNombres!) : (soloTecnicaPreBackfill ? p.Modelo : string.Empty);'''
if old_names not in t: raise SystemExit('Mapper nombres no localizado')
write(p,t.replace(old_names,new_names,1))

# Venta: cliente antiguo sin VarianteId resuelve técnica; comerciales exigen variante exacta.
p='backend/src/Application/Services/VentaService.cs'; t=read(p)
old='''            ProductoVariante? variante = null;
            if (input.ProductoVarianteId.HasValue)
            {
                variante = await ObtenerVarianteAsync(input.ProductoVarianteId.Value, producto.Id, exigirActiva: true);
            }
            else if (producto.Variantes.Any(v => v.Activo && !v.Eliminado))
            {
                throw new BusinessRuleException($"Debes seleccionar una variante para el producto '{producto.Nombre}'.");
            }'''
new='''            ProductoVariante? variante = null;
            if (input.ProductoVarianteId.HasValue)
            {
                variante = await ObtenerVarianteAsync(input.ProductoVarianteId.Value, producto.Id, exigirActiva: true);
            }
            else
            {
                variante = producto.Variantes.SingleOrDefault(v => v.EsTecnica && v.Activo && !v.Eliminado);
                if (variante is null && producto.Variantes.Any(v => !v.EsTecnica && v.Activo && !v.Eliminado))
                    throw new BusinessRuleException($"Debes seleccionar una variante para el producto '{producto.Nombre}'.");
            }'''
if old not in t: raise SystemExit('Venta ArmarDetalles no localizado')
t=t.replace(old,new,1)
old='''            ProductoVariante? variante = null;
            if (d.ProductoVarianteId.HasValue)
            {
                variante = await ObtenerVarianteAsync(d.ProductoVarianteId.Value, producto.Id, exigirActiva: true);
            }
            else if (producto.Variantes.Any(v => v.Activo && !v.Eliminado))
            {
                throw new BusinessRuleException($"Debes seleccionar una variante para el producto '{producto.Nombre}'.");
            }'''
new='''            ProductoVariante? variante = null;
            if (d.ProductoVarianteId.HasValue)
            {
                variante = await ObtenerVarianteAsync(d.ProductoVarianteId.Value, producto.Id, exigirActiva: true);
            }
            else
            {
                variante = producto.Variantes.SingleOrDefault(v => v.EsTecnica && v.Activo && !v.Eliminado);
                if (variante is null && producto.Variantes.Any(v => !v.EsTecnica && v.Activo && !v.Eliminado))
                    throw new BusinessRuleException($"Debes seleccionar una variante para el producto '{producto.Nombre}'.");
            }'''
if old not in t: raise SystemExit('Venta preview no localizado')
write(p,t.replace(old,new,1))

# Compra: mismo principio para producto simple.
p='backend/src/Application/Services/CompraService.cs'; t=read(p)
old='''            ProductoVariante? variante = null;
            if (input.ProductoVarianteId.HasValue)
            {
                variante = await ObtenerVarianteAsync(input.ProductoVarianteId.Value, producto.Id, exigirActiva: true);
            }
            else if (producto.Variantes.Any(v => v.Activo && !v.Eliminado))
            {
                throw new BusinessRuleException($"Debes seleccionar una variante para el producto '{producto.Nombre}'.");
            }'''
new='''            ProductoVariante? variante = null;
            if (input.ProductoVarianteId.HasValue)
            {
                variante = await ObtenerVarianteAsync(input.ProductoVarianteId.Value, producto.Id, exigirActiva: true);
            }
            else
            {
                variante = producto.Variantes.SingleOrDefault(v => v.EsTecnica && v.Activo && !v.Eliminado);
                if (variante is null && producto.Variantes.Any(v => !v.EsTecnica && v.Activo && !v.Eliminado))
                    throw new BusinessRuleException($"Debes seleccionar una variante para el producto '{producto.Nombre}'.");
            }'''
if old not in t: raise SystemExit('Compra ArmarDetalles no localizado')
write(p,t.replace(old,new,1))

# Concurrencia: Producto.Cantidad solo sirve como fallback histórico si falta VarianteId.
p='backend/src/Infrastructure/Services/InventarioConcurrencyService.cs'; t=read(p)
old='''            var cantidadTotalProducto = productoGrupo.Sum(x => x.Cantidad);
            if (esDeduccion && producto.Cantidad < cantidadTotalProducto)
            {
                throw new BusinessRuleException(
                    $"Stock insuficiente para '{producto.Nombre}': disponible {producto.Cantidad}, solicitado {cantidadTotalProducto}.");
            }'''
new='''            var demandasLegacySinVariante = productoGrupo.Where(x => !x.ProductoVarianteId.HasValue).ToList();
            var cantidadLegacy = demandasLegacySinVariante.Sum(x => x.Cantidad);
            if (esDeduccion && cantidadLegacy > 0 && producto.Cantidad < cantidadLegacy)
            {
                throw new BusinessRuleException(
                    $"Stock insuficiente para '{producto.Nombre}': disponible {producto.Cantidad}, solicitado {cantidadLegacy}.");
            }'''
if old not in t: raise SystemExit('Concurrencia agregado no localizado')
write(p,t.replace(old,new,1))

# Carga de variantes: espejo completo derivado.
p='backend/src/Infrastructure/Services/CargaMasivaService.cs'; t=read(p)
old='''                var activas = lista.Where(x => x.Activo).ToList();
                producto.Precio = (activas.Count > 0 ? activas : lista).Min(x => x.Precio ?? producto.Precio);
            }
            MarcarActualizacion(producto);'''
new='''                var activas = lista.Where(x => x.Activo).ToList();
                producto.Precio = (activas.Count > 0 ? activas : lista).Min(x => x.Precio ?? 0m);
                producto.UmbralStockBajo = lista.Sum(x => x.UmbralStockBajo);
                producto.MarcaId = ValorComunCompat(lista.Select(x => x.MarcaId));
                producto.ModeloId = ValorComunCompat(lista.Select(x => x.ModeloId));
                producto.ColorId = ValorComunCompat(lista.Select(x => x.ColorId));
                producto.TallaId = ValorComunCompat(lista.Select(x => x.TallaId));
            }
            MarcarActualizacion(producto);'''
if old not in t: raise SystemExit('Carga proyección no localizada')
t=t.replace(old,new,1)
marker='''    private static CargaMasivaDto MapResumenExpression(CargaMasiva x) => new()'''
helper='''    private static int? ValorComunCompat(IEnumerable<int?> valores)
    {
        var lista = valores.Distinct().Take(2).ToList();
        return lista.Count == 1 ? lista[0] : null;
    }

'''
if marker not in t: raise SystemExit('Carga marker helper no localizado')
write(p,t.replace(marker,helper+marker,1))

# Guardia semántica final.
write('backend/scripts/check-erp-n0-3-runtime.py', r'''#!/usr/bin/env python3
from pathlib import Path
import sys
root=Path(__file__).resolve().parents[2]
errors=[]
def require(rel,text):
    if text not in (root/rel).read_text(encoding='utf-8'): errors.append(f'{rel}: falta {text}')
def forbid(rel,text):
    if text in (root/rel).read_text(encoding='utf-8'): errors.append(f'{rel}: dependencia legacy prohibida {text}')
require('backend/src/Application/Services/ProductoVarianteService.cs','SincronizarProyeccionCompatibilidadAsync')
require('backend/src/Application/Services/ProductoVarianteService.cs','public async Task<ProductoVarianteDto> SincronizarTecnicaAsync')
require('backend/src/Application/Services/VentaService.cs','SingleOrDefault(v => v.EsTecnica && v.Activo && !v.Eliminado)')
require('backend/src/Application/Services/CompraService.cs','SingleOrDefault(v => v.EsTecnica && v.Activo && !v.Eliminado)')
require('backend/src/Infrastructure/Services/InventarioConcurrencyService.cs','demandasLegacySinVariante')
require('backend/src/Infrastructure/Repositories/ProductoRepository.cs','ProductoVariantes')
forbid('backend/src/API/Controllers/ProductosController.cs','AplicarProyeccionLegacy(dto)')
forbid('backend/src/Application/Services/ProductoService.cs','producto.Cantidad = dto.Cantidad')
forbid('backend/src/Application/Services/ProductoService.cs','producto.Costo = dto.Costo')
forbid('backend/src/Application/Services/ProductoService.cs','producto.Precio = dto.Precio')
forbid('backend/src/Infrastructure/Repositories/ProductoRepository.cs','v.Costo ?? v.Producto.Costo')
forbid('backend/src/Infrastructure/Repositories/ProductoRepository.cs','v.Precio ?? v.Producto.Precio')
forbid('backend/src/Application/Services/ProductoEscanerService.cs','variante.Producto.Costo')
forbid('backend/src/Application/Services/ProductoEscanerService.cs','variante.Producto.Precio')
if errors:
    print('N0.3 FAIL:\n'+'\n'.join(errors),file=sys.stderr); sys.exit(1)
print('N0.3 runtime guard: ProductoVariante es autoridad; Producto queda solo como proyección de compatibilidad.')
''')

print('Ajuste N0.3 de compatibilidad derivada aplicado.')

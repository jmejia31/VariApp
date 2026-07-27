from pathlib import Path


def replace_once(path: str, old: str, new: str) -> None:
    file = Path(path)
    text = file.read_text(encoding='utf-8')
    if old not in text:
        raise RuntimeError(f'No se encontró el bloque esperado en {path}: {old[:100]!r}')
    file.write_text(text.replace(old, new, 1), encoding='utf-8')


# DTOs backend: la imagen es información derivada, sin migración ni cambio de esquema.
replace_once(
    'backend/src/Application/DTOs/CompraDto.cs',
    '    public string ProductoModelo { get; set; } = string.Empty;\n    public int Cantidad { get; set; }',
    '    public string ProductoModelo { get; set; } = string.Empty;\n    public string? ProductoImagenPrincipalUrl { get; set; }\n    public int Cantidad { get; set; }'
)
replace_once(
    'backend/src/Application/DTOs/VentaDto.cs',
    '    public string ProductoModelo { get; set; } = string.Empty;\n    public int Cantidad { get; set; }',
    '    public string ProductoModelo { get; set; } = string.Empty;\n    public string? ProductoImagenPrincipalUrl { get; set; }\n    public int Cantidad { get; set; }'
)
replace_once(
    'backend/src/Application/DTOs/MovimientoInventarioDto.cs',
    '    public string ProductoNombre { get; set; } = string.Empty;\n    public string Tipo { get; set; } = string.Empty;',
    '    public string ProductoNombre { get; set; } = string.Empty;\n    public string? ProductoImagenPrincipalUrl { get; set; }\n    public string Tipo { get; set; } = string.Empty;'
)

# Los repositorios cargan únicamente la colección de imágenes ya existente.
replace_once(
    'backend/src/Infrastructure/Repositories/CompraRepository.cs',
    '    private IQueryable<Compra> ConIncludes() =>\n        _context.Compras.Include(c => c.Detalles).ThenInclude(d => d.Producto)\n            .Include(c => c.ImpuestosAplicados);',
    '    private IQueryable<Compra> ConIncludes() =>\n        _context.Compras\n            .Include(c => c.Detalles)\n                .ThenInclude(d => d.Producto)\n                    .ThenInclude(p => p!.Imagenes)\n            .Include(c => c.ImpuestosAplicados)\n            .AsSplitQuery();'
)
replace_once(
    'backend/src/Infrastructure/Repositories/VentaRepository.cs',
    '    private IQueryable<Venta> ConIncludes() =>\n        _context.Ventas.Include(v => v.Detalles).ThenInclude(d => d.Producto)\n            .Include(v => v.Factura)\n            .Include(v => v.DescuentosAplicados)\n            .Include(v => v.ImpuestosAplicados);',
    '    private IQueryable<Venta> ConIncludes() =>\n        _context.Ventas\n            .Include(v => v.Detalles)\n                .ThenInclude(d => d.Producto)\n                    .ThenInclude(p => p!.Imagenes)\n            .Include(v => v.Factura)\n            .Include(v => v.DescuentosAplicados)\n            .Include(v => v.ImpuestosAplicados)\n            .AsSplitQuery();'
)
replace_once(
    'backend/src/Infrastructure/Repositories/MovimientoInventarioRepository.cs',
    '            _context.MovimientosInventario.Include(m => m.Producto).AsQueryable(),',
    '            _context.MovimientosInventario\n                .Include(m => m.Producto)\n                    .ThenInclude(p => p!.Imagenes)\n                .AsSplitQuery(),'
)

# Mapeos de salida.
replace_once(
    'backend/src/Application/Services/CompraService.cs',
    '            ProductoModelo = detalle.ProductoModeloSnapshot,\n            Cantidad = detalle.Cantidad,',
    '            ProductoModelo = detalle.ProductoModeloSnapshot,\n            ProductoImagenPrincipalUrl = detalle.Producto?.ImagenPrincipal?.Url,\n            Cantidad = detalle.Cantidad,'
)
replace_once(
    'backend/src/Application/Services/VentaService.cs',
    '            ProductoModelo = d.ProductoModeloSnapshot,\n            Cantidad = d.Cantidad,',
    '            ProductoModelo = d.ProductoModeloSnapshot,\n            ProductoImagenPrincipalUrl = d.Producto?.ImagenPrincipal?.Url,\n            Cantidad = d.Cantidad,'
)
replace_once(
    'backend/src/Application/Services/MovimientoInventarioService.cs',
    '        ProductoNombre = m.Producto?.Nombre ?? "(producto eliminado)",\n        Tipo = m.Tipo.ToString(),',
    '        ProductoNombre = m.Producto?.Nombre ?? "(producto eliminado)",\n        ProductoImagenPrincipalUrl = m.Producto?.ImagenPrincipal?.Url,\n        Tipo = m.Tipo.ToString(),'
)

# Contratos frontend.
replace_once(
    'frontend/src/app/core/models/compra.model.ts',
    '  productoModelo: string;\n  cantidad: number;',
    '  productoModelo: string;\n  productoImagenPrincipalUrl?: string;\n  cantidad: number;'
)
replace_once(
    'frontend/src/app/core/models/venta.model.ts',
    '  productoModelo: string;\n  cantidad: number;',
    '  productoModelo: string;\n  productoImagenPrincipalUrl?: string;\n  cantidad: number;'
)
replace_once(
    'frontend/src/app/core/models/movimiento-inventario.model.ts',
    '  productoNombre: string;\n  tipo:',
    '  productoNombre: string;\n  productoImagenPrincipalUrl?: string;\n  tipo:'
)

# Importaciones del componente compartido.
for path, anchor in [
    ('frontend/src/app/features/productos/productos-list.component.ts', "import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';"),
    ('frontend/src/app/features/productos/producto-form.component.ts', "import { ProductoImagen } from '../../core/models/producto.model';"),
    ('frontend/src/app/features/productos/producto-detail.component.ts', "import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';"),
    ('frontend/src/app/features/compras/compra-form.component.ts', "import { ResultadoCalculo } from '../../core/models/compra.model';"),
    ('frontend/src/app/features/compras/compra-detail.component.ts', "import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';"),
    ('frontend/src/app/features/compras/compras-list.component.ts', "import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';"),
    ('frontend/src/app/features/ventas/venta-form.component.ts', "import { ResultadoCalculo } from '../../core/models/venta.model';"),
    ('frontend/src/app/features/ventas/venta-detail.component.ts', "import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';"),
    ('frontend/src/app/features/ventas/ventas-list.component.ts', "import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';"),
    ('frontend/src/app/features/inventario/movimientos-list.component.ts', "import { MovimientoInventario } from '../../core/models/movimiento-inventario.model';")
]:
    replace_once(path, anchor, anchor + "\nimport { ProductoImagenComponent } from '../../shared/producto-imagen/producto-imagen.component';")

# El detalle de producto necesita Escape para cerrar la ampliación.
replace_once(
    'frontend/src/app/features/productos/producto-detail.component.ts',
    "import { Component, OnInit, signal } from '@angular/core';",
    "import { Component, HostListener, OnInit, signal } from '@angular/core';"
)
replace_once(
    'frontend/src/app/features/productos/producto-detail.component.ts',
    '  cerrarAmpliada(): void {\n    this.imagenAmpliada.set(null);\n  }',
    "  @HostListener('document:keydown.escape')\n  cerrarAmpliada(): void {\n    this.imagenAmpliada.set(null);\n  }"
)

# Agregar el componente a imports standalone.
replacements = {
    'frontend/src/app/features/productos/productos-list.component.ts': (
        '    MatProgressSpinnerModule, MatDialogModule, MatSlideToggleModule\n',
        '    MatProgressSpinnerModule, MatDialogModule, MatSlideToggleModule, ProductoImagenComponent\n'
    ),
    'frontend/src/app/features/productos/producto-form.component.ts': (
        '    MatInputModule, MatSelectModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule\n',
        '    MatInputModule, MatSelectModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, ProductoImagenComponent\n'
    ),
    'frontend/src/app/features/productos/producto-detail.component.ts': (
        '  imports: [CommonModule, RouterLink, MatIconModule, MatButtonModule, MatProgressSpinnerModule],',
        '  imports: [CommonModule, RouterLink, MatIconModule, MatButtonModule, MatProgressSpinnerModule, ProductoImagenComponent],'
    ),
    'frontend/src/app/features/compras/compra-form.component.ts': (
        '    MatSelectModule, MatAutocompleteModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule\n',
        '    MatSelectModule, MatAutocompleteModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, ProductoImagenComponent\n'
    ),
    'frontend/src/app/features/compras/compra-detail.component.ts': (
        '  imports: [CommonModule, RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatDialogModule],',
        '  imports: [CommonModule, RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatDialogModule, ProductoImagenComponent],'
    ),
    'frontend/src/app/features/compras/compras-list.component.ts': (
        '  imports: [CommonModule, RouterLink, FormsModule, MatIconModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatPaginatorModule, MatProgressSpinnerModule],',
        '  imports: [CommonModule, RouterLink, FormsModule, MatIconModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatPaginatorModule, MatProgressSpinnerModule, ProductoImagenComponent],'
    ),
    'frontend/src/app/features/ventas/venta-form.component.ts': (
        '    MatSelectModule, MatAutocompleteModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule\n',
        '    MatSelectModule, MatAutocompleteModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, ProductoImagenComponent\n'
    ),
    'frontend/src/app/features/ventas/venta-detail.component.ts': (
        '  imports: [CommonModule, RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatDialogModule],',
        '  imports: [CommonModule, RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatDialogModule, ProductoImagenComponent],'
    ),
    'frontend/src/app/features/ventas/ventas-list.component.ts': (
        '  imports: [CommonModule, RouterLink, FormsModule, MatIconModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatPaginatorModule, MatProgressSpinnerModule],',
        '  imports: [CommonModule, RouterLink, FormsModule, MatIconModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatPaginatorModule, MatProgressSpinnerModule, ProductoImagenComponent],'
    ),
    'frontend/src/app/features/inventario/movimientos-list.component.ts': (
        '    MatProgressSpinnerModule, MatIconModule\n',
        '    MatProgressSpinnerModule, MatIconModule, ProductoImagenComponent\n'
    )
}
for path, (old, new) in replacements.items():
    replace_once(path, old, new)

# Helpers para las imágenes seleccionadas en los formularios.
for path in [
    'frontend/src/app/features/compras/compra-form.component.ts',
    'frontend/src/app/features/ventas/venta-form.component.ts'
]:
    replace_once(
        path,
        '  quitarDetalle(index: number): void {',
        "  productoSeleccionado(group: AbstractControl): Producto | undefined {\n    const productoId = Number(group.value.productoId);\n    return this.productos().find((producto) => producto.id === productoId);\n  }\n\n  quitarDetalle(index: number): void {"
    )

# Pruebas unitarias del contrato de imagen principal.
replace_once(
    'backend/tests/InventoryApp.Tests/CompraServiceTests.cs',
    '\n}\n',
    '''

    [Fact]
    public async Task GetByIdAsync_Incluye_Imagen_Principal_Del_Producto()
    {
        var producto = ProductoDePrueba();
        producto.Imagenes.Add(new ProductoImagen
        {
            Id = 10,
            Url = "https://res.cloudinary.com/demo/image/upload/producto-principal.webp",
            EsPrincipal = true,
            Orden = 0
        });
        var compra = new Compra { Id = 7, NumeroCompra = "COM-000007", ProveedorNombre = "Proveedor X" };
        compra.Detalles.Add(new CompraDetalle
        {
            Id = 4,
            ProductoId = producto.Id,
            Producto = producto,
            ProductoNombreSnapshot = producto.Nombre,
            ProductoMarcaSnapshot = producto.Marca,
            ProductoModeloSnapshot = producto.Modelo,
            Cantidad = 1,
            CostoUnitario = 5,
            Subtotal = 5
        });
        _compraRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(compra);

        var resultado = await _service.GetByIdAsync(7);

        Assert.Equal(
            "https://res.cloudinary.com/demo/image/upload/producto-principal.webp",
            resultado!.Detalles.Single().ProductoImagenPrincipalUrl);
    }
}
'''
)
replace_once(
    'backend/tests/InventoryApp.Tests/VentaServiceTests.cs',
    '\n}\n',
    '''

    [Fact]
    public async Task GetByIdAsync_Incluye_Imagen_Principal_Del_Producto()
    {
        var producto = ProductoDePrueba();
        producto.Imagenes.Add(new ProductoImagen
        {
            Id = 11,
            Url = "https://res.cloudinary.com/demo/image/upload/producto-venta.webp",
            EsPrincipal = true,
            Orden = 0
        });
        var venta = VentaDePrueba(cantidadDetalle: 1);
        venta.Detalles.Single().Producto = producto;
        _ventaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(venta);

        var resultado = await _service.GetByIdAsync(1);

        Assert.Equal(
            "https://res.cloudinary.com/demo/image/upload/producto-venta.webp",
            resultado!.Detalles.Single().ProductoImagenPrincipalUrl);
    }
}
'''
)

Path('backend/tests/InventoryApp.Tests/MovimientoInventarioServiceTests.cs').write_text('''using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public class MovimientoInventarioServiceTests
{
    [Fact]
    public async Task GetFilteredAsync_Incluye_Imagen_Principal_Del_Producto()
    {
        var producto = new Producto { Id = 2, Nombre = "Teclado", Marca = "Logitech", Modelo = "K120" };
        producto.Imagenes.Add(new ProductoImagen
        {
            Id = 22,
            Url = "https://res.cloudinary.com/demo/image/upload/teclado.webp",
            EsPrincipal = true,
            Orden = 0
        });
        var movimiento = new MovimientoInventario
        {
            Id = 3,
            ProductoId = producto.Id,
            Producto = producto,
            Tipo = TipoMovimientoInventario.Entrada,
            Cantidad = 2,
            StockAnterior = 0,
            StockNuevo = 2,
            ReferenciaTipo = "Compra",
            ReferenciaId = 5
        };
        var repository = new Mock<IMovimientoInventarioRepository>();
        repository
            .Setup(r => r.GetFilteredAsync(null, null, null, null))
            .ReturnsAsync(new List<MovimientoInventario> { movimiento });
        var service = new MovimientoInventarioService(repository.Object);

        var resultado = await service.GetFilteredAsync(null, null, null, null);

        Assert.Equal("https://res.cloudinary.com/demo/image/upload/teclado.webp", resultado.Single().ProductoImagenPrincipalUrl);
    }
}
''', encoding='utf-8')

print('Contratos, repositorios, mapeos, imports y pruebas de Fase 5 actualizados.')

using InventoryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<ProductoImagen> ProductoImagenes => Set<ProductoImagen>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<RolPermiso> RolPermisos => Set<RolPermiso>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<CompraDetalle> CompraDetalles => Set<CompraDetalle>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
    public DbSet<MovimientoFinanciero> MovimientosFinancieros => Set<MovimientoFinanciero>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<VentaDetalle> VentaDetalles => Set<VentaDetalle>();
    public DbSet<Factura> Facturas => Set<Factura>();
    public DbSet<FacturaDetalle> FacturaDetalles => Set<FacturaDetalle>();
    public DbSet<EmpresaConfiguracion> EmpresaConfiguraciones => Set<EmpresaConfiguracion>();
    public DbSet<RevisionFinanciera> RevisionesFinancieras => Set<RevisionFinanciera>();
    public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Permiso> Permisos => Set<Permiso>();

    public DbSet<Descuento> Descuentos => Set<Descuento>();
    public DbSet<DescuentoProducto> DescuentoProductos => Set<DescuentoProducto>();
    public DbSet<DescuentoCategoria> DescuentoCategorias => Set<DescuentoCategoria>();
    public DbSet<DescuentoCliente> DescuentoClientes => Set<DescuentoCliente>();
    public DbSet<DescuentoRol> DescuentoRoles => Set<DescuentoRol>();
    public DbSet<HistorialUsoDescuento> HistorialUsoDescuentos => Set<HistorialUsoDescuento>();
    public DbSet<VentaDescuento> VentaDescuentos => Set<VentaDescuento>();

    public DbSet<Impuesto> Impuestos => Set<Impuesto>();
    public DbSet<ImpuestoProducto> ImpuestoProductos => Set<ImpuestoProducto>();
    public DbSet<ImpuestoCategoria> ImpuestoCategorias => Set<ImpuestoCategoria>();
    public DbSet<ImpuestoOperacion> ImpuestoOperaciones => Set<ImpuestoOperacion>();
    public DbSet<ImpuestoCliente> ImpuestoClientes => Set<ImpuestoCliente>();
    public DbSet<ImpuestoProveedor> ImpuestoProveedores => Set<ImpuestoProveedor>();
    public DbSet<HistorialAplicacionImpuesto> HistorialAplicacionImpuestos => Set<HistorialAplicacionImpuesto>();
    public DbSet<VentaImpuesto> VentaImpuestos => Set<VentaImpuesto>();
    public DbSet<CompraImpuesto> CompraImpuestos => Set<CompraImpuesto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

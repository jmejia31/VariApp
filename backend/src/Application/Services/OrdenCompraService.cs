using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Services;

public sealed class OrdenCompraService : IOrdenCompraService
{
    private const string EntidadAuditoria = "OrdenCompra";

    private readonly IOrdenCompraRepository _repository;
    private readonly IProveedorRepository _proveedores;
    private readonly ISolicitudCompraRepository _solicitudes;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditoriaService _auditoria;

    public OrdenCompraService(
        IOrdenCompraRepository repository,
        IProveedorRepository proveedores,
        ISolicitudCompraRepository solicitudes,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IAuditoriaService auditoria)
    {
        _repository = repository;
        _proveedores = proveedores;
        _solicitudes = solicitudes;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _auditoria = auditoria;
    }

    public async Task<PagedResult<OrdenCompraDto>> GetPagedAsync(OrdenCompraFiltroDto filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde.Value > filtro.Hasta.Value)
            throw new ArgumentException("El rango de fechas es inválido.", nameof(filtro));

        filtro.Page = Math.Max(1, filtro.Page);
        filtro.PageSize = Math.Clamp(filtro.PageSize, 1, 100);
        var (items, total) = await _repository.GetPagedAsync(filtro);
        return new PagedResult<OrdenCompraDto>
        {
            Items = items.Select(Map).ToList(),
            Page = filtro.Page,
            PageSize = filtro.PageSize,
            TotalCount = total
        };
    }

    public async Task<OrdenCompraDto?> GetByIdAsync(int id)
    {
        if (id <= 0) return null;
        var orden = await _repository.GetByIdAsync(id);
        return orden is null ? null : Map(orden);
    }

    public async Task<OrdenCompraDto> CreateAsync(CreateOrdenCompraDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidarDetalles(dto.Detalles);
        var proveedor = await RequerirProveedorAsync(dto.ProveedorId);
        await ValidarSolicitudAsync(dto.SolicitudCompraId, dto.ProveedorId);

        var orden = new OrdenCompra
        {
            NumeroOrden = await GenerarNumeroAsync(),
            SolicitudCompraId = dto.SolicitudCompraId,
            ProveedorId = proveedor.Id,
            ProveedorNombreSnapshot = proveedor.Nombre,
            ProveedorDocumentoSnapshot = Normalizar(proveedor.Documento),
            Moneda = NormalizarMoneda(dto.Moneda),
            CondicionesCompra = Normalizar(dto.CondicionesCompra),
            FechaEsperadaUtc = dto.FechaEsperadaUtc,
            Observaciones = Normalizar(dto.Observaciones),
            Detalles = dto.Detalles.Select(CrearDetalle).ToList()
        };
        orden.ValidarDocumento();

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _repository.AddAsync(orden);
            await _repository.SaveChangesAsync();
            await RegistrarAuditoriaAsync(AccionPermiso.Crear, "Orden de compra creada", orden, valoresNuevos: Snapshot(orden));
        });

        return Map(orden);
    }

    public async Task<OrdenCompraDto> UpdateAsync(int id, UpdateOrdenCompraDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidarId(id);
        ValidarDetalles(dto.Detalles);
        var proveedor = await RequerirProveedorAsync(dto.ProveedorId);
        await ValidarSolicitudAsync(dto.SolicitudCompraId, dto.ProveedorId);
        var detalles = dto.Detalles.Select(CrearDetalle).ToList();
        foreach (var detalle in detalles) detalle.Validar();

        OrdenCompra? resultado = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var orden = await RequerirForUpdateAsync(id);
            orden.AsegurarEditable();
            var anterior = Snapshot(orden);

            orden.SolicitudCompraId = dto.SolicitudCompraId;
            orden.ProveedorId = proveedor.Id;
            orden.ProveedorNombreSnapshot = proveedor.Nombre;
            orden.ProveedorDocumentoSnapshot = Normalizar(proveedor.Documento);
            orden.Moneda = NormalizarMoneda(dto.Moneda);
            orden.CondicionesCompra = Normalizar(dto.CondicionesCompra);
            orden.FechaEsperadaUtc = dto.FechaEsperadaUtc;
            orden.Observaciones = Normalizar(dto.Observaciones);
            orden.Detalles.Clear();
            foreach (var detalle in detalles) orden.Detalles.Add(detalle);
            orden.ValidarDocumento();

            await _repository.SaveChangesAsync();
            await RegistrarAuditoriaAsync(AccionPermiso.Editar, "Orden de compra editada", orden, anterior, Snapshot(orden));
            resultado = orden;
        });

        return Map(resultado!);
    }

    public async Task<OrdenCompraDto> EnviarAprobacionAsync(int id)
    {
        ValidarId(id);
        var (usuarioId, _) = RequerirUsuario();
        OrdenCompra? resultado = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var orden = await RequerirForUpdateAsync(id);
            var anterior = Snapshot(orden);
            orden.EnviarAprobacion(usuarioId, DateTime.UtcNow);
            await _repository.SaveChangesAsync();
            await RegistrarAuditoriaAsync(AccionPermiso.Confirmar, "Orden de compra enviada a aprobación", orden, anterior, Snapshot(orden));
            resultado = orden;
        });
        return Map(resultado!);
    }

    public async Task<OrdenCompraDto> AprobarAsync(int id)
    {
        ValidarId(id);
        var (usuarioId, nombre) = RequerirUsuario();
        OrdenCompra? resultado = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var orden = await RequerirForUpdateAsync(id);
            var anterior = Snapshot(orden);
            orden.Aprobar(usuarioId, nombre, DateTime.UtcNow);
            await _repository.SaveChangesAsync();
            await RegistrarAuditoriaAsync(AccionPermiso.Aprobar, "Orden de compra aprobada", orden, anterior, Snapshot(orden));
            resultado = orden;
        });
        return Map(resultado!);
    }

    public async Task<OrdenCompraDto> CancelarAsync(int id, CancelarOrdenCompraDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidarId(id);
        var motivo = Normalizar(dto.Motivo) ?? throw new ArgumentException("El motivo de cancelación es obligatorio.", nameof(dto));
        var (usuarioId, _) = RequerirUsuario();
        OrdenCompra? resultado = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var orden = await RequerirForUpdateAsync(id);
            var anterior = Snapshot(orden);
            orden.Cancelar(usuarioId, motivo, DateTime.UtcNow);
            await _repository.SaveChangesAsync();
            await RegistrarAuditoriaAsync(AccionPermiso.Anular, "Orden de compra cancelada", orden, anterior, Snapshot(orden), motivo);
            resultado = orden;
        });
        return Map(resultado!);
    }

    private async Task<Proveedor> RequerirProveedorAsync(int proveedorId)
    {
        if (proveedorId <= 0) throw new ArgumentOutOfRangeException(nameof(proveedorId));
        return await _proveedores.GetByIdAsync(proveedorId)
            ?? throw new KeyNotFoundException("Proveedor no encontrado.");
    }

    private async Task ValidarSolicitudAsync(int? solicitudId, int proveedorId)
    {
        if (!solicitudId.HasValue) return;
        if (solicitudId.Value <= 0) throw new ArgumentOutOfRangeException(nameof(solicitudId));
        var solicitud = await _solicitudes.GetByIdAsync(solicitudId.Value)
            ?? throw new KeyNotFoundException("Solicitud de compra no encontrada.");
        if (solicitud.Estado != EstadoSolicitudCompra.Aprobada)
            throw new InvalidOperationException("La orden solo puede vincular una solicitud de compra aprobada.");
        if (solicitud.ProveedorId.HasValue && solicitud.ProveedorId.Value != proveedorId)
            throw new InvalidOperationException("El proveedor de la orden no coincide con el de la solicitud aprobada.");
    }

    private async Task<OrdenCompra> RequerirForUpdateAsync(int id) =>
        await _repository.GetByIdForUpdateAsync(id)
            ?? throw new KeyNotFoundException("Orden de compra no encontrada.");

    private async Task<string> GenerarNumeroAsync()
    {
        for (var intento = 0; intento < 5; intento++)
        {
            var numero = $"OC-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}"[..32].ToUpperInvariant();
            if (!await _repository.ExisteNumeroAsync(numero)) return numero;
        }
        throw new InvalidOperationException("No fue posible generar un número único de orden de compra.");
    }

    private Task RegistrarAuditoriaAsync(
        AccionPermiso accion,
        string descripcion,
        OrdenCompra orden,
        object? valoresAnteriores = null,
        object? valoresNuevos = null,
        string? motivo = null) =>
        _auditoria.RegistrarEstrictoAsync(
            ModuloSistema.Compras,
            accion,
            descripcion,
            referenciaId: orden.Id,
            entidad: EntidadAuditoria,
            valoresAnteriores: valoresAnteriores,
            valoresNuevos: valoresNuevos,
            motivo: motivo);

    private static object Snapshot(OrdenCompra orden) => new
    {
        orden.NumeroOrden,
        Estado = orden.Estado.ToString(),
        orden.SolicitudCompraId,
        orden.ProveedorId,
        orden.Moneda,
        Lineas = orden.Detalles.Count,
        orden.Subtotal,
        orden.Descuento,
        orden.Impuesto,
        orden.Total,
        orden.FechaEnvioAprobacionUtc,
        orden.FechaAprobacionUtc,
        orden.FechaCancelacionUtc
    };

    private (int UsuarioId, string? Nombre) RequerirUsuario()
    {
        if (!_currentUser.EstaAutenticado || _currentUser.UsuarioId is not > 0)
            throw new InvalidOperationException("Se requiere un usuario autenticado para esta transición.");
        return (_currentUser.UsuarioId.Value, _currentUser.NombreCompleto ?? _currentUser.NombreUsuario);
    }

    private static void ValidarId(int id)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
    }

    private static void ValidarDetalles(IReadOnlyCollection<OrdenCompraDetalleInputDto>? detalles)
    {
        if (detalles is null || detalles.Count == 0)
            throw new ArgumentException("La orden de compra debe contener al menos un detalle.", nameof(detalles));
        foreach (var detalle in detalles)
        {
            if (detalle.ProductoId <= 0) throw new ArgumentException("Cada detalle debe indicar un producto válido.", nameof(detalles));
            if (detalle.ProductoVarianteId is <= 0) throw new ArgumentException("La variante debe ser válida cuando se especifica.", nameof(detalles));
            if (detalle.CantidadOrdenada <= 0) throw new ArgumentException("La cantidad ordenada debe ser mayor que cero.", nameof(detalles));
            if (detalle.PrecioUnitario < 0 || detalle.Descuento < 0 || detalle.Impuesto < 0)
                throw new ArgumentException("Los importes del detalle no pueden ser negativos.", nameof(detalles));
            if (detalle.Descuento > detalle.CantidadOrdenada * detalle.PrecioUnitario)
                throw new ArgumentException("El descuento no puede superar el subtotal del detalle.", nameof(detalles));
        }
    }

    private static OrdenCompraDetalle CrearDetalle(OrdenCompraDetalleInputDto input)
    {
        var detalle = new OrdenCompraDetalle
        {
            ProductoId = input.ProductoId,
            ProductoVarianteId = input.ProductoVarianteId,
            Observacion = Normalizar(input.Observacion)
        };
        detalle.EstablecerValores(input.CantidadOrdenada, input.PrecioUnitario, input.Descuento, input.Impuesto);
        return detalle;
    }

    private static OrdenCompraDto Map(OrdenCompra orden) => new()
    {
        Id = orden.Id,
        NumeroOrden = orden.NumeroOrden,
        Estado = orden.Estado,
        SolicitudCompraId = orden.SolicitudCompraId,
        ProveedorId = orden.ProveedorId,
        ProveedorNombre = orden.ProveedorNombreSnapshot,
        Moneda = orden.Moneda,
        CondicionesCompra = orden.CondicionesCompra,
        FechaEsperadaUtc = orden.FechaEsperadaUtc,
        Observaciones = orden.Observaciones,
        Subtotal = orden.Subtotal,
        Descuento = orden.Descuento,
        Impuesto = orden.Impuesto,
        Total = orden.Total,
        FechaEnvioAprobacionUtc = orden.FechaEnvioAprobacionUtc,
        FechaAprobacionUtc = orden.FechaAprobacionUtc,
        FechaCancelacionUtc = orden.FechaCancelacionUtc,
        Detalles = orden.Detalles.Select(d => new OrdenCompraDetalleDto
        {
            Id = d.Id,
            ProductoId = d.ProductoId,
            ProductoVarianteId = d.ProductoVarianteId,
            CantidadOrdenada = d.CantidadOrdenada,
            PrecioUnitario = d.PrecioUnitario,
            Descuento = d.Descuento,
            Impuesto = d.Impuesto,
            Subtotal = d.Subtotal,
            Total = d.Total,
            Observacion = d.Observacion,
            ProductoSkuSnapshot = d.ProductoSkuSnapshot,
            ProductoNombreSnapshot = d.ProductoNombreSnapshot
        }).ToList()
    };

    private static string NormalizarMoneda(string? moneda)
    {
        var valor = string.IsNullOrWhiteSpace(moneda) ? "HNL" : moneda.Trim().ToUpperInvariant();
        if (valor.Length != 3 || !valor.All(char.IsLetter))
            throw new ArgumentException("La moneda debe usar un código ISO de tres letras.", nameof(moneda));
        return valor;
    }

    private static string? Normalizar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}

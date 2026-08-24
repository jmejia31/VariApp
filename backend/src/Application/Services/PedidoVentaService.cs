using System.Security.Cryptography;
using System.Text;
using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Services;

public sealed class PedidoVentaService : IPedidoVentaService
{
    private readonly IPedidoVentaRepository _repository;
    private readonly ICotizacionRepository _cotizacionRepository;
    private readonly IAuditoriaService _auditoriaService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public PedidoVentaService(
        IPedidoVentaRepository repository,
        ICotizacionRepository cotizacionRepository,
        IAuditoriaService auditoriaService,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _cotizacionRepository = cotizacionRepository ?? throw new ArgumentNullException(nameof(cotizacionRepository));
        _auditoriaService = auditoriaService ?? throw new ArgumentNullException(nameof(auditoriaService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<PedidoVentaDto> GetByIdAsync(int id)
    {
        if (id <= 0)
            throw new BusinessRuleException("El identificador del pedido debe ser mayor que cero.");

        var pedido = await _repository.GetByIdAsync(id, asNoTracking: true)
            ?? throw new ResourceNotFoundException($"Pedido de venta con Id {id} no encontrado.");

        return MapToDto(pedido);
    }

    public async Task<PagedResult<PedidoVentaDto>> GetPagedAsync(PedidoVentaFiltroDto request)
    {
        ValidarFiltro(request);
        var (items, total) = await _repository.GetPagedAsync(request);
        return new PagedResult<PedidoVentaDto>
        {
            Items = items.Select(MapToDto).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }

    public async Task<PedidoVentaDto> CrearDesdeCotizacionAsync(CreatePedidoVentaDto dto, string idempotencyKey)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.CotizacionId <= 0)
            throw new BusinessRuleException("CotizacionId debe ser mayor que cero.");

        var key = NormalizarIdempotencyKey(idempotencyKey);
        var fingerprint = CalcularFingerprint(dto.CotizacionId, dto.Observaciones);
        var (usuarioId, _) = RequerirUsuario();
        var pedidoId = 0;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var porKey = await _repository.GetByIdempotencyKeyForUpdateAsync(key);
            if (porKey is not null)
            {
                ValidarReplay(porKey, fingerprint);
                pedidoId = porKey.Id;
                return;
            }

            var cotizacion = await _cotizacionRepository.GetByIdForUpdateAsync(dto.CotizacionId)
                ?? throw new ResourceNotFoundException($"Cotización con Id {dto.CotizacionId} no encontrada.");

            var porCotizacion = await _repository.GetByCotizacionIdForUpdateAsync(dto.CotizacionId);
            if (porCotizacion is not null)
            {
                if (!string.Equals(porCotizacion.IdempotencyKey, key, StringComparison.Ordinal) ||
                    !string.Equals(porCotizacion.IdempotencyFingerprint, fingerprint, StringComparison.Ordinal))
                    throw new BusinessRuleException("La cotización ya originó un pedido con otra clave o payload idempotente.");

                pedidoId = porCotizacion.Id;
                return;
            }

            var pedido = PedidoVenta.CrearDesdeCotizacion(cotizacion);
            pedido.EstablecerIdempotencia(key, fingerprint);
            if (!string.IsNullOrWhiteSpace(dto.Observaciones))
                pedido.ActualizarObservaciones(dto.Observaciones);

            cotizacion.Convertir(usuarioId, DateTime.UtcNow);

            await _repository.AddAsync(pedido);
            _cotizacionRepository.Update(cotizacion);
            await _repository.SaveChangesAsync();

            await _auditoriaService.RegistrarEstrictoAsync(
                ModuloSistema.Ventas,
                AccionPermiso.Crear,
                "Pedido de venta creado desde cotización.",
                pedido.Id,
                nameof(PedidoVenta),
                valoresNuevos: new { pedido.CotizacionId, pedido.ClienteId, pedido.Estado, pedido.Total });

            await _auditoriaService.RegistrarEstrictoAsync(
                ModuloSistema.Ventas,
                AccionPermiso.Aplicar,
                "Cotización convertida al originar pedido de venta.",
                cotizacion.Id,
                nameof(Cotizacion),
                valoresNuevos: new { cotizacion.Estado, PedidoVentaId = pedido.Id });

            pedidoId = pedido.Id;
        });

        return await GetByIdAsync(pedidoId);
    }

    public async Task<PedidoVentaDto> ActualizarAsync(UpdatePedidoVentaDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Id <= 0)
            throw new BusinessRuleException("El identificador del pedido debe ser mayor que cero.");

        RequerirUsuario();
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var pedido = await RequerirForUpdateAsync(dto.Id);
            var anterior = pedido.Observaciones;
            pedido.ActualizarObservaciones(dto.Observaciones);
            _repository.Update(pedido);
            await _repository.SaveChangesAsync();

            await _auditoriaService.RegistrarEstrictoAsync(
                ModuloSistema.Ventas,
                AccionPermiso.Editar,
                "Pedido de venta actualizado.",
                pedido.Id,
                nameof(PedidoVenta),
                valoresAnteriores: new { Observaciones = anterior },
                valoresNuevos: new { pedido.Observaciones });
        });

        return await GetByIdAsync(dto.Id);
    }

    public Task<PedidoVentaDto> ConfirmarAsync(int id) =>
        CambiarEstadoAsync(
            id,
            AccionPermiso.Confirmar,
            "Pedido de venta confirmado.",
            static (pedido, usuarioId, nombre) => pedido.Confirmar(usuarioId, nombre, DateTime.UtcNow));

    public async Task<PedidoVentaDto> AnularAsync(int id, string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new BusinessRuleException("El motivo de anulación es obligatorio.");

        return await CambiarEstadoAsync(
            id,
            AccionPermiso.Anular,
            "Pedido de venta anulado.",
            (pedido, usuarioId, nombre) => pedido.Anular(usuarioId, nombre, motivo, DateTime.UtcNow),
            motivo.Trim());
    }

    private async Task<PedidoVentaDto> CambiarEstadoAsync(
        int id,
        AccionPermiso accion,
        string descripcion,
        Action<PedidoVenta, int, string> mutacion,
        string? motivo = null)
    {
        if (id <= 0)
            throw new BusinessRuleException("El identificador del pedido debe ser mayor que cero.");

        var (usuarioId, nombre) = RequerirUsuario();
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var pedido = await RequerirForUpdateAsync(id);
            var estadoAnterior = pedido.Estado;
            mutacion(pedido, usuarioId, nombre);
            _repository.Update(pedido);
            await _repository.SaveChangesAsync();

            await _auditoriaService.RegistrarEstrictoAsync(
                ModuloSistema.Ventas,
                accion,
                descripcion,
                pedido.Id,
                nameof(PedidoVenta),
                valoresAnteriores: new { Estado = estadoAnterior },
                valoresNuevos: new { pedido.Estado },
                motivo: motivo);
        });

        return await GetByIdAsync(id);
    }

    private async Task<PedidoVenta> RequerirForUpdateAsync(int id) =>
        await _repository.GetByIdForUpdateAsync(id)
        ?? throw new ResourceNotFoundException($"Pedido de venta con Id {id} no encontrado.");

    private (int UsuarioId, string Nombre) RequerirUsuario()
    {
        if (_currentUserService.UsuarioId is not > 0)
            throw new ForbiddenAccessException("La operación requiere un usuario autenticado.");

        var nombre = _currentUserService.NombreCompleto?.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            nombre = _currentUserService.NombreUsuario?.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ForbiddenAccessException("No se pudo resolver la identidad del usuario autenticado.");

        return (_currentUserService.UsuarioId.Value, nombre);
    }

    private static string NormalizarIdempotencyKey(string key)
    {
        var normalizada = key?.Trim() ?? string.Empty;
        if (normalizada.Length is < 1 or > 128)
            throw new BusinessRuleException("Idempotency-Key es obligatoria y debe tener entre 1 y 128 caracteres.");
        if (normalizada.Any(char.IsControl))
            throw new BusinessRuleException("Idempotency-Key contiene caracteres no permitidos.");
        return normalizada;
    }

    private static string CalcularFingerprint(int cotizacionId, string? observaciones)
    {
        var payload = $"{cotizacionId}|{NormalizarOpcional(observaciones) ?? string.Empty}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    private static void ValidarReplay(PedidoVenta existente, string fingerprint)
    {
        if (!string.Equals(existente.IdempotencyFingerprint, fingerprint, StringComparison.Ordinal))
            throw new BusinessRuleException("Idempotency-Key ya fue utilizada con un payload diferente.");
    }

    private static void ValidarFiltro(PedidoVentaFiltroDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.CotizacionId is <= 0)
            throw new BusinessRuleException("CotizacionId debe ser mayor que cero.");
        if (request.ClienteId is <= 0)
            throw new BusinessRuleException("ClienteId debe ser mayor que cero.");
        if (request.Estado.HasValue && !Enum.IsDefined(request.Estado.Value))
            throw new BusinessRuleException("El estado de pedido no es válido.");
        ValidarFechaUtc(request.FechaDesdeUtc, nameof(request.FechaDesdeUtc));
        ValidarFechaUtc(request.FechaHastaUtc, nameof(request.FechaHastaUtc));
        if (request.FechaDesdeUtc.HasValue && request.FechaHastaUtc.HasValue && request.FechaDesdeUtc > request.FechaHastaUtc)
            throw new BusinessRuleException("FechaDesdeUtc no puede ser posterior a FechaHastaUtc.");
        if (!string.IsNullOrWhiteSpace(request.SortDirection) &&
            !string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleException("SortDirection debe ser 'asc' o 'desc'.");
    }

    private static void ValidarFechaUtc(DateTime? fecha, string nombre)
    {
        if (fecha.HasValue && fecha.Value.Kind != DateTimeKind.Utc)
            throw new BusinessRuleException($"{nombre} debe estar expresada en UTC.");
    }

    private static string? NormalizarOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static PedidoVentaDto MapToDto(PedidoVenta pedido) => new()
    {
        Id = pedido.Id,
        CotizacionId = pedido.CotizacionId,
        ClienteId = pedido.ClienteId,
        ClienteNombreSnapshot = pedido.ClienteNombreSnapshot,
        ClienteDocumentoSnapshot = pedido.ClienteDocumentoSnapshot,
        Observaciones = pedido.Observaciones,
        Estado = pedido.Estado,
        Total = pedido.Total,
        FechaConfirmacionUtc = pedido.FechaConfirmacion,
        ConfirmadoPorUsuarioId = pedido.ConfirmadoPorUsuarioId,
        FechaAnulacionUtc = pedido.FechaAnulacion,
        AnuladoPorUsuarioId = pedido.AnuladoPorUsuarioId,
        MotivoAnulacion = pedido.MotivoAnulacion,
        Detalles = pedido.Detalles.Select(x => new PedidoVentaDetalleDto
        {
            Id = x.Id,
            ProductoId = x.ProductoId,
            ProductoVarianteId = x.ProductoVarianteId,
            ProductoSkuSnapshot = x.ProductoSkuSnapshot,
            ProductoNombreSnapshot = x.ProductoNombreSnapshot,
            ProductoMarcaSnapshot = x.ProductoMarcaSnapshot,
            ProductoModeloSnapshot = x.ProductoModeloSnapshot,
            ProductoColorSnapshot = x.ProductoColorSnapshot,
            ProductoTallaSnapshot = x.ProductoTallaSnapshot,
            Cantidad = x.Cantidad,
            PrecioUnitario = x.PrecioUnitario,
            Total = x.Total
        }).ToList()
    };
}

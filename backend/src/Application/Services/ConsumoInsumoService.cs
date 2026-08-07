using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Services;

public class ConsumoInsumoService : IConsumoInsumoService
{
    private readonly IConsumoInsumoRepository _repository;
    private readonly IProductoRepository _productoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditoriaService _auditoria;

    public ConsumoInsumoService(
        IConsumoInsumoRepository repository,
        IProductoRepository productoRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IAuditoriaService auditoria)
    {
        _repository = repository;
        _productoRepository = productoRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _auditoria = auditoria;
    }

    public async Task<List<ConsumoInsumoDto>> GetAllAsync() =>
        (await _repository.GetAllAsync()).Select(ToDto).ToList();

    public async Task<ConsumoInsumoDto?> GetByIdAsync(int id)
    {
        var consumo = await _repository.GetByIdAsync(id);
        return consumo is null ? null : ToDto(consumo);
    }

    public async Task<ConsumoInsumoDto> CreateAsync(CreateConsumoInsumoDto dto)
    {
        ValidarCabecera(dto.AreaDestino, dto.Motivo, dto.Observaciones, dto.Detalles);
        int creadoId = 0;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var ahora = DateTime.UtcNow;
            var consumo = new ConsumoInsumo
            {
                NumeroConsumo = $"TMP-{Guid.NewGuid():N}"[..20],
                FechaConsumo = dto.FechaConsumo ?? ahora,
                Estado = EstadoConsumoInsumo.Borrador,
                AreaDestino = dto.AreaDestino.Trim(),
                Motivo = dto.Motivo.Trim(),
                Observaciones = NormalizarOpcional(dto.Observaciones),
                CreadoPorUsuarioId = _currentUser.UsuarioId,
                CreadoPorNombreUsuario = _currentUser.NombreUsuario,
                ActualizadoPorUsuarioId = _currentUser.UsuarioId,
                ActualizadoPorNombreUsuario = _currentUser.NombreUsuario,
                FechaCreacion = ahora,
                FechaActualizacion = ahora
            };

            await ReemplazarDetallesAsync(consumo, dto.Detalles);
            await _repository.AddAsync(consumo);
            await _repository.SaveChangesAsync();

            consumo.NumeroConsumo = $"CI-{consumo.Id:D6}";
            _repository.Update(consumo);
            await _repository.SaveChangesAsync();
            creadoId = consumo.Id;
        });

        var creado = await _repository.GetByIdAsync(creadoId)
            ?? throw new InvalidOperationException("No se pudo recuperar el consumo recién creado.");

        await _auditoria.RegistrarAsync(
            ModuloSistema.InsumosAdministrativos,
            AccionPermiso.Crear,
            $"Consumo administrativo creado como borrador: {creado.NumeroConsumo}",
            creado.Id,
            entidad: nameof(ConsumoInsumo));

        return ToDto(creado);
    }

    public async Task<ConsumoInsumoDto?> UpdateAsync(int id, UpdateConsumoInsumoDto dto)
    {
        ValidarCabecera(dto.AreaDestino, dto.Motivo, dto.Observaciones, dto.Detalles);
        var encontrado = false;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var consumo = await _repository.GetByIdForUpdateAsync(id);
            if (consumo is null) return;
            encontrado = true;

            if (consumo.Estado != EstadoConsumoInsumo.Borrador)
                throw new BusinessRuleException("Solo los consumos en estado Borrador pueden editarse.");

            consumo.FechaConsumo = dto.FechaConsumo ?? consumo.FechaConsumo;
            consumo.AreaDestino = dto.AreaDestino.Trim();
            consumo.Motivo = dto.Motivo.Trim();
            consumo.Observaciones = NormalizarOpcional(dto.Observaciones);
            consumo.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
            consumo.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
            consumo.FechaActualizacion = DateTime.UtcNow;

            consumo.Detalles.Clear();
            await ReemplazarDetallesAsync(consumo, dto.Detalles);
            _repository.Update(consumo);
            await _repository.SaveChangesAsync();
        });

        if (!encontrado) return null;

        var actualizado = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("No se pudo recuperar el consumo actualizado.");

        await _auditoria.RegistrarAsync(
            ModuloSistema.InsumosAdministrativos,
            AccionPermiso.Editar,
            $"Consumo administrativo actualizado: {actualizado.NumeroConsumo}",
            actualizado.Id,
            entidad: nameof(ConsumoInsumo));

        return ToDto(actualizado);
    }

    public async Task<bool> DeleteBorradorAsync(int id)
    {
        var eliminado = false;
        string? numero = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var consumo = await _repository.GetByIdForUpdateAsync(id);
            if (consumo is null) return;

            if (consumo.Estado != EstadoConsumoInsumo.Borrador)
                throw new BusinessRuleException("Solo los consumos en estado Borrador pueden eliminarse.");

            numero = consumo.NumeroConsumo;
            consumo.Eliminado = true;
            consumo.FechaEliminacion = DateTime.UtcNow;
            consumo.EliminadoPorUsuarioId = _currentUser.UsuarioId;
            consumo.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
            consumo.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
            consumo.FechaActualizacion = DateTime.UtcNow;
            _repository.Update(consumo);
            await _repository.SaveChangesAsync();
            eliminado = true;
        });

        if (eliminado)
        {
            await _auditoria.RegistrarAsync(
                ModuloSistema.InsumosAdministrativos,
                AccionPermiso.EliminarLogico,
                $"Consumo administrativo eliminado lógicamente: {numero}",
                id,
                entidad: nameof(ConsumoInsumo));
        }

        return eliminado;
    }

    private async Task ReemplazarDetallesAsync(
        ConsumoInsumo consumo,
        IEnumerable<ConsumoInsumoDetalleInputDto> detalles)
    {
        var consolidados = detalles
            .GroupBy(d => (d.ProductoId, d.ProductoVarianteId))
            .Select(g => new ConsumoInsumoDetalleInputDto
            {
                ProductoId = g.Key.ProductoId,
                ProductoVarianteId = g.Key.ProductoVarianteId,
                Cantidad = g.Sum(x => x.Cantidad)
            })
            .OrderBy(d => d.ProductoId)
            .ThenBy(d => d.ProductoVarianteId)
            .ToList();

        var productos = new Dictionary<int, Producto>();
        foreach (var detalle in consolidados)
        {
            if (!productos.TryGetValue(detalle.ProductoId, out var producto))
            {
                producto = await _productoRepository.GetByIdAsync(detalle.ProductoId)
                    ?? throw new BusinessRuleException($"El producto ID '{detalle.ProductoId}' no existe.");
                productos[detalle.ProductoId] = producto;
            }

            if (producto.TipoInventario != TipoInventario.InsumoAdministrativo)
                throw new BusinessRuleException($"El producto '{producto.Nombre}' no está clasificado como insumo administrativo.");
            if (!producto.Activo)
                throw new BusinessRuleException($"El insumo '{producto.Nombre}' está inactivo.");

            var variantes = (producto.Variantes ?? Array.Empty<ProductoVariante>())
                .Where(v => !v.Eliminado)
                .ToList();
            ProductoVariante? variante = null;

            if (variantes.Count > 0)
            {
                if (!detalle.ProductoVarianteId.HasValue)
                    throw new BusinessRuleException($"El insumo '{producto.Nombre}' requiere seleccionar una variante.");

                variante = variantes.FirstOrDefault(v => v.Id == detalle.ProductoVarianteId.Value)
                    ?? throw new BusinessRuleException("La variante indicada no pertenece al insumo seleccionado.");
                if (!variante.Activo)
                    throw new BusinessRuleException($"La variante '{variante.Sku}' está inactiva.");
            }
            else if (detalle.ProductoVarianteId.HasValue)
            {
                throw new BusinessRuleException($"El insumo '{producto.Nombre}' no posee la variante indicada.");
            }

            var costo = variante?.Costo ?? producto.Costo;
            consumo.Detalles.Add(new ConsumoInsumoDetalle
            {
                ProductoId = producto.Id,
                ProductoVarianteId = variante?.Id,
                Cantidad = detalle.Cantidad,
                CostoUnitarioSnapshot = costo,
                CostoTotalSnapshot = Math.Round(costo * detalle.Cantidad, 2, MidpointRounding.AwayFromZero),
                NombreSnapshot = producto.Nombre,
                SkuSnapshot = variante?.Sku,
                ColorSnapshot = variante?.Color?.Nombre ?? producto.Color?.Nombre,
                FechaCreacion = DateTime.UtcNow,
                FechaActualizacion = DateTime.UtcNow
            });
        }
    }

    private static void ValidarCabecera(
        string areaDestino,
        string motivo,
        string? observaciones,
        IReadOnlyCollection<ConsumoInsumoDetalleInputDto> detalles)
    {
        if (string.IsNullOrWhiteSpace(areaDestino) || areaDestino.Trim().Length > 150)
            throw new BusinessRuleException("El área destino es obligatoria y no puede exceder 150 caracteres.");
        if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length > 500)
            throw new BusinessRuleException("El motivo es obligatorio y no puede exceder 500 caracteres.");
        if (observaciones?.Trim().Length > 1000)
            throw new BusinessRuleException("Las observaciones no pueden exceder 1000 caracteres.");
        if (detalles is null || detalles.Count == 0)
            throw new BusinessRuleException("El consumo debe contener al menos un insumo.");
        if (detalles.Count > 200)
            throw new BusinessRuleException("El consumo no puede contener más de 200 líneas.");
        if (detalles.Any(d => d.ProductoId <= 0 || d.Cantidad <= 0))
            throw new BusinessRuleException("Cada línea debe indicar un producto válido y una cantidad mayor a cero.");
    }

    private static string? NormalizarOpcional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ConsumoInsumoDto ToDto(ConsumoInsumo consumo) => new()
    {
        Id = consumo.Id,
        NumeroConsumo = consumo.NumeroConsumo,
        FechaConsumo = consumo.FechaConsumo,
        Estado = consumo.Estado.ToString(),
        AreaDestino = consumo.AreaDestino,
        Motivo = consumo.Motivo,
        Observaciones = consumo.Observaciones,
        FechaConfirmacion = consumo.FechaConfirmacion,
        ConfirmadoPorNombreUsuario = consumo.ConfirmadoPorNombreUsuario,
        FechaAnulacion = consumo.FechaAnulacion,
        AnuladoPorNombreUsuario = consumo.AnuladoPorNombreUsuario,
        MotivoAnulacion = consumo.MotivoAnulacion,
        CostoTotalSnapshot = consumo.Detalles.Sum(d => d.CostoTotalSnapshot),
        Detalles = consumo.Detalles
            .OrderBy(d => d.Id)
            .Select(d => new ConsumoInsumoDetalleDto
            {
                Id = d.Id,
                ProductoId = d.ProductoId,
                ProductoVarianteId = d.ProductoVarianteId,
                Cantidad = d.Cantidad,
                CostoUnitarioSnapshot = d.CostoUnitarioSnapshot,
                CostoTotalSnapshot = d.CostoTotalSnapshot,
                NombreSnapshot = d.NombreSnapshot,
                SkuSnapshot = d.SkuSnapshot,
                ColorSnapshot = d.ColorSnapshot
            })
            .ToList()
    };
}

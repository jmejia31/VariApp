using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Services;

public sealed class SucursalService : ISucursalService
{
    private const int TamanoPaginaMaximo = 100;

    private readonly ISucursalRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditoriaService _auditoria;

    public SucursalService(
        ISucursalRepository repository,
        ICurrentUserService currentUser,
        IAuditoriaService auditoria)
    {
        _repository = repository;
        _currentUser = currentUser;
        _auditoria = auditoria;
    }

    public async Task<SucursalPaginaDto> BuscarAsync(SucursalFiltroDto filtro)
    {
        ValidarEmpresaId(filtro.EmpresaId);

        var pagina = Math.Max(1, filtro.Pagina);
        var tamanoPagina = Math.Clamp(filtro.TamanoPagina, 1, TamanoPaginaMaximo);
        var (items, total) = await _repository.BuscarAsync(
            Limpiar(filtro.Buscar),
            filtro.Activa,
            filtro.EmpresaId,
            pagina,
            tamanoPagina);

        return new SucursalPaginaDto
        {
            Items = items.Select(ToDto).ToList(),
            Pagina = pagina,
            TamanoPagina = tamanoPagina,
            Total = total,
            TotalPaginas = total == 0 ? 0 : (int)Math.Ceiling(total / (double)tamanoPagina)
        };
    }

    public async Task<List<SucursalDto>> GetActivasAsync(int? empresaId = null)
    {
        ValidarEmpresaId(empresaId);
        var sucursales = await _repository.GetActivasAsync(empresaId);
        return sucursales.Select(ToDto).ToList();
    }

    public async Task<SucursalDto?> GetByIdAsync(int id)
    {
        var sucursal = await _repository.GetByIdAsync(id);
        return sucursal is null ? null : ToDto(sucursal);
    }

    public async Task<SucursalDto> CreateAsync(CreateSucursalDto dto)
    {
        ValidarEmpresaId(dto.EmpresaId);
        var codigo = NormalizarCodigo(dto.Codigo);
        var nombre = NormalizarRequerido(dto.Nombre, "El nombre de la sucursal es obligatorio.");
        var zonaHoraria = ValidarZonaHoraria(dto.ZonaHoraria);

        if (await _repository.ExisteCodigoAsync(codigo))
            throw new BusinessRuleException($"Ya existe una sucursal activa con el código '{codigo}'.");

        var sucursal = new Sucursal
        {
            EmpresaId = dto.EmpresaId,
            Codigo = codigo,
            Nombre = nombre,
            Direccion = Limpiar(dto.Direccion),
            Telefono = Limpiar(dto.Telefono),
            Correo = Limpiar(dto.Correo),
            ZonaHoraria = zonaHoraria,
            Activa = true,
            Eliminado = false,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };

        await _repository.AddAsync(sucursal);
        await _repository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(
            ModuloSistema.Sucursales,
            AccionPermiso.Crear,
            $"Sucursal creada: {sucursal.Codigo} - {sucursal.Nombre}",
            sucursal.Id,
            entidad: "Sucursal");

        return ToDto(sucursal);
    }

    public async Task<SucursalDto?> UpdateAsync(int id, UpdateSucursalDto dto)
    {
        var sucursal = await _repository.GetByIdAsync(id);
        if (sucursal is null) return null;

        ValidarEmpresaId(dto.EmpresaId);
        var codigo = NormalizarCodigo(dto.Codigo);
        var nombre = NormalizarRequerido(dto.Nombre, "El nombre de la sucursal es obligatorio.");
        var zonaHoraria = ValidarZonaHoraria(dto.ZonaHoraria);

        if (await _repository.ExisteCodigoAsync(codigo, id))
            throw new BusinessRuleException($"Ya existe otra sucursal activa con el código '{codigo}'.");

        sucursal.EmpresaId = dto.EmpresaId;
        sucursal.Codigo = codigo;
        sucursal.Nombre = nombre;
        sucursal.Direccion = Limpiar(dto.Direccion);
        sucursal.Telefono = Limpiar(dto.Telefono);
        sucursal.Correo = Limpiar(dto.Correo);
        sucursal.ZonaHoraria = zonaHoraria;
        // El estado se modifica exclusivamente mediante Activar/Desactivar.
        sucursal.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        sucursal.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        sucursal.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(sucursal);
        await _repository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(
            ModuloSistema.Sucursales,
            AccionPermiso.Editar,
            $"Sucursal actualizada: {sucursal.Codigo} - {sucursal.Nombre}",
            sucursal.Id,
            entidad: "Sucursal");

        return ToDto(sucursal);
    }

    public async Task<SucursalDto?> CambiarEstadoAsync(int id, bool activa)
    {
        var sucursal = await _repository.GetByIdAsync(id);
        if (sucursal is null) return null;

        // PATCH de estado es idempotente: repetir la misma transición no crea
        // escrituras ni auditoría duplicada.
        if (sucursal.Activa == activa)
            return ToDto(sucursal);

        sucursal.Activa = activa;
        sucursal.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        sucursal.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        sucursal.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(sucursal);
        await _repository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(
            ModuloSistema.Sucursales,
            activa ? AccionPermiso.Activar : AccionPermiso.Desactivar,
            $"Sucursal {(activa ? "activada" : "desactivada")}: {sucursal.Codigo} - {sucursal.Nombre}",
            sucursal.Id,
            entidad: "Sucursal");

        return ToDto(sucursal);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var sucursal = await _repository.GetByIdAsync(id);
        if (sucursal is null) return false;

        sucursal.Activa = false;
        sucursal.Eliminado = true;
        sucursal.FechaEliminacion = DateTime.UtcNow;
        sucursal.EliminadoPorUsuarioId = _currentUser.UsuarioId;
        sucursal.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        sucursal.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        sucursal.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(sucursal);
        var eliminado = await _repository.SaveChangesAsync();
        if (eliminado)
        {
            await _auditoria.RegistrarAsync(
                ModuloSistema.Sucursales,
                AccionPermiso.EliminarLogico,
                $"Sucursal eliminada lógicamente: {sucursal.Codigo} - {sucursal.Nombre}",
                sucursal.Id,
                entidad: "Sucursal");
        }

        return eliminado;
    }

    private static void ValidarEmpresaId(int? empresaId)
    {
        if (empresaId.HasValue && empresaId.Value <= 0)
            throw new BusinessRuleException("EmpresaId debe ser mayor que cero cuando se especifica.");
    }

    private static string NormalizarCodigo(string? valor)
    {
        var codigo = NormalizarRequerido(valor, "El código de la sucursal es obligatorio.");
        return codigo.ToUpperInvariant();
    }

    private static string NormalizarRequerido(string? valor, string mensaje)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new BusinessRuleException(mensaje);
        return valor.Trim();
    }

    private static string ValidarZonaHoraria(string? valor)
    {
        var zona = NormalizarRequerido(valor, "La zona horaria es obligatoria.");
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(zona);
            return zona;
        }
        catch (TimeZoneNotFoundException)
        {
            throw new BusinessRuleException($"La zona horaria '{zona}' no es válida.");
        }
        catch (InvalidTimeZoneException)
        {
            throw new BusinessRuleException($"La zona horaria '{zona}' no es válida.");
        }
    }

    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static SucursalDto ToDto(Sucursal s) => new()
    {
        Id = s.Id,
        EmpresaId = s.EmpresaId,
        Codigo = s.Codigo,
        Nombre = s.Nombre,
        Direccion = s.Direccion,
        Telefono = s.Telefono,
        Correo = s.Correo,
        ZonaHoraria = s.ZonaHoraria,
        Activa = s.Activa,
        CreadoPorNombreUsuario = s.CreadoPorNombreUsuario,
        ActualizadoPorNombreUsuario = s.ActualizadoPorNombreUsuario,
        FechaCreacion = s.FechaCreacion,
        FechaActualizacion = s.FechaActualizacion
    };
}

using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace InventoryApp.Application.Services;

public class EmpresaConfiguracionService : IEmpresaConfiguracionService
{
    private static readonly HashSet<string> ExtensionesPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private static readonly HashSet<string> MimePermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    private const long MaxLogoBytes = 5 * 1024 * 1024;

    private readonly IEmpresaConfiguracionRepository _repository;
    private readonly IImageStorageService _imageStorage;
    private readonly IAuditoriaService _auditoria;

    public EmpresaConfiguracionService(
        IEmpresaConfiguracionRepository repository,
        IImageStorageService imageStorage,
        IAuditoriaService auditoria)
    {
        _repository = repository;
        _imageStorage = imageStorage;
        _auditoria = auditoria;
    }

    public async Task<EmpresaConfiguracionDto> GetActivaAsync()
    {
        var config = await GetActivaEntidadAsync();
        return ToDto(config);
    }

    public async Task<EmpresaConfiguracion> GetActivaEntidadAsync()
    {
        return await GetOrCreateAsync();
    }

    public async Task<EmpresaConfiguracionDto> UpdateAsync(UpdateEmpresaConfiguracionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NombreComercial))
            throw new BusinessRuleException("El nombre comercial es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.NombreVisibleSistema))
            throw new BusinessRuleException("El nombre visible del sistema es obligatorio.");

        var config = await GetOrCreateAsync();
        var anterior = ToDto(config);

        config.NombreComercial = dto.NombreComercial.Trim();
        config.RazonSocial = Limpiar(dto.RazonSocial);
        config.Eslogan = dto.Eslogan?.Trim() ?? string.Empty;
        config.RTN = Limpiar(dto.RTN);
        config.Telefono = Limpiar(dto.Telefono);
        config.Correo = Limpiar(dto.Correo);
        config.Direccion = Limpiar(dto.Direccion);
        config.SitioWeb = Limpiar(dto.SitioWeb);
        config.Facebook = Limpiar(dto.Facebook);
        config.Instagram = Limpiar(dto.Instagram);
        config.WhatsApp = Limpiar(dto.WhatsApp);
        config.NombreVisibleSistema = dto.NombreVisibleSistema.Trim();
        config.DescripcionSistema = string.IsNullOrWhiteSpace(dto.DescripcionSistema)
            ? "Administrativo"
            : dto.DescripcionSistema.Trim();
        config.MensajeLogin = string.IsNullOrWhiteSpace(dto.MensajeLogin)
            ? $"Inicia sesión para administrar {config.NombreVisibleSistema}"
            : dto.MensajeLogin.Trim();
        config.Copyright = string.IsNullOrWhiteSpace(dto.Copyright)
            ? $"© {DateTime.UtcNow.Year} {config.NombreVisibleSistema}. Todos los derechos reservados."
            : dto.Copyright.Trim();
        config.MostrarCopyright = dto.MostrarCopyright;
        config.UsarAnioAutomaticoCopyright = dto.UsarAnioAutomaticoCopyright;
        config.EncabezadoActivo = dto.EncabezadoActivo;
        config.EncabezadoTexto = Limpiar(dto.EncabezadoTexto);
        config.PiePaginaActivo = dto.PiePaginaActivo;
        config.PiePaginaTexto = Limpiar(dto.PiePaginaTexto);
        config.Moneda = string.IsNullOrWhiteSpace(dto.Moneda) ? "HNL" : dto.Moneda.Trim().ToUpperInvariant();
        config.ZonaHoraria = string.IsNullOrWhiteSpace(dto.ZonaHoraria) ? "America/Tegucigalpa" : dto.ZonaHoraria.Trim();
        config.FormatoFecha = string.IsNullOrWhiteSpace(dto.FormatoFecha) ? "dd/MM/yyyy" : dto.FormatoFecha.Trim();
        config.InformacionFiscal = Limpiar(dto.InformacionFiscal);
        config.TextoLegal = Limpiar(dto.TextoLegal);
        config.TextoFactura = Limpiar(dto.TextoFactura);
        config.TextoReportes = Limpiar(dto.TextoReportes);
        config.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(config);
        await _repository.SaveChangesAsync();

        var nueva = ToDto(config);
        await _auditoria.RegistrarAsync(
            ModuloSistema.Configuracion,
            AccionPermiso.Editar,
            "Configuracion empresarial actualizada.",
            config.Id,
            entidad: "EmpresaConfiguracion",
            valoresAnteriores: anterior,
            valoresNuevos: nueva);

        return nueva;
    }

    public async Task<EmpresaConfiguracionDto> UpdateLogoAsync(IFormFile logo)
    {
        ValidarLogo(logo);

        var config = await GetOrCreateAsync();
        var anterior = ToDto(config);
        var publicIdAnterior = config.LogoPublicId;

        var (url, publicId) = await _imageStorage.UploadAsync(logo);
        config.LogoUrl = url;
        config.LogoPublicId = publicId;
        config.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(config);
        await _repository.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(publicIdAnterior))
            await _imageStorage.DeleteAsync(publicIdAnterior);

        var nueva = ToDto(config);
        await _auditoria.RegistrarAsync(
            ModuloSistema.Configuracion,
            AccionPermiso.Editar,
            "Logo empresarial actualizado.",
            config.Id,
            entidad: "EmpresaConfiguracion",
            valoresAnteriores: anterior,
            valoresNuevos: nueva);

        return nueva;
    }

    public async Task<EmpresaConfiguracionDto> RestaurarLogoAsync()
    {
        var config = await GetOrCreateAsync();
        var anterior = ToDto(config);
        var publicIdAnterior = config.LogoPublicId;

        config.LogoUrl = null;
        config.LogoPublicId = null;
        config.FechaActualizacion = DateTime.UtcNow;
        _repository.Update(config);
        await _repository.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(publicIdAnterior))
            await _imageStorage.DeleteAsync(publicIdAnterior);

        var nueva = ToDto(config);
        await _auditoria.RegistrarAsync(
            ModuloSistema.Configuracion,
            AccionPermiso.Editar,
            "Logo empresarial restaurado al valor predeterminado.",
            config.Id,
            entidad: "EmpresaConfiguracion",
            valoresAnteriores: anterior,
            valoresNuevos: nueva);

        return nueva;
    }

    private async Task<EmpresaConfiguracion> GetOrCreateAsync()
    {
        var config = await _repository.GetActivaAsync();
        if (config is not null) return config;

        config = new EmpresaConfiguracion { Activa = true };
        await _repository.AddAsync(config);
        await _repository.SaveChangesAsync();
        return config;
    }

    private static void ValidarLogo(IFormFile logo)
    {
        if (logo.Length <= 0)
            throw new BusinessRuleException("El archivo del logo está vacío.");
        if (logo.Length > MaxLogoBytes)
            throw new BusinessRuleException("El logo no puede exceder 5 MB.");

        var extension = Path.GetExtension(logo.FileName);
        if (!ExtensionesPermitidas.Contains(extension))
            throw new BusinessRuleException("Formato de logo no permitido. Usa JPG, PNG o WEBP.");
        if (!MimePermitidos.Contains(logo.ContentType))
            throw new BusinessRuleException("Tipo MIME de logo no permitido.");
    }

    private static string? Limpiar(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static EmpresaConfiguracionDto ToDto(EmpresaConfiguracion c) => new()
    {
        Id = c.Id,
        NombreComercial = c.NombreComercial,
        RazonSocial = c.RazonSocial,
        Eslogan = c.Eslogan,
        RTN = c.RTN,
        Telefono = c.Telefono,
        Correo = c.Correo,
        Direccion = c.Direccion,
        SitioWeb = c.SitioWeb,
        Facebook = c.Facebook,
        Instagram = c.Instagram,
        WhatsApp = c.WhatsApp,
        LogoUrl = c.LogoUrl,
        NombreVisibleSistema = c.NombreVisibleSistema,
        DescripcionSistema = c.DescripcionSistema,
        MensajeLogin = c.MensajeLogin,
        Copyright = c.UsarAnioAutomaticoCopyright
            ? c.Copyright.Replace("2026", DateTime.UtcNow.Year.ToString())
            : c.Copyright,
        MostrarCopyright = c.MostrarCopyright,
        UsarAnioAutomaticoCopyright = c.UsarAnioAutomaticoCopyright,
        EncabezadoActivo = c.EncabezadoActivo,
        EncabezadoTexto = c.EncabezadoTexto,
        PiePaginaActivo = c.PiePaginaActivo,
        PiePaginaTexto = c.PiePaginaTexto,
        Moneda = c.Moneda,
        ZonaHoraria = c.ZonaHoraria,
        FormatoFecha = c.FormatoFecha,
        InformacionFiscal = c.InformacionFiscal,
        TextoLegal = c.TextoLegal,
        TextoFactura = c.TextoFactura,
        TextoReportes = c.TextoReportes
    };
}

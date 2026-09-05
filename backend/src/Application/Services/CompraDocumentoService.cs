using InventoryApp.Application.DTOs;
using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace InventoryApp.Application.Services;

public class CompraDocumentoService : ICompraDocumentoService
{
    private const int MaxDocumentosPorCompra = 10;
    private const long MaxBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> ContentTypesPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "application/pdf"
    };

    private static readonly HashSet<string> ExtensionesPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".pdf"
    };

    private readonly ICompraRepository _compraRepository;
    private readonly ICompraDocumentoRepository _documentoRepository;
    private readonly ICompraDocumentoStorageService _storage;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditoriaService _auditoria;

    public CompraDocumentoService(
        ICompraRepository compraRepository,
        ICompraDocumentoRepository documentoRepository,
        ICompraDocumentoStorageService storage,
        ICurrentUserService currentUser,
        IAuditoriaService auditoria)
    {
        _compraRepository = compraRepository;
        _documentoRepository = documentoRepository;
        _storage = storage;
        _currentUser = currentUser;
        _auditoria = auditoria;
    }

    public async Task<List<CompraDocumentoDto>> GetByCompraAsync(int compraId)
    {
        await ObtenerCompraAutorizadaAsync(compraId);
        var documentos = await _documentoRepository.GetByCompraIdAsync(compraId);
        return documentos.Select(ToDto).ToList();
    }

    public async Task<CompraDocumentoDto> UploadAsync(int compraId, IFormFile archivo)
    {
        var compra = await ObtenerCompraAutorizadaAsync(compraId);
        ValidarArchivo(archivo);

        var cantidad = await _documentoRepository.CountByCompraIdAsync(compraId);
        if (cantidad >= MaxDocumentosPorCompra)
            throw new BusinessRuleException($"Una compra puede tener como máximo {MaxDocumentosPorCompra} comprobantes adjuntos.");

        var almacenado = await _storage.UploadAsync(archivo);
        var documento = new CompraDocumento
        {
            CompraId = compraId,
            NombreOriginal = Path.GetFileName(archivo.FileName),
            ContentType = almacenado.ContentType,
            SizeBytes = almacenado.SizeBytes,
            Url = almacenado.Url,
            PublicId = almacenado.PublicId,
            ResourceType = almacenado.ResourceType,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };

        try
        {
            await _documentoRepository.AddAsync(documento);
            await _documentoRepository.SaveChangesAsync();
        }
        catch
        {
            await _storage.DeleteAsync(almacenado.PublicId, almacenado.ResourceType);
            throw;
        }

        await _auditoria.RegistrarAsync(
            ModuloSistema.Compras,
            AccionPermiso.Editar,
            $"Comprobante adjuntado a la compra {compra.NumeroCompra}: {documento.NombreOriginal}.",
            documento.Id,
            entidad: "CompraDocumento",
            valoresNuevos: new
            {
                compraId,
                documento.NombreOriginal,
                documento.ContentType,
                documento.SizeBytes
            });

        return ToDto(documento);
    }

    public async Task<(Stream Contenido, string ContentType, string NombreArchivo)?> DownloadAsync(
        int compraId,
        int documentoId)
    {
        var compra = await ObtenerCompraAutorizadaAsync(compraId);
        var documento = await _documentoRepository.GetByIdAsync(compraId, documentoId);
        if (documento is null) return null;

        var descarga = await _storage.DownloadAsync(documento.Url);
        if (descarga is null) return null;

        await _auditoria.RegistrarAsync(
            ModuloSistema.Compras,
            AccionPermiso.Exportar,
            $"Comprobante descargado de la compra {compra.NumeroCompra}: {documento.NombreOriginal}.",
            documento.Id,
            entidad: "CompraDocumento",
            valoresNuevos: new { compraId, documentoId, documento.NombreOriginal });

        return (descarga.Value.Contenido, descarga.Value.ContentType, documento.NombreOriginal);
    }

    public async Task<bool> DeleteAsync(int compraId, int documentoId)
    {
        var compra = await ObtenerCompraAutorizadaAsync(compraId);
        var documento = await _documentoRepository.GetByIdAsync(compraId, documentoId);
        if (documento is null) return false;

        await _storage.DeleteAsync(documento.PublicId, documento.ResourceType);

        documento.Eliminado = true;
        documento.FechaEliminacion = DateTime.UtcNow;
        documento.EliminadoPorUsuarioId = _currentUser.UsuarioId;
        _documentoRepository.Update(documento);
        await _documentoRepository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            ModuloSistema.Compras,
            AccionPermiso.Editar,
            $"Comprobante retirado de la compra {compra.NumeroCompra}: {documento.NombreOriginal}.",
            documento.Id,
            entidad: "CompraDocumento",
            valoresAnteriores: new
            {
                compraId,
                documento.NombreOriginal,
                documento.ContentType,
                documento.SizeBytes
            },
            valoresNuevos: new { documento.Eliminado, documento.FechaEliminacion });

        return true;
    }

    private async Task<Compra> ObtenerCompraAutorizadaAsync(int compraId)
    {
        return await _compraRepository.GetByIdAsync(compraId)
            ?? throw new BusinessRuleException("La compra no existe o no pertenece al usuario autenticado.");
    }

    private static void ValidarArchivo(IFormFile archivo)
    {
        if (archivo is null || archivo.Length <= 0)
            throw new BusinessRuleException("Selecciona un archivo válido.");
        if (archivo.Length > MaxBytes)
            throw new BusinessRuleException("El comprobante no puede superar 10 MB.");

        var extension = Path.GetExtension(archivo.FileName);
        if (!ExtensionesPermitidas.Contains(extension) || !ContentTypesPermitidos.Contains(archivo.ContentType))
            throw new BusinessRuleException("Solo se permiten archivos JPG, PNG, WebP o PDF.");
    }

    private static CompraDocumentoDto ToDto(CompraDocumento documento) => new()
    {
        Id = documento.Id,
        CompraId = documento.CompraId,
        NombreOriginal = documento.NombreOriginal,
        ContentType = documento.ContentType,
        SizeBytes = documento.SizeBytes,
        EsImagen = documento.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase),
        FechaCreacion = documento.FechaCreacion,
        CreadoPorNombreUsuario = documento.CreadoPorNombreUsuario
    };
}

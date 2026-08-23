using System.ComponentModel.DataAnnotations;

namespace InventoryApp.Application.DTOs;

public sealed class RechazarCotizacionDto
{
    [Required]
    [MaxLength(500)]
    public string Motivo { get; set; } = string.Empty;
}

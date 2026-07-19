namespace InventoryApp.Domain.Entities;

/// Tema visual global de la aplicación (sección 16 del prompt). Fila única
/// (Id=1) — configuración global administrada solo por usuarios
/// autorizados, no personalización por usuario individual (el prompt
/// permite que coexista una configuración por usuario, pero no existía
/// ninguna en este proyecto, así que no se inventa una).
public class TemaVisual
{
    public int Id { get; set; }

    public string ColorPrimario { get; set; } = "#4f46e5";
    public string ColorSecundario { get; set; } = "#4338ca";
    public string ColorAcento { get; set; } = "#6366f1";
    public string FondoPrincipal { get; set; } = "#f5f6fb";
    public string FondoTarjetas { get; set; } = "#ffffff";
    public string MenuLateral { get; set; } = "#1f2333";
    public string BarraSuperior { get; set; } = "#ffffff";
    public string Encabezados { get; set; } = "#1f2333";
    public string BotonesPrincipales { get; set; } = "#4f46e5";
    public string TextoPrincipal { get; set; } = "#1f2333";
    public string TextoSecundario { get; set; } = "#6b7280";
    public string ColorExito { get; set; } = "#10b981";
    public string ColorAdvertencia { get; set; } = "#f59e0b";
    public string ColorError { get; set; } = "#ef4444";
    public string ColorInformacion { get; set; } = "#3b82f6";

    public DateTime? FechaActualizacion { get; set; }
    public int? ActualizadoPorUsuarioId { get; set; }
}

using InventoryApp.Application.DTOs.Contabilidad;
using InventoryApp.Application.Exceptions;
using InventoryApp.Domain.Entities.Contabilidad;

namespace InventoryApp.Application.Services;

/// <summary>
/// Traduce un evento de negocio validado a un asiento contable usando la configuración persistida.
/// La escritura e idempotencia final permanecen delegadas a IAsientoContableWriter.
/// </summary>
public static class MotorContableApplicationService
{
    public static CrearAsientoContableDto CrearAsiento(
        EventoContableDto evento,
        ConfiguracionContable configuracion)
    {
        ArgumentNullException.ThrowIfNull(evento);
        ArgumentNullException.ThrowIfNull(configuracion);

        evento.Validar();

        if (!configuracion.Activo)
            throw new BusinessRuleException($"La configuración contable para {evento.Tipo} está inactiva.");

        if (configuracion.Evento != evento.Tipo)
            throw new BusinessRuleException("La configuración contable no corresponde al tipo de evento solicitado.");

        if (configuracion.CuentaDebeId <= 0 || configuracion.CuentaHaberId <= 0)
            throw new BusinessRuleException("La configuración contable debe definir cuentas válidas de debe y haber.");

        if (configuracion.CuentaDebeId == configuracion.CuentaHaberId)
            throw new BusinessRuleException("Las cuentas de debe y haber deben ser diferentes.");

        var referencia = evento.Referencia.Trim();
        var numero = $"AUTO-{(int)evento.Tipo}-{evento.DocumentoOrigenId}";

        return new CrearAsientoContableDto
        {
            Fecha = evento.Fecha,
            Concepto = $"{evento.Tipo}: {referencia}",
            Numero = numero,
            DocumentoOrigenId = evento.DocumentoOrigenId,
            TipoDocumentoOrigen = evento.Tipo.ToString(),
            Detalles =
            [
                new CrearAsientoDetalleDto
                {
                    CuentaContableId = configuracion.CuentaDebeId,
                    Debe = evento.Monto,
                    Haber = 0m,
                    Referencia = referencia
                },
                new CrearAsientoDetalleDto
                {
                    CuentaContableId = configuracion.CuentaHaberId,
                    Debe = 0m,
                    Haber = evento.Monto,
                    Referencia = referencia
                }
            ]
        };
    }
}

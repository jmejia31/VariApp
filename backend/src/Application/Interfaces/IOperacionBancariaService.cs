using InventoryApp.Application.DTOs.Bancos;

namespace InventoryApp.Application.Interfaces;

public interface IOperacionBancariaService
{
    Task RegistrarDepositoAsync(DepositoBancarioDto dto, int usuarioId);
    Task RegistrarRetiroAsync(RetiroBancarioDto dto, int usuarioId);
    Task RegistrarTransferenciaAsync(TransferenciaBancariaDto dto, int usuarioId);
    Task RegistrarComisionAsync(ComisionBancariaDto dto, int usuarioId);
    Task RegistrarInteresAsync(InteresBancarioDto dto, int usuarioId);
    Task RegistrarConciliacionAsync(ConciliacionBancariaDto dto, int usuarioId);
}

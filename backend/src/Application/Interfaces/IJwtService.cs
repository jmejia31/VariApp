using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Interfaces;

public interface IJwtService
{
    (string Token, DateTime ExpiraEn) GenerarToken(Usuario usuario);
}

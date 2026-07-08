using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto);
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace InventoryApp.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime ExpiraEn) GenerarToken(Usuario usuario)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secret = jwtSettings["Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret no está configurado.");
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];

        // El token ya no representa una expiración fija de la sesión. El frontend
        // lo renueva mientras detecta actividad y cierra la sesión tras 30 minutos
        // continuos de inactividad. Se deja un margen mínimo para completar la
        // renovación sin cerrar al usuario en medio de una operación.
        var expiraMinutos = int.TryParse(jwtSettings["ExpiraMinutos"], out var m) ? m : 35;
        expiraMinutos = Math.Clamp(expiraMinutos, 35, 720);

        var expiraEn = DateTime.UtcNow.AddMinutes(expiraMinutos);

        var esAdministrador = usuario.RolEntidad?.EsAdministrador ?? (usuario.Rol == Domain.Enums.RolUsuario.Administrador);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.NombreUsuario),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("id", usuario.Id.ToString()),
            new Claim("nombreUsuario", usuario.NombreUsuario),
            new Claim("nombreCompleto", usuario.NombreCompleto),
            new Claim("rol", usuario.Rol.ToString()),
            new Claim("esAdministrador", esAdministrador.ToString()),
            new Claim(ClaimTypes.Role, usuario.Rol.ToString())
        };

        if (usuario.RolId.HasValue)
            claims.Add(new Claim("rolId", usuario.RolId.Value.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiraEn,
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return (tokenString, expiraEn);
    }
}

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
        var expiraMinutos = int.TryParse(jwtSettings["ExpiraMinutos"], out var m) ? m : 480;

        var expiraEn = DateTime.UtcNow.AddMinutes(expiraMinutos);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.NombreUsuario),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("id", usuario.Id.ToString())
        };

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

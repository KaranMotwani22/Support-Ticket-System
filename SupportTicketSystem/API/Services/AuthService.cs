using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using SupportTicketAPI.Data;
using SupportTicketAPI.DTOs;
using SupportTicketAPI.Models;

namespace SupportTicketAPI.Services;

public class AuthService
{
    private readonly DatabaseContext _db;
    private readonly IConfiguration _config;

    public AuthService(DatabaseContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest req)
    {
        using var con = _db.CreateConnection();
        await con.OpenAsync();

        var user = await con.QuerySingleOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Username = @Username AND IsActive = 1",
            new { req.Username });

        if (user == null) return null;
        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.Password)) return null;

        var token = GenerateJwt(user);

        return new LoginResponse
        {
            Token    = token,
            UserId   = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Role     = user.Role
        };
    }

    private string GenerateJwt(User user)
    {
        var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(8);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name,           user.Username),
            new Claim(ClaimTypes.Role,           user.Role),
            new Claim("FullName",                user.FullName),
        };

        var token = new JwtSecurityToken(
            issuer:   _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims:   claims,
            expires:  expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

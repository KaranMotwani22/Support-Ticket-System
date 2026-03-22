using Microsoft.AspNetCore.Mvc;
using SupportTicketAPI.DTOs;
using SupportTicketAPI.Services;

namespace SupportTicketAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth) => _auth = auth;

    /// <summary>Login and receive a JWT token.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(ApiResponse<string>.Fail("Username and password are required."));

        var result = await _auth.LoginAsync(req);
        if (result == null)
            return Unauthorized(ApiResponse<string>.Fail("Invalid username or password."));

        return Ok(ApiResponse<LoginResponse>.Ok(result, "Login successful."));
    }
}

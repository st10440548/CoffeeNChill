using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CoffeeNChill.Gateway;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace CoffeeNChill.Gateway.Controllers;

public sealed record RegisterRequest([Required, EmailAddress] string Email, [Required] string FullName, [Required, MinLength(10)] string Password);
public sealed record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);

/// <summary>Customer registration and JWT login.</summary>
[ApiController, Route("api/auth")]
public sealed class AuthController(UserStore users, IConfiguration configuration) : ControllerBase
{
    /// <summary>Register a customer account. Privileged roles cannot be self-assigned.</summary>
    [HttpPost("register"), AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(request.FullName) || request.Password.Length > 256)
            return BadRequest("Invalid name or password length.");
        if (!await users.RegisterAsync(email, request.FullName.Trim(), request.Password))
            return Conflict("User already exists.");
        return StatusCode(201, new { email, role = "Customer" });
    }

    /// <summary>Validate credentials and return a signed bearer token.</summary>
    [HttpPost("login"), AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await users.VerifyAsync(request.Email.Trim().ToLowerInvariant(), request.Password);
        if (user is null) return Unauthorized("Invalid credentials.");
        var issuer = configuration["Jwt:Issuer"] ?? "CoffeeNChill";
        var audience = configuration["Jwt:Audience"] ?? "CoffeeNChillClients";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var claims = new[] {
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.RowKey),
            new Claim(ClaimTypes.Role, user.PartitionKey)
        };
        var expiry = DateTime.UtcNow.AddHours(2);
        var jwt = new JwtSecurityToken(issuer, audience, claims, expires: expiry,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(jwt), tokenType = "Bearer", expiresAt = expiry, role = user.PartitionKey });
    }
}

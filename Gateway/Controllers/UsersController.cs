using System.ComponentModel.DataAnnotations;
using CoffeeNChill.Gateway;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Gateway.Controllers;

public sealed record CreateStaffRequest([Required, EmailAddress] string Email, [Required] string FullName, [Required, MinLength(12)] string Password);

/// <summary>Administrative staff account provisioning.</summary>
[ApiController, Route("api/users"), Authorize(Roles = "Admin")]
public sealed class UsersController(UserStore users) : ControllerBase
{
    /// <summary>Create a Staff user; accessible only with an Admin JWT.</summary>
    [HttpPost("staff")]
    public async Task<IActionResult> CreateStaff(CreateStaffRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) || request.Password.Length > 256)
            return BadRequest("Invalid name or password length.");
        var email = request.Email.Trim().ToLowerInvariant();
        if (!await users.CreateAsync(email, request.FullName.Trim(), request.Password, "Staff"))
            return Conflict("User already exists.");
        return StatusCode(201, new { email, role = "Staff" });
    }
}

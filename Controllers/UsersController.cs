using System.Security.Claims;
using DogWalkingApp.Api.Data;
using DogWalkingApp.Api.Dtos;
using DogWalkingApp.Api.Models;
using DogWalkingApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DogWalkingApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtService _jwtService;

    public UsersController(AppDbContext db, JwtService jwtService)
    {
        _db = db;
        _jwtService = jwtService;
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var role = NormalizeRole(request.Role);
        if (role is null)
        {
            return BadRequest("Роль должна быть Owner, Walker или Both.");
        }

        if (await _db.Users.AnyAsync(user => user.Email == email))
        {
            return Conflict("Аккаунт с таким email уже существует.");
        }

        var user = new User
        {
            PublicId = Guid.NewGuid(),
            Email = email,
            Name = request.Name.Trim(),
            Role = role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Conflict("Аккаунт с таким email уже существует.");
        }

        return CreatedAtAction(nameof(GetMe), new { }, new { message = "Аккаунт создан. Теперь можно войти." });
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(item => item.Email == email && item.DeletedAt == null);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized("Неверный email или пароль.");
        }

        var response = new AuthResponse(
            _jwtService.GenerateToken(user),
            new AuthUserDto(user.Id, user.PublicId, user.Name, user.Email, user.Role));

        return Ok(response);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthUserDto>> GetMe()
    {
        if (!long.TryParse(User.FindFirstValue("uid"), out var userId))
        {
            return Unauthorized();
        }

        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == userId && item.DeletedAt == null);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(new AuthUserDto(user.Id, user.PublicId, user.Name, user.Email, user.Role));
    }

    private static string? NormalizeRole(string role) => role.Trim().ToLowerInvariant() switch
    {
        "owner" => "owner",
        "walker" or "sitter" => "sitter",
        "both" => "both",
        _ => null
    };
}

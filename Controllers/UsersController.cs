using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using DogWalkingApp.Api.Models;
using DogWalkingApp.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DogWalkingApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UsersController(AppDbContext context, IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public class RegisterDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;

        public string Role { get; set; } = "Owner";
    }

    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var role = request.Role?.Trim().ToLowerInvariant() switch
        {
            "owner" => "Owner",
            "walker" => "Walker",
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Укажите имя.");
        }

        if (role.Length == 0)
        {
            return BadRequest("Роль должна быть Owner или Walker.");
        }

        if (await _context.Users.AnyAsync(user => user.Email.ToLower() == email))
        {
            return Conflict("Пользователь с таким email уже существует.");
        }

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            Role = role
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Пользователь успешно зарегистрирован!", UserId = user.Id });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(item => item.Email.ToLower() == email);
        if (user is null)
        {
            return Unauthorized("Неверный email или пароль.");
        }

        var verification = PasswordVerificationResult.Failed;
        try
        {
            verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        }
        catch (FormatException)
        {
            // Accounts created by the old prototype may still have a plain-text password.
        }

        if (verification == PasswordVerificationResult.Failed &&
            string.Equals(user.PasswordHash, request.Password, StringComparison.Ordinal))
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
            verification = PasswordVerificationResult.Success;
        }

        if (verification == PasswordVerificationResult.Failed)
        {
            return Unauthorized("Неверный email или пароль.");
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        }

        var accessToken = SessionToken.Create();
        var now = DateTime.UtcNow;
        var expiresAt = now.AddDays(14);
        _context.UserSessions.Add(new UserSession
        {
            UserId = user.Id,
            TokenHash = SessionToken.Hash(accessToken),
            CreatedAt = now,
            ExpiresAt = expiresAt
        });
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = "Вход выполнен.",
            AccessToken = accessToken,
            ExpiresAt = expiresAt,
            User = new { user.Id, user.Name, user.Email, user.Role }
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var authorization = Request.Headers.Authorization.ToString();
        var token = authorization["Bearer ".Length..].Trim();
        var tokenHash = SessionToken.Hash(token);
        var session = await _context.UserSessions.FirstOrDefaultAsync(item => item.TokenHash == tokenHash);

        if (session is not null && session.RevokedAt is null)
        {
            session.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return NoContent();
    }
}
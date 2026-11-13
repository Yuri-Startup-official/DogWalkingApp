using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DogWalkingApp.Api.Models;

namespace DogWalkingApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    // Класс для приема данных (DTO)
    public class RegisterDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Owner";
    }

    // POST: api/users/register
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto request)
    {
        // Проверяем, есть ли уже такой email
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return BadRequest("Пользователь с таким email уже существует.");
        }

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = request.Password, // Пока просто для теста (позже сделаем хеширование!)
            Role = request.Role
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Пользователь успешно зарегистрирован!", UserId = user.Id });
    }
}
using DogWalkingApp.Api.Data;
using DogWalkingApp.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DogWalkingApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db) => _db = db;

    // GET: api/users
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetAll()
    {
        return await _db.Users
            .Where(u => u.DeletedAt == null)
            .ToListAsync();
    }

    // GET: api/users/5
    [HttpGet("{id:long}")]
    public async Task<ActionResult<User>> GetById(long id)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

        if (user == null) return NotFound();
        return user;
    }

    // POST: api/users
    [HttpPost]
    public async Task<ActionResult<User>> Create([FromBody] CreateUserRequest request)
    {
        var user = new User
        {
            PublicId = Guid.NewGuid(),
            Email = request.Email,
            Phone = request.Phone,
            PasswordHash = request.PasswordHash, // В реале — хешировать!
            Name = request.Name,
            Role = request.Role ?? "owner"
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }
}

// DTO для создания — не светим всю сущность в API
public record CreateUserRequest(
    string Email,
    string Name,
    string PasswordHash,
    string? Phone = null,
    string? Role = null
);

//PasswordHash в DTO — это временно, чтобы можно было протестировать. На проде сюда придёт Password (открытый), а хеширование будет внутри контроллера или сервиса.


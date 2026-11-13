using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DogWalkingApp.Api.Models;

namespace DogWalkingApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DogsController : ControllerBase
{
    private readonly AppDbContext _context;

    public DogsController(AppDbContext context)
    {
        _context = context;
    }

    // Класс для приема данных о собаке
    public class CreateDogDto
    {
        public string Name { get; set; } = string.Empty;
        public string Breed { get; set; } = string.Empty;
        public int Age { get; set; }
        public int OwnerId { get; set; } // ID владельца, к которому привяжем собаку
    }

    // POST: api/dogs
    [HttpPost]
    public async Task<IActionResult> CreateDog(CreateDogDto request)
    {
        // Проверяем, существует ли владелец с таким ID
        var ownerExists = await _context.Users.AnyAsync(u => u.Id == request.OwnerId);
        if (!ownerExists)
        {
            return BadRequest("Владелец с таким ID не найден.");
        }

        var dog = new Dog
        {
            Name = request.Name,
            Breed = request.Breed,
            Age = request.Age,
            OwnerId = request.OwnerId
        };

        _context.Dogs.Add(dog);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Собака успешно добавлена!", DogId = dog.Id });
    }
    // GET: api/dogs/owner/{ownerId}
    [HttpGet("owner/{ownerId}")]
    public async Task<IActionResult> GetDogsByOwner(int ownerId)
    {
        var dogs = await _context.Dogs
            .Where(d => d.OwnerId == ownerId)
            .ToListAsync();

        return Ok(dogs);
    }
}
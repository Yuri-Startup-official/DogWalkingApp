using DogWalkingApp.Api.Data;
using DogWalkingApp.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DogWalkingApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PetsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PetsController(AppDbContext db) => _db = db;

    // GET: api/pets
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Pet>>> GetAll()
    {
        return await _db.Pets
            .Where(p => p.DeletedAt == null)
            .ToListAsync();
    }

    // GET: api/pets/5
    [HttpGet("{id:long}")]
    public async Task<ActionResult<Pet>> GetById(long id)
    {
        var pet = await _db.Pets
            .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

        if (pet == null) return NotFound();
        return pet;
    }

    // POST: api/pets
    [HttpPost]
    public async Task<ActionResult<Pet>> Create([FromBody] CreatePetRequest request)
    {
        // Проверяем, что владелец существует
        var ownerExists = await _db.Users.AnyAsync(u => u.Id == request.OwnerId && u.DeletedAt == null);
        if (!ownerExists)
            return BadRequest($"Владелец с ID {request.OwnerId} не найден.");

        var pet = new Pet
        {
            PublicId = Guid.NewGuid(),
            OwnerId = request.OwnerId,
            Name = request.Name,
            Species = request.Species ?? "dog",
            Breed = request.Breed,
            BirthDate = request.BirthDate,
            WeightKg = request.WeightKg,
            Sex = request.Sex,
            Sterilized = request.Sterilized,
            PhotoUrl = request.PhotoUrl,
            MedicalNotes = request.MedicalNotes,
            Medications = request.Medications,
            Vaccinated = request.Vaccinated,
            VaccinationExpires = request.VaccinationExpires,
            BehaviorNotes = request.BehaviorNotes,
            AggressionLevel = request.AggressionLevel,
            SpecialNeeds = request.SpecialNeeds
        };

        _db.Pets.Add(pet);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = pet.Id }, pet);
    }
}

// DTO для создания питомца
public record CreatePetRequest(
    long OwnerId,
    string Name,
    string? Species = "dog",
    string? Breed = null,
    DateOnly? BirthDate = null,
    decimal? WeightKg = null,
    string? Sex = null,
    bool Sterilized = false,
    string? PhotoUrl = null,
    string? MedicalNotes = null,
    string? Medications = null,
    bool Vaccinated = false,
    DateOnly? VaccinationExpires = null,
    string? BehaviorNotes = null,
    int AggressionLevel = 0,
    string? SpecialNeeds = null
);
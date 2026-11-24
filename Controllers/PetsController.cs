using System.ComponentModel.DataAnnotations;
using DogWalkingApp.Api.Data;
using DogWalkingApp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DogWalkingApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "owner,both")]
public sealed class PetsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PetsController(AppDbContext db) => _db = db;

    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<PetDto>>> GetMine()
    {
        if (!long.TryParse(User.FindFirst("uid")?.Value, out var ownerId))
        {
            return Unauthorized();
        }

        var pets = await _db.Pets.AsNoTracking()
            .Where(pet => pet.OwnerId == ownerId && pet.DeletedAt == null)
            .OrderBy(pet => pet.Name)
            .ToListAsync();

        return Ok(pets.Select(ToDto).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<PetDto>> Create([FromBody] CreatePetRequest request)
    {
        if (!long.TryParse(User.FindFirst("uid")?.Value, out var ownerId))
        {
            return Unauthorized();
        }

        var birthDate = request.BirthDate;
        if (birthDate is null && request.Age is not null)
        {
            birthDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-request.Age.Value);
        }

        var pet = new Pet
        {
            PublicId = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = request.Name.Trim(),
            Species = "dog",
            Breed = string.IsNullOrWhiteSpace(request.Breed) ? null : request.Breed.Trim(),
            BirthDate = birthDate,
            WeightKg = request.WeightKg,
            Sex = request.Sex,
            Sterilized = request.Sterilized,
            MedicalNotes = request.MedicalNotes,
            Medications = request.Medications,
            Vaccinated = request.Vaccinated,
            VaccinationExpires = request.VaccinationExpires,
            BehaviorNotes = request.BehaviorNotes,
            AggressionLevel = request.AggressionLevel,
            SpecialNeeds = request.SpecialNeeds,
            CreatedAt = DateTime.UtcNow
        };

        _db.Pets.Add(pet);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMine), new { }, ToDto(pet));
    }

    private static PetDto ToDto(Pet pet) => new(
        pet.Id,
        pet.PublicId,
        pet.Name,
        pet.Breed,
        CalculateAge(pet.BirthDate),
        pet.BirthDate,
        pet.WeightKg,
        pet.Sex,
        pet.Sterilized,
        pet.MedicalNotes,
        pet.Medications,
        pet.Vaccinated,
        pet.VaccinationExpires,
        pet.BehaviorNotes,
        pet.AggressionLevel,
        pet.SpecialNeeds);

    private static int? CalculateAge(DateOnly? birthDate)
    {
        if (birthDate is null)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - birthDate.Value.Year;
        if (birthDate.Value > today.AddYears(-age))
        {
            age--;
        }

        return Math.Max(0, age);
    }

    public sealed class CreatePetRequest
    {
        [Required, StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Breed { get; set; }

        [Range(0, 40)]
        public int? Age { get; set; }

        public DateOnly? BirthDate { get; set; }

        [Range(typeof(decimal), "0", "300")]
        public decimal? WeightKg { get; set; }

        [StringLength(10)]
        public string? Sex { get; set; }

        public bool Sterilized { get; set; }
        public bool Vaccinated { get; set; }
        public DateOnly? VaccinationExpires { get; set; }

        [StringLength(2000)]
        public string? MedicalNotes { get; set; }

        [StringLength(2000)]
        public string? Medications { get; set; }

        [StringLength(2000)]
        public string? BehaviorNotes { get; set; }

        [Range(0, 3)]
        public int AggressionLevel { get; set; }

        [StringLength(2000)]
        public string? SpecialNeeds { get; set; }
    }

    public sealed record PetDto(
        long Id,
        Guid PublicId,
        string Name,
        string? Breed,
        int? Age,
        DateOnly? BirthDate,
        decimal? WeightKg,
        string? Sex,
        bool Sterilized,
        string? MedicalNotes,
        string? Medications,
        bool Vaccinated,
        DateOnly? VaccinationExpires,
        string? BehaviorNotes,
        int AggressionLevel,
        string? SpecialNeeds);
}

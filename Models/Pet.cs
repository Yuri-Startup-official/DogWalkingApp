using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DogWalkingApp.Api.Models;

public class Pet
{
    public long Id { get; set; }
    public Guid PublicId { get; set; }
    public long OwnerId { get; set; }
    
    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;
    
    [MaxLength(10)]
    public string Species { get; set; } = "dog";
    
    [MaxLength(100)]
    public string? Breed { get; set; }
    
    public DateOnly? BirthDate { get; set; }
    
    [Column(TypeName = "decimal(5,2)")]
    public decimal? WeightKg { get; set; }
    
    [MaxLength(10)]
    public string? Sex { get; set; }
    
    public bool Sterilized { get; set; }
    public string? PhotoUrl { get; set; }
    public string? MedicalNotes { get; set; }
    public string? Medications { get; set; }
    public bool Vaccinated { get; set; }
    public DateOnly? VaccinationExpires { get; set; }
    public string? BehaviorNotes { get; set; }
    
    public int AggressionLevel { get; set; } // 0..3
    public string? SpecialNeeds { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
    
    public User Owner { get; set; } = null!;
}
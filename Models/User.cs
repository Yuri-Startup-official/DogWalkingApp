using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DogWalkingApp.Api.Models;

public class User
{
    public long Id { get; set; }
    public Guid PublicId { get; set; }
    
    [Required, MaxLength(255)]
    public string Email { get; set; } = null!;
    
    [MaxLength(30)]
    public string? Phone { get; set; }
    
    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = null!;
    
    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;
    
    public string? AvatarUrl { get; set; }
    
    // В PostgreSQL это ENUM, в C# — обычная строка или enum
    [MaxLength(10)]
    public string Role { get; set; } = "owner"; // owner | sitter | both
    
    public bool EmailVerified { get; set; }
    public bool PhoneVerified { get; set; }
    
    [Column(TypeName = "decimal(3,2)")]
    public decimal RatingAsSitter { get; set; }
    
    public int ReviewsAsSitter { get; set; }
    
    [Column(TypeName = "decimal(3,2)")]
    public decimal RatingAsOwner { get; set; }
    
    public int ReviewsAsOwner { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}
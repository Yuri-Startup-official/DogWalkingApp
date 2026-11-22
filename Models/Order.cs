using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DogWalkingApp.Api.Models;

public class Order
{
    public long Id { get; set; }
    public Guid PublicId { get; set; }
    
    // Прогулка или передержка
    [MaxLength(10)]
    public string ServiceType { get; set; } = null!;
    
    public long OwnerId { get; set; }
    public long? SitterId { get; set; }
    
    // pending | accepted | in_progress | completed | cancelled | disputed
    [MaxLength(20)]
    public string Status { get; set; } = "pending";
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal? TotalPrice { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal? PlatformFee { get; set; }
    
    public string? PaymentIntentId { get; set; }
    public string? PaymentStatus { get; set; } = "pending";
    public DateTime? PaidAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
    
    public User Owner { get; set; } = null!;
    public User? Sitter { get; set; }
}
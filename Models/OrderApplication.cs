using System.ComponentModel.DataAnnotations;

namespace DogWalkingApp.Api.Models;

public sealed class OrderApplication
{
    public long Id { get; set; }

    public long OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public long SitterId { get; set; }
    public User Sitter { get; set; } = null!;

    [MaxLength(20)]
    public string Status { get; set; } = "pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

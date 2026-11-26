using System.ComponentModel.DataAnnotations;

namespace DogWalkingApp.Api.Models;

public sealed class SupportConversation
{
    public long Id { get; set; }

    public long RequesterId { get; set; }
    public User Requester { get; set; } = null!;

    public long? AssignedManagerId { get; set; }
    public User? AssignedManager { get; set; }

    public long? OrderId { get; set; }
    public Order? Order { get; set; }

    [Required, MaxLength(40)]
    public string Topic { get; set; } = "other";

    [Required, MaxLength(20)]
    public string Status { get; set; } = "open";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    public ICollection<SupportMessage> Messages { get; set; } = new List<SupportMessage>();
}

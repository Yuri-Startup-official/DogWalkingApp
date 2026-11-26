using System.ComponentModel.DataAnnotations;

namespace DogWalkingApp.Api.Models;

public sealed class SupportMessage
{
    public long Id { get; set; }

    public long ConversationId { get; set; }
    public SupportConversation Conversation { get; set; } = null!;

    public long SenderId { get; set; }
    public User Sender { get; set; } = null!;

    [Required, MaxLength(4000)]
    public string Body { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

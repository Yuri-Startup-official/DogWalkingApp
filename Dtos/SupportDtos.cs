using System.ComponentModel.DataAnnotations;

namespace DogWalkingApp.Api.Dtos;

public sealed class CreateSupportConversationRequest
{
    [Required, StringLength(40)]
    public string Topic { get; set; } = "other";

    public long? OrderId { get; set; }

    [Required, StringLength(4000, MinimumLength = 1)]
    public string Message { get; set; } = string.Empty;
}

public sealed class SendSupportMessageRequest
{
    [Required, StringLength(4000, MinimumLength = 1)]
    public string Body { get; set; } = string.Empty;
}

public sealed class UpdateSupportStatusRequest
{
    [Required, StringLength(20)]
    public string Status { get; set; } = string.Empty;
}

public sealed record SupportConversationDto(
    long Id,
    string Topic,
    string Status,
    long? OrderId,
    string RequesterName,
    string RequesterRole,
    long? AssignedManagerId,
    string? AssignedManagerName,
    string? LastMessage,
    DateTime UpdatedAt);

public sealed record SupportMessageDto(
    long Id,
    long SenderId,
    string SenderName,
    string SenderRole,
    string Body,
    DateTime CreatedAt);

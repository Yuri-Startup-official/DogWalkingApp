using System.ComponentModel.DataAnnotations;

namespace DogWalkingApp.Api.Models;

public class WalkingOrder
{
    public long OrderId { get; set; }
    public DateTime StartTime { get; set; }
    public int DurationMin { get; set; }
    public string? MeetingPoint { get; set; }
    public int PetsCount { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }

    public Order Order { get; set; } = null!;
}
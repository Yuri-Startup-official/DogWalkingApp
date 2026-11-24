namespace DogWalkingApp.Api.Models;

public class BoardingOrder
{
    public long OrderId { get; set; }
    public DateTime CheckInAt { get; set; }
    public DateTime CheckOutAt { get; set; }
    public int PetsCount { get; set; }
    public DateTime? ActualCheckIn { get; set; }
    public DateTime? ActualCheckOut { get; set; }

    public Order Order { get; set; } = null!;
}

namespace DogWalkingApp.Api.Models;

public class BoardingOrder
{
    public long OrderId { get; set; }
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public int PetsCount { get; set; }
    public DateTime? ActualCheckIn { get; set; }
    public DateTime? ActualCheckOut { get; set; }

    public Order Order { get; set; } = null!;
}
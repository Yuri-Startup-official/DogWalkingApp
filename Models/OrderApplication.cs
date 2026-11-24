namespace DogWalkingApp.Api.Models;

public class OrderApplication
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int WalkerId { get; set; }
    public User? Walker { get; set; }

    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

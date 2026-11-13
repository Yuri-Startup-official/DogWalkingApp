namespace DogWalkingApp.Api.Models;

public class Order
{
    public int Id { get; set; }
    
    public int OwnerId { get; set; }
    public User? Owner { get; set; }
    
    public int? WalkerId { get; set; }
    public User? Walker { get; set; }
    
    public int DogId { get; set; }
    public Dog? Dog { get; set; }
    
    public string Status { get; set; } = "Pending"; 
    
    public DateTime Date { get; set; }
    
    public string? ReportText { get; set; }
}
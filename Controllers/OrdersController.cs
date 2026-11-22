using DogWalkingApp.Api.Data;
using DogWalkingApp.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DogWalkingApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;

    public OrdersController(AppDbContext db) => _db = db;

    // GET: api/orders
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetAll()
    {
        return await _db.Orders
            .Include(o => o.Owner)
            .Include(o => o.Sitter)
            .ToListAsync();
    }

    // GET: api/orders/5
    [HttpGet("{id:long}")]
    public async Task<ActionResult<Order>> GetById(long id)
    {
        var order = await _db.Orders
            .Include(o => o.Owner)
            .Include(o => o.Sitter)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();
        return order;
    }

    // POST: api/orders
    [HttpPost]
    public async Task<ActionResult<Order>> Create([FromBody] CreateOrderRequest request)
    {
        // Проверка владельца
        var ownerExists = await _db.Users.AnyAsync(u => u.Id == request.OwnerId && u.DeletedAt == null);
        if (!ownerExists)
            return BadRequest($"Владелец с ID {request.OwnerId} не найден.");

        // Создаем сам заказ
        var order = new Order
        {
            PublicId = Guid.NewGuid(),
            ServiceType = request.ServiceType,
            OwnerId = request.OwnerId,
            SitterId = request.SitterId,
            Status = "pending",
            TotalPrice = request.TotalPrice,
            PlatformFee = request.PlatformFee
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(); // Сохраняем, чтобы получить ID

        // Создаем детали в зависимости от типа
        if (request.ServiceType == "walking")
        {
            if (request.StartTime == null || request.DurationMin == null)
                return BadRequest("Для прогулки нужно указать StartTime и DurationMin.");

            var walking = new WalkingOrder
            {
                OrderId = order.Id,
                StartTime = request.StartTime.Value,
                DurationMin = request.DurationMin.Value,
                PetsCount = request.PetsCount ?? 1,
                MeetingPoint = request.MeetingPoint
            };
            _db.WalkingOrders.Add(walking);
        }
        else if (request.ServiceType == "boarding")
        {
            if (request.CheckIn == null || request.CheckOut == null)
                return BadRequest("Для передержки нужно указать CheckIn и CheckOut.");

            var boarding = new BoardingOrder
            {
                OrderId = order.Id,
                CheckIn = request.CheckIn.Value,
                CheckOut = request.CheckOut.Value,
                PetsCount = request.PetsCount ?? 1
            };
            _db.BoardingOrders.Add(boarding);
        }

        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }
}

// DTO для создания заказа
public record CreateOrderRequest(
    string ServiceType,
    long OwnerId,
    long? SitterId = null,
    decimal? TotalPrice = null,
    decimal? PlatformFee = null,
    
    // Для прогулки
    DateTime? StartTime = null,
    int? DurationMin = null,
    string? MeetingPoint = null,
    
    // Для передержки
    DateOnly? CheckIn = null,
    DateOnly? CheckOut = null,
    
    // Общее
    int? PetsCount = null
);
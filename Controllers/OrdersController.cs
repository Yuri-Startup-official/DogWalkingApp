using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DogWalkingApp.Api.Models;

namespace DogWalkingApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _context;

    public OrdersController(AppDbContext context)
    {
        _context = context;
    }

    public class CreateOrderDto
    {
        public int OwnerId { get; set; }
        public int DogId { get; set; }
        public DateTime Date { get; set; }
    }

    // POST: api/orders
    [HttpPost]
    public async Task<IActionResult> CreateOrder(CreateOrderDto request)
    {
        // Проверяем, существует ли собака и принадлежит ли она этому владельцу
        var dog = await _context.Dogs.FirstOrDefaultAsync(d => d.Id == request.DogId && d.OwnerId == request.OwnerId);
        if (dog == null)
        {
            return BadRequest("Собака не найдена или не принадлежит этому владельцу.");
        }

        var order = new Order
        {
            OwnerId = request.OwnerId,
            DogId = request.DogId,
            Date = request.Date,
            Status = "Pending" // Ожидает выгульщика
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Заказ успешно создан!", OrderId = order.Id });
    }
}
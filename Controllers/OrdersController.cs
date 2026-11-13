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
    // 1. GET: api/orders/available - Посмотреть все свободные заказы
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableOrders()
    {
        var orders = await _context.Orders
            .Include(o => o.Dog)   // Подтягиваем данные о собаке
            .Include(o => o.Owner) // Подтягиваем данные о владельце
            .Where(o => o.Status == "Pending")
            .ToListAsync();

        return Ok(orders);
    }

// 2. PUT: api/orders/{orderId}/take/{walkerId} - Взять заказ
    [HttpPut("{orderId}/take/{walkerId}")]
    public async Task<IActionResult> TakeOrder(int orderId, int walkerId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null) return NotFound("Заказ не найден.");
    
        if (order.Status != "Pending") return BadRequest("Этот заказ уже взят или завершен.");

        // Проверяем, что пользователь действительно выгульщик
        var walker = await _context.Users.FindAsync(walkerId);
        if (walker == null || walker.Role != "Walker")
        {
            return BadRequest("Пользователь не найден или не является выгульщиком.");
        }

        order.WalkerId = walkerId;
        order.Status = "InProgress"; // Меняем статус на "В процессе"
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Заказ успешно взят в работу!", OrderId = order.Id });
    }

// 3. PUT: api/orders/{orderId}/complete - Завершить заказ
    [HttpPut("{orderId}/complete")]
    public async Task<IActionResult> CompleteOrder(int orderId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null) return NotFound("Заказ не найден.");

        if (order.Status != "InProgress") return BadRequest("Можно завершить только заказ в процессе.");

        order.Status = "Completed"; // Меняем статус на "Завершен"
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Заказ успешно завершен!", OrderId = order.Id });
    }
}
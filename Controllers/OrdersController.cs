using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
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

        public int DogId { get; set; }
        public string ServiceType { get; set; } = "Walk";
        public DateTime StartAt { get; set; }
        public DateTime? EndAt { get; set; }
        public int? DurationMinutes { get; set; }
    }

    // POST: api/orders
    [Authorize(Roles = "Owner")]
    [HttpPost]
    public async Task<IActionResult> CreateOrder(CreateOrderDto request)
    {
        if (string.IsNullOrWhiteSpace(request.ServiceType))
        {
            return BadRequest("Тип услуги обязателен.");
        }

        var serviceType = request.ServiceType.Trim().ToLowerInvariant() switch
        {
            "walk" => "Walk",
            "boarding" => "Boarding",
            _ => string.Empty
        };
        if (serviceType.Length == 0)
        {
            return BadRequest("Тип услуги должен быть Walk или Boarding.");
        }

        if (request.StartAt == default)
        {
            return BadRequest("Укажите дату и время начала.");
        }

        if (serviceType == "Walk" && (request.DurationMinutes is null or <= 0 || request.EndAt is not null))
        {
            return BadRequest("Для выгула укажите положительную длительность и не указывайте дату окончания.");
        }

        if (serviceType == "Boarding" && (request.EndAt is null || request.EndAt.Value <= request.StartAt || request.DurationMinutes is not null))
        {
            return BadRequest("Для передержки укажите дату окончания позже даты начала; длительность прогулки не нужна.");
        }
        var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Проверяем, существует ли собака и принадлежит ли она этому владельцу
        var dog = await _context.Dogs.FirstOrDefaultAsync(d => d.Id == request.DogId && d.OwnerId == ownerId);
        if (dog == null)
        {
            return BadRequest("Собака не найдена или не принадлежит этому владельцу.");
        }

        var order = new Order
        {
            OwnerId = ownerId,
            DogId = request.DogId,
            ServiceType = serviceType,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            DurationMinutes = request.DurationMinutes,
            Status = "Pending" // Ожидает исполнителя
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Заказ успешно создан!", OrderId = order.Id });
    }
    
    // НОВЫЙ КЛАСС: Для приема отчета
    public class CompleteOrderDto
    {
        public string? ReportText { get; set; }
    }
    // 1. GET: api/orders/available - Посмотреть все свободные заказы
    [Authorize(Roles = "Walker")]
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
    [Authorize(Roles = "Walker")]
    [HttpPut("{orderId}/take")]
    public async Task<IActionResult> TakeOrder(int orderId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null) return NotFound("Заказ не найден.");
    
        if (order.Status != "Pending") return BadRequest("Этот заказ уже взят или завершен.");

        // Проверяем, что пользователь действительно выгульщик
        var walkerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
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
    // 3. PUT: api/orders/{orderId}/complete - Завершить заказ и добавить отчет
    [Authorize(Roles = "Walker")]
    [HttpPut("{orderId}/complete")]
    public async Task<IActionResult> CompleteOrder(int orderId, [FromBody] CompleteOrderDto request)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null) return NotFound("Заказ не найден.");

        var walkerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (order.WalkerId != walkerId) return Forbid();

        if (order.Status != "InProgress") return BadRequest("Можно завершить только заказ в процессе.");

        order.Status = "Completed"; // Меняем статус на "Завершен"
        order.ReportText = request.ReportText; // Сохраняем отчет
        
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Заказ успешно завершен!", OrderId = order.Id });
    }
}
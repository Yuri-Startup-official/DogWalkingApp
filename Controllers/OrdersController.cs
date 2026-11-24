using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DogWalkingApp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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

    public sealed class CreateOrderDto
    {
        [Range(1, int.MaxValue)]
        public int DogId { get; set; }

        [Required]
        public string ServiceType { get; set; } = "Walk";

        public DateTimeOffset StartAt { get; set; }
        public DateTimeOffset? EndAt { get; set; }
        public int? DurationMinutes { get; set; }

        [Required, StringLength(120)]
        public string Area { get; set; } = string.Empty;

        [Required, StringLength(300)]
        public string LocationAddress { get; set; } = string.Empty;

        [Required, StringLength(2000)]
        public string Description { get; set; } = string.Empty;
    }

    public sealed class CompleteOrderDto
    {
        [StringLength(2000)]
        public string? ReportText { get; set; }
    }

    public sealed record DogSummaryDto(int Id, string Name, string Breed, int Age);

    public sealed record OrderListDto(
        int Id,
        string ServiceType,
        string Status,
        DateTime StartAt,
        DateTime? EndAt,
        int? DurationMinutes,
        string Area,
        string? LocationAddress,
        string Description,
        string? ReportText,
        int ApplicationCount,
        string? ApplicationStatus,
        DogSummaryDto? Dog);

    public sealed record OrderApplicationDto(int WalkerId, string WalkerName, string Status, DateTime CreatedAt);

    [Authorize(Roles = "Owner")]
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto request)
    {
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

        if (string.IsNullOrWhiteSpace(request.Area)
            || string.IsNullOrWhiteSpace(request.LocationAddress)
            || string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest("Укажите район, адрес и описание заказа.");
        }

        var startAt = request.StartAt.UtcDateTime;
        var endAt = request.EndAt?.UtcDateTime;
        if (startAt == default)
        {
            return BadRequest("Укажите дату и время начала.");
        }

        if (serviceType == "Walk" && (request.DurationMinutes is null or <= 0 || endAt is not null))
        {
            return BadRequest("Для выгула укажите положительную длительность и не указывайте дату окончания.");
        }

        if (serviceType == "Boarding" && (endAt is null || endAt.Value <= startAt || request.DurationMinutes is not null))
        {
            return BadRequest("Для передержки укажите дату окончания позже даты начала; длительность прогулки не нужна.");
        }

        var ownerId = CurrentUserId;
        var dogExists = await _context.Dogs.AnyAsync(dog => dog.Id == request.DogId && dog.OwnerId == ownerId);
        if (!dogExists)
        {
            return BadRequest("Собака не найдена или не принадлежит этому владельцу.");
        }

        var order = new Order
        {
            OwnerId = ownerId,
            DogId = request.DogId,
            ServiceType = serviceType,
            StartAt = startAt,
            EndAt = endAt,
            DurationMinutes = request.DurationMinutes,
            Area = request.Area.Trim(),
            LocationAddress = request.LocationAddress.Trim(),
            Description = request.Description.Trim(),
            Status = "Pending"
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMyOrders), new { }, new { orderId = order.Id, order.Status });
    }

    [Authorize(Roles = "Owner,Walker")]
    [HttpGet("mine")]
    public async Task<IActionResult> GetMyOrders()
    {
        var userId = CurrentUserId;
        var isOwner = User.IsInRole("Owner");
        var query = _context.Orders
            .AsNoTracking()
            .Include(order => order.Dog)
            .Include(order => order.Applications)
            .AsQueryable();

        query = isOwner
            ? query.Where(order => order.OwnerId == userId)
            : query.Where(order => order.WalkerId == userId || order.Applications.Any(application => application.WalkerId == userId));

        var orders = await query.OrderByDescending(order => order.StartAt).ToListAsync();
        var result = orders.Select(order => ToOrderListDto(
            order,
            revealLocation: isOwner || order.WalkerId == userId,
            applicationStatus: order.Applications.FirstOrDefault(application => application.WalkerId == userId)?.Status));

        return Ok(result);
    }

    [Authorize(Roles = "Walker")]
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableOrders()
    {
        var walkerId = CurrentUserId;
        var orders = await _context.Orders
            .AsNoTracking()
            .Include(order => order.Dog)
            .Include(order => order.Applications)
            .Where(order => order.Status == "Pending" && order.OwnerId != walkerId)
            .OrderBy(order => order.StartAt)
            .ToListAsync();

        return Ok(orders.Select(order => ToOrderListDto(
            order,
            revealLocation: false,
            applicationStatus: order.Applications.FirstOrDefault(application => application.WalkerId == walkerId)?.Status)));
    }

    [Authorize(Roles = "Walker")]
    [HttpPost("{orderId:int}/applications")]
    public async Task<IActionResult> ApplyToOrder(int orderId)
    {
        var walkerId = CurrentUserId;
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var order = await _context.Orders
            .FromSqlInterpolated($"SELECT * FROM \"Orders\" WHERE \"Id\" = {orderId} FOR UPDATE")
            .FirstOrDefaultAsync();
        if (order is null)
        {
            return NotFound("Заказ не найден.");
        }

        if (order.Status != "Pending" || order.OwnerId == walkerId)
        {
            return Conflict("Этот заказ больше не доступен для отклика.");
        }

        var application = new OrderApplication { OrderId = orderId, WalkerId = walkerId, Status = "Pending" };
        _context.OrderApplications.Add(application);
        try
        {
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Conflict("Вы уже откликнулись на этот заказ.");
        }

        return Ok(new { orderId, application.Status, application.CreatedAt });
    }

    [Authorize(Roles = "Owner")]
    [HttpGet("{orderId:int}/applications")]
    public async Task<IActionResult> GetOrderApplications(int orderId)
    {
        var ownerId = CurrentUserId;
        var orderExists = await _context.Orders.AnyAsync(order => order.Id == orderId && order.OwnerId == ownerId);
        if (!orderExists)
        {
            return NotFound("Заказ не найден.");
        }

        var applications = await _context.OrderApplications
            .AsNoTracking()
            .Where(application => application.OrderId == orderId)
            .Include(application => application.Walker)
            .OrderBy(application => application.CreatedAt)
            .Select(application => new OrderApplicationDto(
                application.WalkerId,
                application.Walker!.Name,
                application.Status,
                application.CreatedAt))
            .ToListAsync();

        return Ok(applications);
    }

    [Authorize(Roles = "Owner")]
    [HttpPut("{orderId:int}/assign/{walkerId:int}")]
    public async Task<IActionResult> AssignOrder(int orderId, int walkerId)
    {
        var ownerId = CurrentUserId;
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var order = await _context.Orders
            .FromSqlInterpolated($"SELECT * FROM \"Orders\" WHERE \"Id\" = {orderId} FOR UPDATE")
            .FirstOrDefaultAsync();
        if (order is null || order.OwnerId != ownerId)
        {
            return NotFound("Заказ не найден.");
        }

        if (order.Status != "Pending")
        {
            return Conflict("Заказ уже назначен, отменён или недоступен.");
        }

        var hasPendingApplication = await _context.OrderApplications.AnyAsync(application =>
            application.OrderId == orderId && application.WalkerId == walkerId && application.Status == "Pending");
        if (!hasPendingApplication)
        {
            return BadRequest("У этого исполнителя нет активного отклика на заказ.");
        }

        order.WalkerId = walkerId;
        order.Status = "Assigned";
        await _context.SaveChangesAsync();

        await _context.OrderApplications
            .Where(application => application.OrderId == orderId && application.Status == "Pending")
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                application => application.Status,
                application => application.WalkerId == walkerId ? "Accepted" : "Rejected"));

        await transaction.CommitAsync();
        return NoContent();
    }

    [Authorize(Roles = "Walker")]
    [HttpPut("{orderId:int}/start")]
    public async Task<IActionResult> StartOrder(int orderId)
    {
        var walkerId = CurrentUserId;
        var updated = await _context.Orders
            .Where(order => order.Id == orderId && order.WalkerId == walkerId && order.Status == "Assigned")
            .ExecuteUpdateAsync(setters => setters.SetProperty(order => order.Status, "InProgress"));

        return updated == 1 ? NoContent() : Conflict("Начать можно только назначенный вам заказ.");
    }

    [Authorize(Roles = "Walker")]
    [HttpPut("{orderId:int}/complete")]
    public async Task<IActionResult> CompleteOrder(int orderId, [FromBody] CompleteOrderDto request)
    {
        var walkerId = CurrentUserId;
        var order = await _context.Orders.FirstOrDefaultAsync(item => item.Id == orderId);
        if (order is null)
        {
            return NotFound("Заказ не найден.");
        }

        if (order.WalkerId != walkerId)
        {
            return Forbid();
        }

        if (order.Status != "InProgress")
        {
            return Conflict("Завершить можно только заказ, который уже выполняется.");
        }

        order.Status = "Completed";
        order.ReportText = request.ReportText?.Trim();
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static OrderListDto ToOrderListDto(Order order, bool revealLocation, string? applicationStatus)
    {
        return new OrderListDto(
            order.Id,
            order.ServiceType,
            order.Status,
            order.StartAt,
            order.EndAt,
            order.DurationMinutes,
            order.Area,
            revealLocation ? order.LocationAddress : null,
            order.Description,
            order.ReportText,
            order.Applications.Count(application => application.Status == "Pending"),
            applicationStatus,
            order.Dog is null ? null : new DogSummaryDto(order.Dog.Id, order.Dog.Name, order.Dog.Breed, order.Dog.Age));
    }
}

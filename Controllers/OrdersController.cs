using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;
using DogWalkingApp.Api.Data;
using DogWalkingApp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DogWalkingApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;

    public OrdersController(AppDbContext db) => _db = db;

    [Authorize(Roles = "owner,both")]
    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderRequest request)
    {
        if (!TryGetUserId(out var ownerId))
        {
            return Unauthorized();
        }

        var serviceType = NormalizeService(request.ServiceType);
        if (serviceType is null)
        {
            return BadRequest("ServiceType должен быть Walk или Boarding.");
        }

        if (string.IsNullOrWhiteSpace(request.Area)
            || string.IsNullOrWhiteSpace(request.LocationAddress)
            || string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest("Укажите район, точный адрес и описание заказа.");
        }

        var startAt = request.StartAt.UtcDateTime;
        var endAt = request.EndAt?.UtcDateTime;
        if (startAt == default || startAt <= DateTime.UtcNow.AddMinutes(-1))
        {
            return BadRequest("Укажите дату начала в будущем.");
        }

        if (serviceType == "walking" && (request.DurationMinutes is null or <= 0 || endAt is not null))
        {
            return BadRequest("Для выгула укажите длительность и не задавайте дату окончания.");
        }

        if (serviceType == "boarding" && (endAt is null || endAt <= startAt || request.DurationMinutes is not null))
        {
            return BadRequest("Для передержки дата окончания должна быть позже даты начала.");
        }

        var pet = await _db.Pets.FirstOrDefaultAsync(item =>
            item.Id == request.PetId && item.OwnerId == ownerId && item.DeletedAt == null);
        if (pet is null)
        {
            return BadRequest("Питомец не найден или не принадлежит владельцу.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var order = new Order
        {
            PublicId = Guid.NewGuid(),
            OwnerId = ownerId,
            PetId = pet.Id,
            ServiceType = serviceType,
            Status = "pending",
            Area = request.Area.Trim(),
            LocationAddress = request.LocationAddress.Trim(),
            Description = request.Description.Trim(),
            TotalPrice = request.TotalPrice,
            PaymentStatus = "pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        if (serviceType == "walking")
        {
            order.WalkingDetails = new WalkingOrder
            {
                OrderId = order.Id,
                StartTime = startAt,
                DurationMin = request.DurationMinutes!.Value,
                PetsCount = 1,
                MeetingPoint = order.Area
            };
        }
        else
        {
            order.BoardingDetails = new BoardingOrder
            {
                OrderId = order.Id,
                CheckInAt = startAt,
                CheckOutAt = endAt!.Value,
                PetsCount = 1
            };
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        await LoadOrderDetails(_db.Orders.Where(item => item.Id == order.Id))
            .FirstAsync();
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, ToDto(order, revealLocation: true, applicationStatus: null, canContactSupport: true));
    }

    [Authorize(Roles = "owner,sitter,both")]
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetMine()
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var canOwn = User.IsInRole("owner") || User.IsInRole("both");
        var query = LoadOrderDetails(_db.Orders.AsNoTracking());
        query = canOwn
            ? query.Where(order => order.OwnerId == userId || order.SitterId == userId || order.Applications.Any(app => app.SitterId == userId))
            : query.Where(order => order.SitterId == userId || order.Applications.Any(app => app.SitterId == userId));

        var orders = await query.OrderByDescending(order => order.CreatedAt).ToListAsync();
        return Ok(orders.Select(order => ToDto(
            order,
            revealLocation: order.OwnerId == userId || order.SitterId == userId,
            applicationStatus: order.Applications.FirstOrDefault(app => app.SitterId == userId)?.Status,
            canContactSupport: order.OwnerId == userId || order.SitterId == userId)).ToList());
    }

    [Authorize(Roles = "sitter,both")]
    [HttpGet("available")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetAvailable()
    {
        if (!TryGetUserId(out var sitterId))
        {
            return Unauthorized();
        }

        var orders = await LoadOrderDetails(_db.Orders.AsNoTracking())
            .Where(order => order.Status == "pending" && order.OwnerId != sitterId)
            .OrderBy(order => order.ServiceType == "walking" ? order.WalkingDetails!.StartTime : order.BoardingDetails!.CheckInAt)
            .ToListAsync();

        return Ok(orders.Select(order => ToDto(
            order,
            revealLocation: false,
            applicationStatus: order.Applications.FirstOrDefault(app => app.SitterId == sitterId)?.Status,
            canContactSupport: false)).ToList());
    }

    [Authorize(Roles = "owner,sitter,both")]
    [HttpGet("{id:long}")]
    public async Task<ActionResult<OrderDto>> GetById(long id)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var order = await LoadOrderDetails(_db.Orders.AsNoTracking())
            .FirstOrDefaultAsync(item => item.Id == id);
        if (order is null)
        {
            return NotFound();
        }

        var isOwner = order.OwnerId == userId && (User.IsInRole("owner") || User.IsInRole("both"));
        var isAssignedSitter = order.SitterId == userId;
        var hasApplied = order.Applications.Any(app => app.SitterId == userId);
        if (!isOwner && !isAssignedSitter && !hasApplied)
        {
            return NotFound();
        }

        return Ok(ToDto(order, revealLocation: isOwner || isAssignedSitter,
            applicationStatus: order.Applications.FirstOrDefault(app => app.SitterId == userId)?.Status,
            canContactSupport: isOwner || isAssignedSitter));
    }

    [Authorize(Roles = "sitter,both")]
    [HttpPost("{id:long}/applications")]
    public async Task<IActionResult> Apply(long id)
    {
        if (!TryGetUserId(out var sitterId))
        {
            return Unauthorized();
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var order = await _db.Orders.FirstOrDefaultAsync(item => item.Id == id);
        if (order is null)
        {
            return NotFound("Заказ не найден.");
        }

        if (order.Status != "pending" || order.OwnerId == sitterId)
        {
            return Conflict("Этот заказ больше недоступен для отклика.");
        }

        var application = new OrderApplication { OrderId = id, SitterId = sitterId, Status = "pending" };
        _db.OrderApplications.Add(application);
        try
        {
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception.InnerException))
        {
            return Conflict("Вы уже откликнулись на этот заказ.");
        }

        return Ok(new { orderId = id, application.Status, application.CreatedAt });
    }

    [Authorize(Roles = "owner,both")]
    [HttpGet("{id:long}/applications")]
    public async Task<ActionResult<IReadOnlyList<OrderApplicationDto>>> GetApplications(long id)
    {
        if (!TryGetUserId(out var ownerId))
        {
            return Unauthorized();
        }

        var ownsOrder = await _db.Orders.AnyAsync(order => order.Id == id && order.OwnerId == ownerId);
        if (!ownsOrder)
        {
            return NotFound("Заказ не найден.");
        }

        var applications = await _db.OrderApplications.AsNoTracking()
            .Where(application => application.OrderId == id)
            .Include(application => application.Sitter)
            .OrderBy(application => application.CreatedAt)
            .Select(application => new OrderApplicationDto(
                application.SitterId,
                application.Sitter.Name,
                application.Sitter.RatingAsSitter,
                application.Sitter.ReviewsAsSitter,
                application.Status,
                application.CreatedAt))
            .ToListAsync();

        return Ok(applications);
    }

    [Authorize(Roles = "owner,both")]
    [HttpPut("{id:long}/assign/{sitterId:long}")]
    public async Task<IActionResult> Assign(long id, long sitterId)
    {
        if (!TryGetUserId(out var ownerId))
        {
            return Unauthorized();
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var order = await _db.Orders.FirstOrDefaultAsync(item => item.Id == id && item.OwnerId == ownerId);
        if (order is null)
        {
            return NotFound("Заказ не найден.");
        }

        if (order.Status != "pending")
        {
            return Conflict("Заказ уже назначен или недоступен.");
        }

        var hasApplication = await _db.OrderApplications.AnyAsync(application =>
            application.OrderId == id && application.SitterId == sitterId && application.Status == "pending");
        if (!hasApplication)
        {
            return BadRequest("У исполнителя нет активного отклика на заказ.");
        }

        order.SitterId = sitterId;
        order.Status = "accepted";
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _db.OrderApplications
            .Where(application => application.OrderId == id && application.Status == "pending")
            .ExecuteUpdateAsync(update => update.SetProperty(
                application => application.Status,
                application => application.SitterId == sitterId ? "accepted" : "rejected"));

        await transaction.CommitAsync();
        return NoContent();
    }

    [Authorize(Roles = "sitter,both")]
    [HttpPut("{id:long}/start")]
    public async Task<IActionResult> Start(long id)
    {
        if (!TryGetUserId(out var sitterId))
        {
            return Unauthorized();
        }

        var order = await _db.Orders
            .Include(item => item.WalkingDetails)
            .Include(item => item.BoardingDetails)
            .FirstOrDefaultAsync(item => item.Id == id && item.SitterId == sitterId && item.Status == "accepted");
        if (order is null)
        {
            return Conflict("Начать можно только назначенный вам заказ.");
        }

        order.Status = "in_progress";
        order.UpdatedAt = DateTime.UtcNow;
        if (order.WalkingDetails is not null)
        {
            order.WalkingDetails.StartedAt = DateTime.UtcNow;
        }
        if (order.BoardingDetails is not null)
        {
            order.BoardingDetails.ActualCheckIn = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "sitter,both")]
    [HttpPut("{id:long}/complete")]
    public async Task<IActionResult> Complete(long id, [FromBody] CompleteOrderRequest request)
    {
        if (!TryGetUserId(out var sitterId))
        {
            return Unauthorized();
        }

        var order = await _db.Orders
            .Include(item => item.WalkingDetails)
            .Include(item => item.BoardingDetails)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (order is null)
        {
            return NotFound("Заказ не найден.");
        }

        if (order.SitterId != sitterId)
        {
            return Forbid();
        }

        if (order.Status != "in_progress")
        {
            return Conflict("Завершить можно только заказ, который уже выполняется.");
        }

        order.Status = "completed";
        order.ReportText = request.ReportText?.Trim();
        order.UpdatedAt = DateTime.UtcNow;
        if (order.WalkingDetails is not null)
        {
            order.WalkingDetails.FinishedAt = DateTime.UtcNow;
        }
        if (order.BoardingDetails is not null)
        {
            order.BoardingDetails.ActualCheckOut = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    private IQueryable<Order> LoadOrderDetails(IQueryable<Order> query) => query
        .Include(order => order.Pet)
        .Include(order => order.WalkingDetails)
        .Include(order => order.BoardingDetails)
        .Include(order => order.Applications);

    private static OrderDto ToDto(Order order, bool revealLocation, string? applicationStatus, bool canContactSupport)
    {
        var startAt = order.WalkingDetails?.StartTime ?? order.BoardingDetails?.CheckInAt ?? order.CreatedAt;
        DateTime? endAt = order.WalkingDetails is not null
            ? order.WalkingDetails.StartTime.AddMinutes(order.WalkingDetails.DurationMin)
            : order.BoardingDetails?.CheckOutAt;

        return new OrderDto(
            order.Id,
            order.PublicId,
            order.ServiceType,
            order.Status,
            startAt,
            endAt,
            order.WalkingDetails?.DurationMin,
            order.Area,
            revealLocation ? order.LocationAddress : null,
            order.Description,
            order.TotalPrice,
            order.Applications.Count(application => application.Status == "pending"),
            applicationStatus,
            canContactSupport,
            order.Pet is null ? null : new PetSummaryDto(
                order.Pet.Id,
                order.Pet.Name,
                order.Pet.Breed,
                CalculateAge(order.Pet.BirthDate)));
    }

    private static int? CalculateAge(DateOnly? birthDate)
    {
        if (birthDate is null)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - birthDate.Value.Year;
        return birthDate.Value > today.AddYears(-age) ? age - 1 : age;
    }

    private static string? NormalizeService(string serviceType) => serviceType.Trim().ToLowerInvariant() switch
    {
        "walk" or "walking" => "walking",
        "boarding" => "boarding",
        _ => null
    };

    private bool TryGetUserId(out long userId) => long.TryParse(User.FindFirstValue("uid"), out userId);

    private static bool IsUniqueViolation(Exception? exception) =>
        exception is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    public sealed class CreateOrderRequest
    {
        [Range(1, long.MaxValue)]
        public long PetId { get; set; }

        [Required, StringLength(20)]
        public string ServiceType { get; set; } = string.Empty;

        public DateTimeOffset StartAt { get; set; }
        public DateTimeOffset? EndAt { get; set; }

        [Range(1, 1440)]
        public int? DurationMinutes { get; set; }

        [Required, StringLength(120)]
        public string Area { get; set; } = string.Empty;

        [Required, StringLength(300)]
        public string LocationAddress { get; set; } = string.Empty;

        [Required, StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Range(typeof(decimal), "0", "10000000")]
        public decimal? TotalPrice { get; set; }
    }

    public sealed class CompleteOrderRequest
    {
        [StringLength(2000)]
        public string? ReportText { get; set; }
    }

    public sealed record PetSummaryDto(long Id, string Name, string? Breed, int? Age);

    public sealed record OrderDto(
        long Id,
        Guid PublicId,
        string ServiceType,
        string Status,
        DateTime StartAt,
        DateTime? EndAt,
        int? DurationMinutes,
        string Area,
        string? LocationAddress,
        string Description,
        decimal? TotalPrice,
        int ApplicationCount,
        string? ApplicationStatus,
        bool CanContactSupport,
        PetSummaryDto? Pet);

    public sealed record OrderApplicationDto(
        long SitterId,
        string SitterName,
        decimal Rating,
        int ReviewCount,
        string Status,
        DateTime CreatedAt);
}

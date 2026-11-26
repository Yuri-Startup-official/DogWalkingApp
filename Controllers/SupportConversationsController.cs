using System.Security.Claims;
using DogWalkingApp.Api.Data;
using DogWalkingApp.Api.Dtos;
using DogWalkingApp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace DogWalkingApp.Api.Controllers;

[ApiController]
[Route("api/support/conversations")]
[Authorize(Roles = "owner,sitter,both,support")]
public sealed class SupportConversationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SupportConversationsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SupportConversationDto>>> List(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var query = _db.SupportConversations.AsNoTracking();
        query = User.IsInRole("support")
            ? query.Where(item => item.AssignedManagerId == null || item.AssignedManagerId == userId)
            : query.Where(item => item.RequesterId == userId);

        var conversations = await query
            .OrderByDescending(item => item.UpdatedAt)
            .Take(100)
            .Select(item => new SupportConversationDto(
                item.Id,
                item.Topic,
                item.Status,
                item.OrderId,
                item.Requester.Name,
                item.Requester.Role,
                item.AssignedManagerId,
                item.AssignedManager == null ? null : item.AssignedManager.Name,
                item.Messages.OrderByDescending(message => message.Id).Select(message => message.Body).FirstOrDefault(),
                item.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Ok(conversations);
    }

    [HttpPost]
    [EnableRateLimiting("support")]
    public async Task<ActionResult<SupportConversationDto>> Create(
        [FromBody] CreateSupportConversationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (User.IsInRole("support")) return Forbid();

        var topic = request.Topic.Trim().ToLowerInvariant();
        if (topic is not ("order" or "account" or "other"))
            return BadRequest("Тема должна быть order, account или other.");

        var body = request.Message.Trim();
        if (body.Length == 0) return BadRequest("Напишите первое сообщение.");

        if (request.OrderId is not null)
        {
            var canReferenceOrder = await _db.Orders.AnyAsync(order =>
                order.Id == request.OrderId && (order.OwnerId == userId || order.SitterId == userId), cancellationToken);
            if (!canReferenceOrder) return NotFound("Заказ не найден.");
        }

        var now = DateTime.UtcNow;
        var conversation = new SupportConversation
        {
            RequesterId = userId,
            OrderId = request.OrderId,
            Topic = topic,
            Status = "open",
            CreatedAt = now,
            UpdatedAt = now
        };
        conversation.Messages.Add(new SupportMessage
        {
            SenderId = userId,
            Body = body,
            CreatedAt = now
        });

        _db.SupportConversations.Add(conversation);
        await _db.SaveChangesAsync(cancellationToken);

        var requester = await _db.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new { user.Name, user.Role })
            .SingleAsync(cancellationToken);

        var result = new SupportConversationDto(
            conversation.Id, conversation.Topic, conversation.Status, conversation.OrderId,
            requester.Name, requester.Role, null, null, body, conversation.UpdatedAt);
        return CreatedAtAction(nameof(List), new { }, result);
    }

    [HttpGet("{id:long}/messages")]
    public async Task<ActionResult<IReadOnlyList<SupportMessageDto>>> GetMessages(long id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var conversation = await FindAccessibleConversation(id, userId, cancellationToken);
        if (conversation is null) return NotFound();

        var messages = await _db.SupportMessages.AsNoTracking()
            .Where(message => message.ConversationId == id)
            .OrderByDescending(message => message.Id)
            .Take(100)
            .OrderBy(message => message.Id)
            .Select(message => new SupportMessageDto(
                message.Id,
                message.SenderId,
                message.Sender.Name,
                message.Sender.Role,
                message.Body,
                message.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(messages);
    }

    [HttpPost("{id:long}/messages")]
    [EnableRateLimiting("support")]
    public async Task<IActionResult> SendMessage(
        long id,
        [FromBody] SendSupportMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var conversation = await FindAccessibleConversation(id, userId, cancellationToken);
        if (conversation is null) return NotFound();
        if (User.IsInRole("support") && conversation.AssignedManagerId != userId)
            return Conflict("Сначала назначьте этот диалог себе.");

        var body = request.Body.Trim();
        if (body.Length == 0) return BadRequest("Сообщение не может быть пустым.");

        var now = DateTime.UtcNow;
        _db.SupportMessages.Add(new SupportMessage
        {
            ConversationId = conversation.Id,
            SenderId = userId,
            Body = body,
            CreatedAt = now
        });
        conversation.UpdatedAt = now;
        if (User.IsInRole("support"))
        {
            conversation.Status = "waiting";
            conversation.ResolvedAt = null;
        }
        else
        {
            conversation.Status = "open";
            conversation.ResolvedAt = null;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { conversation.Id, conversation.Status, conversation.UpdatedAt });
    }

    [HttpPut("{id:long}/assignment")]
    [Authorize(Roles = "support")]
    public async Task<IActionResult> AssignToMe(long id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var now = DateTime.UtcNow;
        var updated = await _db.SupportConversations
            .Where(item => item.Id == id && (item.AssignedManagerId == null || item.AssignedManagerId == userId))
            .ExecuteUpdateAsync(update => update
                .SetProperty(item => item.AssignedManagerId, (long?)userId)
                .SetProperty(item => item.UpdatedAt, now), cancellationToken);
        if (updated == 0)
        {
            var exists = await _db.SupportConversations.AnyAsync(item => item.Id == id, cancellationToken);
            if (!exists) return NotFound();
            return Conflict("Диалог уже назначен другому менеджеру.");
        }

        return NoContent();
    }

    [HttpPut("{id:long}/status")]
    [Authorize(Roles = "support")]
    public async Task<IActionResult> UpdateStatus(
        long id,
        [FromBody] UpdateSupportStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var conversation = await _db.SupportConversations
            .FirstOrDefaultAsync(item => item.Id == id && item.AssignedManagerId == userId, cancellationToken);
        if (conversation is null) return NotFound();

        var status = request.Status.Trim().ToLowerInvariant();
        if (status is not ("open" or "waiting" or "resolved"))
            return BadRequest("Допустимые статусы: open, waiting, resolved.");

        conversation.Status = status;
        conversation.ResolvedAt = status == "resolved" ? DateTime.UtcNow : null;
        conversation.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private Task<SupportConversation?> FindAccessibleConversation(long id, long userId, CancellationToken cancellationToken)
    {
        var query = _db.SupportConversations.Where(item => item.Id == id);
        query = User.IsInRole("support")
            ? query.Where(item => item.AssignedManagerId == null || item.AssignedManagerId == userId)
            : query.Where(item => item.RequesterId == userId);
        return query.FirstOrDefaultAsync(cancellationToken);
    }

    private bool TryGetUserId(out long userId) => long.TryParse(User.FindFirstValue("uid"), out userId);
}

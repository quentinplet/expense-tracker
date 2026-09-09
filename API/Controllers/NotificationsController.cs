using API.DTOs.Requests;
using API.DTOs.Responses;
using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
public class NotificationsController(IUnitOfWork uow, INotificationService notificationService) : BaseApiController
{
    /// GET /api/notifications?take=20 — un seul appel, compteur non lues + les
    /// `take` plus récentes (lues et non lues confondues), comme GET /api/budgets.
    [HttpGet]
    public async Task<ActionResult<NotificationsResponseDto>> GetAll([FromQuery] NotificationQueryDto query)
    {
        var userId = User.GetMemberId();
        return Ok(await notificationService.GetForUserAsync(userId, query.Take));
    }

    [HttpPut("{id}/read")]
    public async Task<ActionResult> MarkAsRead(Guid id)
    {
        var userId = User.GetMemberId();
        var notification = await uow.NotificationRepository.GetByIdAsync(id);
        if (notification == null) return NotFound();
        if (notification.UserId != userId) return Forbid();

        notification.IsRead = true;
        uow.NotificationRepository.Update(notification);
        if (await uow.Complete()) return NoContent();
        return BadRequest("Failed to mark notification as read");
    }

    [HttpPut("read-all")]
    public async Task<ActionResult> MarkAllAsRead()
    {
        var userId = User.GetMemberId();
        var unread = await uow.NotificationRepository.GetUnreadAsync(userId);
        foreach (var notification in unread)
        {
            notification.IsRead = true;
            uow.NotificationRepository.Update(notification);
        }

        await uow.Complete();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var userId = User.GetMemberId();
        var notification = await uow.NotificationRepository.GetByIdAsync(id);
        if (notification == null) return NotFound();
        if (notification.UserId != userId) return Forbid();

        uow.NotificationRepository.Delete(notification);
        if (await uow.Complete()) return NoContent();
        return BadRequest("Failed to delete notification");
    }
}

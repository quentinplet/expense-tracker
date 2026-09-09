using API.Entities;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data.Repositories;

public class NotificationRepository(AppDbContext context) : INotificationRepository
{
    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task<List<Notification>> GetRecentAsync(Guid userId, int take)
    {
        return await context.Notifications
            .Include(n => n.Transaction)
            .Include(n => n.Budget).ThenInclude(b => b!.Category)
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<Notification?> GetByIdAsync(Guid id)
    {
        return await context.Notifications.FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task<List<Notification>> GetUnreadAsync(Guid userId)
    {
        return await context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();
    }

    public async Task<bool> ThresholdNotificationExistsAsync(Guid budgetId, int thresholdPercent)
    {
        return await context.Notifications.AnyAsync(n =>
            n.BudgetId == budgetId && n.ThresholdPercent == thresholdPercent);
    }

    public void Add(Notification notification)
    {
        context.Notifications.Add(notification);
    }

    public void Update(Notification notification)
    {
        context.Notifications.Update(notification);
    }

    public void Delete(Notification notification)
    {
        context.Notifications.Remove(notification);
    }
}

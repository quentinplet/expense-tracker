using System;
using API.DTOs.Responses;
using API.Entities;

namespace API.Extensions;

public static class NotificationExtension
{
    public static NotificationDto ToNotificationDto(this Notification n) => new(
        n.Id, n.Type, n.IsRead, n.CreatedAt,
        n.TransactionId, n.Transaction?.Label, n.Transaction?.Amount,
        n.BudgetId, n.Budget?.Category?.Name, n.Budget?.Category?.TranslationKey,
        n.Budget == null ? null : n.Budget.CategoryId == null, n.Budget?.Month, n.ThresholdPercent);
}

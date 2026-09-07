using System;
using System.Text.Json.Serialization;
using API.Entities;

namespace API.DTOs.Responses;

/// Données structurées uniquement — le backend ne construit jamais le texte
/// affiché, c'est le frontend qui choisit la clé i18n et interpole (même règle
/// que CategorySummaryDto). Tous les champs Transaction* sont null sauf pour
/// Type == RecurringTransactionGenerated ; tous les champs Budget*/ThresholdPercent
/// sont null sauf pour Type == BudgetThresholdReached, où BudgetCategoryName/
/// BudgetCategoryTranslationKey restent en plus null si BudgetIsGlobal est true
/// (même convention que BudgetResponseDto pour un budget global).
public record NotificationDto(
    Guid Id,
    [property: JsonConverter(typeof(JsonStringEnumConverter<NotificationType>))]
    NotificationType Type,
    bool IsRead,
    DateTime CreatedAt,

    Guid? TransactionId, string? TransactionLabel, decimal? TransactionAmount,

    Guid? BudgetId, string? BudgetCategoryName, string? BudgetCategoryTranslationKey,
    bool? BudgetIsGlobal, string? BudgetMonth, int? ThresholdPercent);

public record NotificationsResponseDto(int UnreadCount, IReadOnlyList<NotificationDto> Items);

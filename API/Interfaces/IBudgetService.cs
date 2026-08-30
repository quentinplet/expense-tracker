using System;
using API.DTOs.Responses;
using API.Entities;

namespace API.Interfaces;

public interface IBudgetService
{
    /// Le budget global d'abord, s'il existe, puis les budgets catégorie — Spent et
    /// Remaining calculés en un seul aller-retour sur les transactions du mois.
    Task<List<BudgetResponseDto>> GetBudgetsForMonthAsync(Guid userId, string month);

    /// Dépensé d'un seul budget (global ou catégorie) sur son mois. Pour les
    /// endpoints qui manipulent un budget à la fois (GetById/Create/Update) — la
    /// contrainte « un seul aller-retour » de GetBudgetsForMonthAsync ne s'y
    /// applique pas, il n'y a qu'un budget à résoudre.
    Task<decimal> GetSpentAsync(Guid userId, Budget budget);

    /// Duplique tout budget AutoRenew du mois courant vers le mois suivant, tous
    /// utilisateurs confondus, si la ligne suivante n'existe pas déjà. Appelé par
    /// BudgetAutoRenewJob ; retourne le nombre de lignes créées.
    Task<int> RenewBudgetsAsync();
}
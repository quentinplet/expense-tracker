using System;
using API.DTOs.Responses;
using API.Entities;
using API.Extensions;
using API.Interfaces;

namespace API.Services;

public class DashboardService(IDashboardRepository repository) : IDashboardService
{
    private const int TrendMonths = 12;
    private const int RecentCount = 5;

    public async Task<DashboardResponseDto> GetDashboardDataAsync(Guid userId, int year, int month)
    {
        var current = new DateOnly(year, month, 1);
        var next = current.AddMonths(1);
        var previous = current.AddMonths(-1);

        // Fenêtre de 13 mois : les 12 points de la courbe, plus le mois précédent
        // du mois affiché, nécessaire à la comparaison.
        var windowStart = current.AddMonths(-(TrendMonths - 1));
        var queryStart = windowStart < previous ? windowStart : previous;

        var monthlyTotals = await repository.GetMonthlyTotalsAsync(userId, queryStart, next);
        var breakdown = await repository.GetExpenseBreakdownAsync(userId, current, next);
        var recent = await repository.GetRecentAsync(userId, current, next, RecentCount);

        var totals = BuildTotals(monthlyTotals, current, previous);

        return new DashboardResponseDto(
            Month: Format(current),
            Totals: totals,
            Breakdown: BuildBreakdown(breakdown, totals.Expenses),
            Trend: BuildTrend(monthlyTotals, windowStart),
            RecentTransactions: [.. recent.Select(t => t.ToTransactionResponseDto())]);
    }

    private static MonthTotalsDto BuildTotals(
        IReadOnlyList<MonthlyTotalProjection> totals, DateOnly current, DateOnly previous)
    {
        var expenses = Sum(totals, current, TransactionType.Expense);
        var income = Sum(totals, current, TransactionType.Income);
        var previousExpenses = Sum(totals, previous, TransactionType.Expense);
        var previousIncome = Sum(totals, previous, TransactionType.Income);

        return new MonthTotalsDto(
            Expenses: expenses,
            Income: income,
            Net: income - expenses,
            PreviousExpenses: previousExpenses,
            PreviousIncome: previousIncome,
            PreviousNet: previousIncome - previousExpenses);
    }

    private static decimal Sum(
        IReadOnlyList<MonthlyTotalProjection> totals, DateOnly month, TransactionType type)
    {
        return totals
            .Where(t => t.Year == month.Year && t.Month == month.Month && t.Type == type)
            .Sum(t => t.Total);
    }

    private static List<CategoryBreakdownDto> BuildBreakdown(
        IReadOnlyList<CategoryTotalProjection> breakdown, decimal monthExpenses)
    {
        return
        [
            .. breakdown.Select(c => new CategoryBreakdownDto(
                c.CategoryId,
                c.Name,
                c.TranslationKey,
                c.Color,
                c.Icon,
                c.Total,
                // Un mois sans dépense doit donner 0, jamais une division par zéro.
                monthExpenses == 0 ? 0 : c.Total / monthExpenses)),
        ];
    }

    /// <summary>
    /// Les mois sans transaction sont absents du GroupBy. Sans remplissage explicite,
    /// la courbe décalerait silencieusement ses points sur les mauvaises étiquettes.
    /// </summary>
    private static List<MonthlyPointDto> BuildTrend(
        IReadOnlyList<MonthlyTotalProjection> totals, DateOnly windowStart)
    {
        var points = new List<MonthlyPointDto>(TrendMonths);

        for (var i = 0; i < TrendMonths; i++)
        {
            var month = windowStart.AddMonths(i);
            points.Add(new MonthlyPointDto(
                Format(month),
                Sum(totals, month, TransactionType.Expense),
                Sum(totals, month, TransactionType.Income)));
        }

        return points;
    }

    private static string Format(DateOnly month) => month.ToString("yyyy-MM");
}
using System;
using API.DTOs.Requests;
using API.DTOs.Responses;
using API.Entities;
using API.Extensions;
using API.Interfaces;

namespace API.Services;

public class DashboardService(IDashboardRepository repository) : IDashboardService
{
    /// <summary>Longueur minimale de la série mensuelle.</summary>
    private const int TrendMonths = 12;

    /// <summary>Longueur maximale de la série, soit dix ans.</summary>
    /// <remarks>
    /// La courbe est bornée par les données, que rien ne valide : une transaction
    /// saisie en 1900 produirait sinon quinze cents points, et une réponse de
    /// plusieurs mégaoctets pour une requête d'une ligne.
    /// </remarks>
    private const int MaxTrendMonths = 120;

    private const int RecentCount = 5;

    public async Task<DashboardResponseDto> GetDashboardDataAsync(Guid userId, DashboardRequestDto request)
    {
        // Le mois affiché borne la répartition et la courbe journalière ; la courbe
        // mensuelle, elle, court jusqu'au mois courant quel que soit ce choix.
        var anchor = request.ResolveMonth();

        var next = anchor.AddMonths(1);
        var previous = anchor.AddMonths(-1);

        var monthlyTotals = await repository.GetMonthlyTotalsAsync(userId);
        var dailyTotals = await repository.GetDailyTotalsAsync(userId, anchor, next);
        var overall = await repository.GetOverallTotalsAsync(userId);

        // Douze derniers mois glissants, ancrés sur le mois courant réel — comme
        // BuildTrend, indépendant du mois affiché par le picker. `YearExpenses` est
        // la somme de cette même répartition, pas overall.Expenses (qui reste
        // volontairement sans borne, réservé à CumulativeNet).
        var currentMonth = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var yearFrom = currentMonth.AddMonths(-(TrendMonths - 1));
        var yearToExclusive = currentMonth.AddMonths(1);

        var breakdown = await repository.GetExpenseBreakdownAsync(userId, anchor, next);
        var breakdownYear = await repository.GetExpenseBreakdownAsync(userId, yearFrom, yearToExclusive);
        var yearExpenses = breakdownYear.Sum(c => c.Total);

        // Décorrélé de la période : « récentes » veut dire récentes.
        var recent = await repository.GetRecentAsync(userId, RecentCount);

        var totals = BuildMonthTotals(monthlyTotals, anchor, previous);

        return new DashboardResponseDto(
            Month: request.Month,
            Totals: totals,
            CumulativeNet: overall.Income - overall.Expenses,
            Breakdown: BuildBreakdown(breakdown, totals.Expenses),
            BreakdownYear: BuildBreakdown(breakdownYear, yearExpenses),
            YearExpenses: yearExpenses,
            DailyTrend: BuildDailyTrend(dailyTotals, anchor, next),
            Trend: BuildTrend(monthlyTotals),
            RecentTransactions: [.. recent.Select(t => t.ToTransactionResponseDto())]);
    }

    private static MonthTotalsDto BuildMonthTotals(
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
        IReadOnlyList<CategoryTotalProjection> breakdown, decimal periodExpenses)
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
                // Une période sans dépense doit donner 0, jamais une division par zéro.
                periodExpenses == 0 ? 0 : c.Total / periodExpenses)),
        ];
    }

    /// <summary>
    /// Un point par jour du mois affiché, du 1er au dernier, y compris les jours sans
    /// transaction : une courbe qui saute les jours vides écrase l'axe du temps et
    /// laisse croire à une dépense continue là où il n'y a eu que deux achats.
    /// </summary>
    private static List<DailyPointDto> BuildDailyTrend(
        IReadOnlyList<DailyTotalProjection> totals, DateOnly from, DateOnly toExclusive)
    {
        var points = new List<DailyPointDto>();

        for (var day = from; day < toExclusive; day = day.AddDays(1))
        {
            points.Add(new DailyPointDto(
                day.ToString("yyyy-MM-dd"),
                SumDay(totals, day, TransactionType.Expense),
                SumDay(totals, day, TransactionType.Income)));
        }

        return points;
    }

    private static decimal SumDay(
        IReadOnlyList<DailyTotalProjection> totals, DateOnly day, TransactionType type)
    {
        return totals.Where(t => t.Date == day && t.Type == type).Sum(t => t.Total);
    }

    /// <summary>
    /// Série mensuelle sur tout l'historique, volontairement indépendante du mois
    /// affiché : elle se lit jusqu'au mois courant même quand le picker est sur un
    /// mois passé, sinon reculer d'un mois amputerait la courbe de tout ce qui suit.
    /// </summary>
    /// <remarks>
    /// Le mois courant est ici lu en UTC, alors que le mois affiché vient toujours du
    /// client. La nuance de fuseau ne coûte ici qu'un point à zéro de plus ou de moins
    /// au bord droit pendant quelques heures, là où elle changerait les chiffres
    /// affichés s'il s'agissait de choisir un mois.
    ///
    /// Les mois sans transaction sont absents du GroupBy. Sans remplissage explicite,
    /// la courbe décalerait silencieusement ses points sur les mauvaises étiquettes.
    /// </remarks>
    private static List<MonthlyPointDto> BuildTrend(IReadOnlyList<MonthlyTotalProjection> totals)
    {
        var currentMonth = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        // Douze mois au minimum, étendus de part et d'autre par les données réelles :
        // l'axe reste stable chez quelqu'un qui vient de commencer, et un mois sans
        // rien enregistré apparaît comme un creux plutôt que d'être escamoté.
        var start = currentMonth.AddMonths(-(TrendMonths - 1));
        var end = currentMonth;

        foreach (var row in totals)
        {
            var month = new DateOnly(row.Year, row.Month, 1);
            if (month < start) start = month;
            if (month > end) end = month;
        }

        var floor = end.AddMonths(-(MaxTrendMonths - 1));
        if (start < floor) start = floor;

        var points = new List<MonthlyPointDto>();
        for (var month = start; month <= end; month = month.AddMonths(1))
        {
            points.Add(new MonthlyPointDto(
                month.ToString("yyyy-MM"),
                Sum(totals, month, TransactionType.Expense),
                Sum(totals, month, TransactionType.Income)));
        }

        return points;
    }
}
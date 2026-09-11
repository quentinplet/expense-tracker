using API.DTOs.Requests;
using API.Entities;
using API.Interfaces;
using API.Services;
using Moq;

namespace API.Tests.Services;

public class DashboardServiceTests
{
    private readonly Guid _userId = Guid.NewGuid();

    /// Setups par défaut couvrant les cinq appels de repository que
    /// GetDashboardDataAsync fait systématiquement — chaque test ne remplace que
    /// ce qui l'intéresse (Moq retient la dernière Setup correspondante).
    private Mock<IDashboardRepository> CreateRepoWithDefaults()
    {
        var repo = new Mock<IDashboardRepository>();
        repo.Setup(r => r.GetMonthlyTotalsAsync(_userId))
            .ReturnsAsync((IReadOnlyList<MonthlyTotalProjection>)[]);
        repo.Setup(r => r.GetDailyTotalsAsync(_userId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync((IReadOnlyList<DailyTotalProjection>)[]);
        repo.Setup(r => r.GetOverallTotalsAsync(_userId))
            .ReturnsAsync(new OverallTotalsProjection(0, 0));
        repo.Setup(r => r.GetExpenseBreakdownAsync(_userId, It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>()))
            .ReturnsAsync((IReadOnlyList<CategoryTotalProjection>)[]);
        repo.Setup(r => r.GetRecentAsync(_userId, It.IsAny<int>()))
            .ReturnsAsync((IReadOnlyList<Transaction>)[]);
        return repo;
    }

    private static DashboardRequestDto MakeRequest(string month = "2026-09") => new() { Month = month };

    [Fact]
    public async Task GetDashboardDataAsync_EchoesRequestedMonth()
    {
        var sut = new DashboardService(CreateRepoWithDefaults().Object);

        var result = await sut.GetDashboardDataAsync(_userId, MakeRequest("2026-02"));

        Assert.Equal("2026-02", result.Month);
    }

    [Fact]
    public async Task GetDashboardDataAsync_CumulativeNet_IsOverallIncomeMinusExpenses()
    {
        var repo = CreateRepoWithDefaults();
        repo.Setup(r => r.GetOverallTotalsAsync(_userId)).ReturnsAsync(new OverallTotalsProjection(500, 300));
        var sut = new DashboardService(repo.Object);

        var result = await sut.GetDashboardDataAsync(_userId, MakeRequest());

        Assert.Equal(200, result.CumulativeNet);
    }

    [Fact]
    public async Task GetDashboardDataAsync_MonthTotals_SumsCurrentAndPreviousMonthSeparately()
    {
        var repo = CreateRepoWithDefaults();
        repo.Setup(r => r.GetMonthlyTotalsAsync(_userId)).ReturnsAsync(
        [
            new MonthlyTotalProjection(2026, 9, TransactionType.Expense, 300),
            new MonthlyTotalProjection(2026, 9, TransactionType.Income, 1000),
            new MonthlyTotalProjection(2026, 8, TransactionType.Expense, 250),
            new MonthlyTotalProjection(2026, 8, TransactionType.Income, 900),
            new MonthlyTotalProjection(2026, 7, TransactionType.Expense, 999), // ni courant ni précédent
        ]);
        var sut = new DashboardService(repo.Object);

        var result = await sut.GetDashboardDataAsync(_userId, MakeRequest("2026-09"));

        Assert.Equal(300, result.Totals.Expenses);
        Assert.Equal(1000, result.Totals.Income);
        Assert.Equal(700, result.Totals.Net);
        Assert.Equal(250, result.Totals.PreviousExpenses);
        Assert.Equal(900, result.Totals.PreviousIncome);
        Assert.Equal(650, result.Totals.PreviousNet);
    }

    [Fact]
    public async Task GetDashboardDataAsync_Breakdown_ShareIsZeroWhenPeriodHasNoExpenses()
    {
        // Une période sans dépense doit donner 0, jamais une division par zéro.
        var repo = CreateRepoWithDefaults();
        repo.Setup(r => r.GetExpenseBreakdownAsync(_userId, new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 1)))
            .ReturnsAsync([new CategoryTotalProjection(Guid.NewGuid(), "Groceries", null, "#111", "pi-cart", 50)]);
        var sut = new DashboardService(repo.Object);

        var result = await sut.GetDashboardDataAsync(_userId, MakeRequest("2026-09"));

        Assert.Equal(0, result.Breakdown.Single().Share);
    }

    [Fact]
    public async Task GetDashboardDataAsync_Breakdown_ShareIsFractionOfPeriodExpenses()
    {
        var repo = CreateRepoWithDefaults();
        repo.Setup(r => r.GetMonthlyTotalsAsync(_userId)).ReturnsAsync(
            [new MonthlyTotalProjection(2026, 9, TransactionType.Expense, 200)]);
        repo.Setup(r => r.GetExpenseBreakdownAsync(_userId, new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 1)))
            .ReturnsAsync([new CategoryTotalProjection(Guid.NewGuid(), "Groceries", null, "#111", "pi-cart", 50)]);
        var sut = new DashboardService(repo.Object);

        var result = await sut.GetDashboardDataAsync(_userId, MakeRequest("2026-09"));

        Assert.Equal(0.25m, result.Breakdown.Single().Share);
    }

    [Fact]
    public async Task GetDashboardDataAsync_BreakdownAllTime_SharesUseAllTimeExpensesAsDenominator()
    {
        // §BuildBreakdown : appelé une seconde fois pour BreakdownAllTime, avec
        // overall.Expenses comme dénominateur — indépendant des dépenses de la
        // période affichée.
        var repo = CreateRepoWithDefaults();
        repo.Setup(r => r.GetOverallTotalsAsync(_userId)).ReturnsAsync(new OverallTotalsProjection(0, 400));
        repo.Setup(r => r.GetExpenseBreakdownAsync(_userId, null, null))
            .ReturnsAsync([new CategoryTotalProjection(Guid.NewGuid(), "Groceries", null, "#111", "pi-cart", 100)]);
        var sut = new DashboardService(repo.Object);

        var result = await sut.GetDashboardDataAsync(_userId, MakeRequest("2026-09"));

        Assert.Equal(400, result.AllTimeExpenses);
        Assert.Equal(0.25m, result.BreakdownAllTime.Single().Share);
    }

    [Fact]
    public async Task GetDashboardDataAsync_DailyTrend_HasOnePointPerDayOfMonthIncludingZeros()
    {
        // Un point par jour du mois affiché, y compris les jours sans transaction
        // (§BuildDailyTrend) — septembre 2026 compte 30 jours.
        var repo = CreateRepoWithDefaults();
        repo.Setup(r => r.GetDailyTotalsAsync(_userId, new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 1)))
            .ReturnsAsync([new DailyTotalProjection(new DateOnly(2026, 9, 15), TransactionType.Expense, 42)]);
        var sut = new DashboardService(repo.Object);

        var result = await sut.GetDashboardDataAsync(_userId, MakeRequest("2026-09"));

        Assert.Equal(30, result.DailyTrend.Count);
        Assert.Equal("2026-09-01", result.DailyTrend[0].Date);
        Assert.Equal(0, result.DailyTrend[0].Expenses);
        Assert.Equal(42, result.DailyTrend.Single(d => d.Date == "2026-09-15").Expenses);
    }

    [Fact]
    public async Task GetDashboardDataAsync_Trend_DefaultsToTwelveTrailingMonthsWhenNoHistory()
    {
        var sut = new DashboardService(CreateRepoWithDefaults().Object);
        var currentMonth = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        var result = await sut.GetDashboardDataAsync(_userId, MakeRequest("2026-09"));

        Assert.Equal(12, result.Trend.Count);
        Assert.Equal(currentMonth.AddMonths(-11).ToString("yyyy-MM"), result.Trend[0].Month);
        Assert.Equal(currentMonth.ToString("yyyy-MM"), result.Trend[^1].Month);
    }

    [Fact]
    public async Task GetDashboardDataAsync_Trend_IgnoresDisplayedMonthAndTracksCurrentUtcMonth()
    {
        // §BuildTrend : la courbe mensuelle court jusqu'au mois courant quel que
        // soit le mois affiché — reculer le picker ne l'ampute pas.
        var sut = new DashboardService(CreateRepoWithDefaults().Object);
        var currentMonth = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        var result = await sut.GetDashboardDataAsync(_userId, MakeRequest("2020-01"));

        Assert.Equal(currentMonth.ToString("yyyy-MM"), result.Trend[^1].Month);
    }

    [Fact]
    public async Task GetDashboardDataAsync_Trend_ExtendsBackToOldestTransactionMonth()
    {
        var currentMonth = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var oldMonth = currentMonth.AddMonths(-20);
        var repo = CreateRepoWithDefaults();
        repo.Setup(r => r.GetMonthlyTotalsAsync(_userId)).ReturnsAsync(
            [new MonthlyTotalProjection(oldMonth.Year, oldMonth.Month, TransactionType.Expense, 10)]);
        var sut = new DashboardService(repo.Object);

        var result = await sut.GetDashboardDataAsync(_userId, MakeRequest("2026-09"));

        Assert.Equal(oldMonth.ToString("yyyy-MM"), result.Trend[0].Month);
        Assert.Equal(currentMonth.ToString("yyyy-MM"), result.Trend[^1].Month);
        Assert.Equal(21, result.Trend.Count);
    }

    [Fact]
    public async Task GetDashboardDataAsync_Trend_CapsAtOneHundredTwentyMonthsEvenWithOlderHistory()
    {
        // §MaxTrendMonths : borne la courbe même si une transaction bien plus
        // ancienne existe, pour éviter une réponse de plusieurs mégaoctets.
        var currentMonth = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var ancientMonth = currentMonth.AddMonths(-200);
        var repo = CreateRepoWithDefaults();
        repo.Setup(r => r.GetMonthlyTotalsAsync(_userId)).ReturnsAsync(
            [new MonthlyTotalProjection(ancientMonth.Year, ancientMonth.Month, TransactionType.Expense, 10)]);
        var sut = new DashboardService(repo.Object);

        var result = await sut.GetDashboardDataAsync(_userId, MakeRequest("2026-09"));

        Assert.Equal(120, result.Trend.Count);
        Assert.Equal(currentMonth.AddMonths(-119).ToString("yyyy-MM"), result.Trend[0].Month);
    }

    [Fact]
    public async Task GetDashboardDataAsync_RecentTransactions_MappedRegardlessOfDisplayedMonth()
    {
        // §GetDashboardDataAsync : « récentes » veut dire récentes, décorrélé de la
        // période — vérifié ici avec une transaction bien antérieure au mois affiché.
        var repo = CreateRepoWithDefaults();
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(), Amount = 15, Type = TransactionType.Expense,
            Date = new DateOnly(2020, 1, 1), Label = "Ancien café", UserId = _userId,
        };
        repo.Setup(r => r.GetRecentAsync(_userId, 5)).ReturnsAsync([transaction]);
        var sut = new DashboardService(repo.Object);

        var result = await sut.GetDashboardDataAsync(_userId, MakeRequest("2026-09"));

        var dto = Assert.Single(result.RecentTransactions);
        Assert.Equal(transaction.Id, dto.Id);
        Assert.Equal("Ancien café", dto.Label);
    }
}

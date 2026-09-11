using API.Entities;
using API.Interfaces;
using API.Services;
using API.Tests.TestHelpers;
using Moq;

namespace API.Tests.Services;

public class BudgetServiceTests
{
    private readonly FakeUnitOfWork _uow = new();
    private readonly BudgetService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public BudgetServiceTests()
    {
        _sut = new BudgetService(_uow.Uow);
    }

    [Fact]
    public async Task GetSpentAsync_GlobalBudget_SumsAcrossAllCategories()
    {
        var budget = new Budget { Id = Guid.NewGuid(), AmountLimit = 500, Month = "2026-09", UserId = _userId, CategoryId = null };
        _uow.BudgetRepository
            .Setup(r => r.GetSpentTotalsAsync(_userId, new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 1)))
            .ReturnsAsync(
            [
                new CategorySpentProjection(Guid.NewGuid(), 30m),
                new CategorySpentProjection(Guid.NewGuid(), 70m),
            ]);

        var spent = await _sut.GetSpentAsync(_userId, budget);

        Assert.Equal(100m, spent);
    }

    [Fact]
    public async Task GetSpentAsync_CategoryBudget_FiltersToItsOwnCategory()
    {
        var categoryId = Guid.NewGuid();
        var budget = new Budget { Id = Guid.NewGuid(), AmountLimit = 500, Month = "2026-09", UserId = _userId, CategoryId = categoryId };
        _uow.BudgetRepository
            .Setup(r => r.GetSpentTotalsAsync(_userId, new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 1)))
            .ReturnsAsync(
            [
                new CategorySpentProjection(categoryId, 30m),
                new CategorySpentProjection(Guid.NewGuid(), 70m),
            ]);

        var spent = await _sut.GetSpentAsync(_userId, budget);

        Assert.Equal(30m, spent);
    }

    [Fact]
    public async Task GetBudgetsForMonthAsync_OrdersGlobalBudgetFirst()
    {
        var categoryBudget = new Budget { Id = Guid.NewGuid(), AmountLimit = 100, Month = "2026-09", UserId = _userId, CategoryId = Guid.NewGuid() };
        var globalBudget = new Budget { Id = Guid.NewGuid(), AmountLimit = 500, Month = "2026-09", UserId = _userId, CategoryId = null };
        _uow.BudgetRepository.Setup(r => r.GetAllByUserIdAndMonthAsync(_userId, "2026-09"))
            .ReturnsAsync([categoryBudget, globalBudget]);
        _uow.BudgetRepository
            .Setup(r => r.GetSpentTotalsAsync(_userId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync((IReadOnlyList<CategorySpentProjection>)[]);

        var result = await _sut.GetBudgetsForMonthAsync(_userId, "2026-09");

        Assert.Equal(globalBudget.Id, result[0].Id);
        Assert.Equal(categoryBudget.Id, result[1].Id);
    }

    [Fact]
    public async Task GetBudgetsForMonthAsync_AutoRenewConflict_TrueWhenNextMonthAlreadyExists()
    {
        var categoryId = Guid.NewGuid();
        var budget = new Budget
        {
            Id = Guid.NewGuid(), AmountLimit = 100, Month = "2026-09",
            UserId = _userId, CategoryId = categoryId, AutoRenew = true,
        };
        var nextMonthBudget = new Budget
        {
            Id = Guid.NewGuid(), AmountLimit = 100, Month = "2026-10",
            UserId = _userId, CategoryId = categoryId,
        };
        _uow.BudgetRepository.Setup(r => r.GetAllByUserIdAndMonthAsync(_userId, "2026-09")).ReturnsAsync([budget]);
        _uow.BudgetRepository.Setup(r => r.GetAllByUserIdAndMonthAsync(_userId, "2026-10")).ReturnsAsync([nextMonthBudget]);
        _uow.BudgetRepository
            .Setup(r => r.GetSpentTotalsAsync(_userId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync((IReadOnlyList<CategorySpentProjection>)[]);

        var result = await _sut.GetBudgetsForMonthAsync(_userId, "2026-09");

        Assert.True(result.Single().AutoRenewConflict);
    }

    [Fact]
    public async Task GetBudgetsForMonthAsync_AutoRenewConflict_FalseWhenNoAutoRenewBudgetExists()
    {
        // Optimisation documentée dans BudgetService : si aucun budget du mois n'a
        // AutoRenew, le second aller-retour est sauté entièrement.
        var budget = new Budget { Id = Guid.NewGuid(), AmountLimit = 100, Month = "2026-09", UserId = _userId, AutoRenew = false };
        _uow.BudgetRepository.Setup(r => r.GetAllByUserIdAndMonthAsync(_userId, "2026-09")).ReturnsAsync([budget]);
        _uow.BudgetRepository
            .Setup(r => r.GetSpentTotalsAsync(_userId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync((IReadOnlyList<CategorySpentProjection>)[]);

        var result = await _sut.GetBudgetsForMonthAsync(_userId, "2026-09");

        Assert.False(result.Single().AutoRenewConflict);
        _uow.BudgetRepository.Verify(r => r.GetAllByUserIdAndMonthAsync(_userId, "2026-10"), Times.Never);
    }

    [Fact]
    public async Task RenewBudgetsAsync_SkipsCandidateWhenNextMonthAlreadyExists()
    {
        var candidate = new Budget
        {
            Id = Guid.NewGuid(), AmountLimit = 100, Month = DateTime.UtcNow.ToString("yyyy-MM"),
            UserId = _userId, AutoRenew = true,
        };
        _uow.BudgetRepository.Setup(r => r.GetAutoRenewCandidatesAsync(It.IsAny<string>())).ReturnsAsync([candidate]);
        _uow.BudgetRepository.Setup(r => r.ExistsAsync(_userId, It.IsAny<string>(), candidate.CategoryId, null))
            .ReturnsAsync(true);

        var created = await _sut.RenewBudgetsAsync();

        Assert.Equal(0, created);
        _uow.BudgetRepository.Verify(r => r.Add(It.IsAny<Budget>()), Times.Never);
    }

    [Fact]
    public async Task RenewBudgetsAsync_CreatesMissingNextMonthBudget()
    {
        var candidate = new Budget
        {
            Id = Guid.NewGuid(), AmountLimit = 100, Month = DateTime.UtcNow.ToString("yyyy-MM"),
            UserId = _userId, AutoRenew = true, CategoryId = Guid.NewGuid(),
        };
        _uow.BudgetRepository.Setup(r => r.GetAutoRenewCandidatesAsync(It.IsAny<string>())).ReturnsAsync([candidate]);
        _uow.BudgetRepository.Setup(r => r.ExistsAsync(_userId, It.IsAny<string>(), candidate.CategoryId, null))
            .ReturnsAsync(false);

        var created = await _sut.RenewBudgetsAsync();

        Assert.Equal(1, created);
        _uow.BudgetRepository.Verify(r => r.Add(It.Is<Budget>(b =>
            b.UserId == _userId && b.CategoryId == candidate.CategoryId && b.AmountLimit == 100)), Times.Once);
    }
}

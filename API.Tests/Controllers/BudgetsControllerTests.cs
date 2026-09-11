using API.Controllers;
using API.DTOs.Requests;
using API.Entities;
using API.Interfaces;
using API.Services;
using API.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers;

public class BudgetsControllerTests
{
    private readonly FakeUnitOfWork _uow = new();
    private readonly BudgetsController _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public BudgetsControllerTests()
    {
        // Le vrai BudgetService, pas un mock : il n'a lui-même que IUnitOfWork
        // comme dépendance, déjà couvert par FakeUnitOfWork. Le comportement de
        // GetSpentAsync/GetBudgetsForMonthAsync est aussi couvert isolément dans
        // BudgetServiceTests.
        var service = new BudgetService(_uow.Uow);
        _sut = new BudgetsController(_uow.Uow, service);
        _sut.SetUser(_userId);
    }

    private static Category MakeCategory(Guid userId, TransactionType type) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Groceries",
        Icon = "pi-cart",
        Color = "#3b82f6",
        Type = type,
        UserId = userId,
    };

    private static BudgetRequestDto MakeDto(Guid? categoryId, string month = "2026-09", decimal amount = 100) => new()
    {
        CategoryId = categoryId,
        Month = month,
        AmountLimit = amount,
        AutoRenew = false,
    };

    [Fact]
    public async Task CreateBudget_CategoryDoesNotExist_ReturnsNotFound()
    {
        _uow.CategoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Category?)null);

        var result = await _sut.CreateBudget(MakeDto(Guid.NewGuid()));

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateBudget_CategoryBelongsToAnotherUser_ReturnsForbid()
    {
        var category = MakeCategory(Guid.NewGuid(), TransactionType.Expense);
        _uow.CategoryRepository.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);

        var result = await _sut.CreateBudget(MakeDto(category.Id));

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task CreateBudget_CategoryIsIncomeType_ReturnsBadRequest()
    {
        var category = MakeCategory(_userId, TransactionType.Income);
        _uow.CategoryRepository.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);

        var result = await _sut.CreateBudget(MakeDto(category.Id));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateBudget_AlreadyExistsForMonth_ReturnsBadRequest()
    {
        var category = MakeCategory(_userId, TransactionType.Expense);
        _uow.CategoryRepository.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);
        _uow.BudgetRepository.Setup(r => r.ExistsAsync(_userId, "2026-09", category.Id, null)).ReturnsAsync(true);

        var result = await _sut.CreateBudget(MakeDto(category.Id));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateBudget_GlobalBudget_SkipsCategoryValidation()
    {
        var result = await _sut.CreateBudget(MakeDto(categoryId: null));

        Assert.IsType<CreatedAtActionResult>(result.Result);
        _uow.CategoryRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task CreateBudget_Valid_HasZeroSpent()
    {
        var category = MakeCategory(_userId, TransactionType.Expense);
        _uow.CategoryRepository.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);

        var result = await _sut.CreateBudget(MakeDto(category.Id));

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<API.DTOs.Responses.BudgetResponseDto>(created.Value);
        Assert.Equal(0, dto.Spent);
    }

    [Fact]
    public async Task UpdateBudget_DoesNotExist_ReturnsNotFound()
    {
        _uow.BudgetRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Budget?)null);

        var result = await _sut.UpdateBudget(Guid.NewGuid(), MakeDto(null));

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UpdateBudget_BelongsToAnotherUser_ReturnsForbid()
    {
        var budget = new Budget { Id = Guid.NewGuid(), AmountLimit = 50, Month = "2026-09", UserId = Guid.NewGuid() };
        _uow.BudgetRepository.Setup(r => r.GetByIdAsync(budget.Id)).ReturnsAsync(budget);

        var result = await _sut.UpdateBudget(budget.Id, MakeDto(null));

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task UpdateBudget_IgnoresCategoryIdAndMonthFromDto()
    {
        // §BudgetsController.UpdateBudget : CategoryId et Month sont immuables
        // après création, reçus dans le DTO partagé POST/PUT mais toujours ignorés.
        var originalCategoryId = Guid.NewGuid();
        var budget = new Budget
        {
            Id = Guid.NewGuid(), AmountLimit = 50, Month = "2026-09",
            CategoryId = originalCategoryId, UserId = _userId,
        };
        _uow.BudgetRepository.Setup(r => r.GetByIdAsync(budget.Id)).ReturnsAsync(budget);
        _uow.BudgetRepository
            .Setup(r => r.GetSpentTotalsAsync(_userId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync((IReadOnlyList<CategorySpentProjection>)[]);

        var otherCategoryId = Guid.NewGuid();
        await _sut.UpdateBudget(budget.Id, MakeDto(otherCategoryId, month: "2026-10", amount: 200));

        Assert.Equal(originalCategoryId, budget.CategoryId);
        Assert.Equal("2026-09", budget.Month);
        Assert.Equal(200, budget.AmountLimit);
    }

    [Fact]
    public async Task DeleteBudget_DoesNotExist_ReturnsNotFound()
    {
        _uow.BudgetRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Budget?)null);

        var result = await _sut.DeleteBudget(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteBudget_BelongsToAnotherUser_ReturnsForbid()
    {
        var budget = new Budget { Id = Guid.NewGuid(), AmountLimit = 50, Month = "2026-09", UserId = Guid.NewGuid() };
        _uow.BudgetRepository.Setup(r => r.GetByIdAsync(budget.Id)).ReturnsAsync(budget);

        var result = await _sut.DeleteBudget(budget.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteBudget_Owner_ReturnsNoContent()
    {
        var budget = new Budget { Id = Guid.NewGuid(), AmountLimit = 50, Month = "2026-09", UserId = _userId };
        _uow.BudgetRepository.Setup(r => r.GetByIdAsync(budget.Id)).ReturnsAsync(budget);

        var result = await _sut.DeleteBudget(budget.Id);

        Assert.IsType<NoContentResult>(result);
    }
}

using API.Controllers;
using API.DTOs.Requests;
using API.Entities;
using API.Interfaces;
using API.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers;

public class CategoriesControllerTests
{
    private readonly FakeUnitOfWork _uow = new();
    private readonly CategoriesController _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public CategoriesControllerTests()
    {
        _sut = new CategoriesController(_uow.Uow);
        _sut.SetUser(_userId);
    }

    private Category MakeCategory(TransactionType type = TransactionType.Expense, bool locked = false, Guid? userId = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Groceries",
        Icon = "pi-cart",
        Color = "#3b82f6",
        Type = type,
        UserId = userId ?? _userId,
        IsLocked = locked,
    };

    private static CategoryRequestDto MakeDto(string name = "New Category", TransactionType type = TransactionType.Expense) => new()
    {
        Name = name,
        Icon = "pi-star",
        Color = "#111111",
        Type = type,
    };

    [Fact]
    public async Task Create_DuplicateNameAndType_ReturnsBadRequest()
    {
        _uow.CategoryRepository.Setup(r => r.ExistsAsync(_userId, "New Category", TransactionType.Expense, null))
            .ReturnsAsync(true);

        var result = await _sut.Create(MakeDto());

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_Valid_ReturnsCreatedAtAction()
    {
        var result = await _sut.Create(MakeDto());

        Assert.IsType<CreatedAtActionResult>(result.Result);
        _uow.CategoryRepository.Verify(r => r.Add(It.Is<Category>(c =>
            c.UserId == _userId && !c.IsLocked && c.Enabled)), Times.Once);
    }

    [Fact]
    public async Task Update_DoesNotExist_ReturnsNotFound()
    {
        _uow.CategoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Category?)null);

        var result = await _sut.Update(Guid.NewGuid(), MakeDto());

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Update_BelongsToAnotherUser_ReturnsForbid()
    {
        var category = MakeCategory(userId: Guid.NewGuid());
        _uow.CategoryRepository.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);

        var result = await _sut.Update(category.Id, MakeDto());

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Update_RenamedToExistingName_ReturnsBadRequest()
    {
        var category = MakeCategory();
        _uow.CategoryRepository.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);
        _uow.CategoryRepository.Setup(r => r.ExistsAsync(_userId, "Renamed", category.Type, category.Id))
            .ReturnsAsync(true);

        var result = await _sut.Update(category.Id, MakeDto(name: "Renamed"));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Update_Renaming_ClearsTranslationKey()
    {
        var category = MakeCategory();
        category.TranslationKey = "category.system.groceries";
        _uow.CategoryRepository.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);

        await _sut.Update(category.Id, MakeDto(name: "Renamed"));

        Assert.Null(category.TranslationKey);
        Assert.Equal("Renamed", category.Name);
    }

    [Fact]
    public async Task Update_SameName_KeepsTranslationKey()
    {
        var category = MakeCategory();
        category.TranslationKey = "category.system.groceries";
        category.Name = "Groceries";
        _uow.CategoryRepository.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);

        await _sut.Update(category.Id, MakeDto(name: "Groceries"));

        Assert.Equal("category.system.groceries", category.TranslationKey);
    }

    [Fact]
    public async Task Update_TypeFromDtoIsIgnored()
    {
        // §CategoriesController.Update : Type est immuable après création, reçu
        // dans le DTO partagé POST/PUT mais toujours ignoré.
        var category = MakeCategory(type: TransactionType.Expense);
        _uow.CategoryRepository.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);

        await _sut.Update(category.Id, MakeDto(type: TransactionType.Income));

        Assert.Equal(TransactionType.Expense, category.Type);
    }

    [Fact]
    public async Task ToggleEnabled_DoesNotExist_ReturnsNotFound()
    {
        _uow.CategoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Category?)null);

        var result = await _sut.ToggleEnabled(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ToggleEnabled_BelongsToAnotherUser_ReturnsForbid()
    {
        var category = MakeCategory(userId: Guid.NewGuid());
        _uow.CategoryRepository.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);

        var result = await _sut.ToggleEnabled(category.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task ToggleEnabled_FlipsEnabledFlag()
    {
        var category = MakeCategory();
        category.Enabled = true;
        _uow.CategoryRepository.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);

        await _sut.ToggleEnabled(category.Id);

        Assert.False(category.Enabled);
    }

    [Fact]
    public async Task Delete_DoesNotExist_ReturnsNotFound()
    {
        _uow.CategoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Category?)null);

        var result = await _sut.Delete(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Delete_BelongsToAnotherUser_ReturnsForbid()
    {
        var category = MakeCategory(userId: Guid.NewGuid());
        _uow.CategoryRepository.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);

        var result = await _sut.Delete(category.Id);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Delete_LockedCategory_ReturnsForbid()
    {
        var category = MakeCategory(locked: true);
        _uow.CategoryRepository.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);

        var result = await _sut.Delete(category.Id);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Delete_Unused_DeletesWithoutReassignment()
    {
        var category = MakeCategory();
        _uow.CategoryRepository.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);
        _uow.TransactionRepository.Setup(r => r.GetByCategoryIdAsync(_userId, category.Id)).ReturnsAsync([]);
        _uow.BudgetRepository.Setup(r => r.GetByCategoryIdAsync(_userId, category.Id)).ReturnsAsync([]);
        _uow.RecurringTransactionRepository.Setup(r => r.GetByCategoryIdAsync(_userId, category.Id)).ReturnsAsync([]);

        var result = await _sut.Delete(category.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<API.DTOs.Responses.CategoryDeletedDto>(ok.Value);
        Assert.Equal(0, dto.ReassignedTransactionCount);
        _uow.CategoryRepository.Verify(r => r.GetLockedByTypeAsync(It.IsAny<Guid>(), It.IsAny<TransactionType>()), Times.Never);
        _uow.CategoryRepository.Verify(r => r.Delete(category), Times.Once);
    }

    [Fact]
    public async Task Delete_Used_ReassignsTransactionsBudgetsAndRecurringTransactionsToOther()
    {
        var category = MakeCategory();
        var other = MakeCategory(locked: true);
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(), Amount = 10, Type = TransactionType.Expense,
            Date = new DateOnly(2026, 9, 1), Label = "x", UserId = _userId, CategoryId = category.Id,
        };
        var budget = new Budget { Id = Guid.NewGuid(), AmountLimit = 50, Month = "2026-09", UserId = _userId, CategoryId = category.Id };
        var recurring = new RecurringTransaction
        {
            Id = Guid.NewGuid(), Label = "Loyer", Amount = 800, Type = TransactionType.Expense,
            Frequency = Frequency.Monthly, NextDueDate = new DateOnly(2026, 10, 1),
            UserId = _userId, CategoryId = category.Id,
        };

        _uow.CategoryRepository.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);
        _uow.CategoryRepository.Setup(r => r.GetLockedByTypeAsync(_userId, category.Type)).ReturnsAsync(other);
        _uow.TransactionRepository.Setup(r => r.GetByCategoryIdAsync(_userId, category.Id)).ReturnsAsync([transaction]);
        _uow.BudgetRepository.Setup(r => r.GetByCategoryIdAsync(_userId, category.Id)).ReturnsAsync([budget]);
        _uow.RecurringTransactionRepository.Setup(r => r.GetByCategoryIdAsync(_userId, category.Id)).ReturnsAsync([recurring]);
        _uow.BudgetRepository.Setup(r => r.ExistsAsync(_userId, budget.Month, other.Id, null)).ReturnsAsync(false);

        var result = await _sut.Delete(category.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<API.DTOs.Responses.CategoryDeletedDto>(ok.Value);
        Assert.Equal(1, dto.ReassignedTransactionCount);
        Assert.Equal(other.Id, transaction.CategoryId);
        Assert.Equal(other.Id, budget.CategoryId);
        Assert.Equal(other.Id, recurring.CategoryId);
        _uow.BudgetRepository.Verify(r => r.Delete(It.IsAny<Budget>()), Times.Never);
    }

    [Fact]
    public async Task Delete_BudgetCollidesWithOthersOwnBudget_DeletesInsteadOfReassigning()
    {
        // §CategoriesController.Delete : réaffecter violerait l'index unique partiel
        // (UserId, Month, CategoryId) si "Other" a déjà son propre budget ce mois-ci
        // — le budget est supprimé plutôt que de planter sur SaveChanges.
        var category = MakeCategory();
        var other = MakeCategory(locked: true);
        var budget = new Budget { Id = Guid.NewGuid(), AmountLimit = 50, Month = "2026-09", UserId = _userId, CategoryId = category.Id };

        _uow.CategoryRepository.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);
        _uow.CategoryRepository.Setup(r => r.GetLockedByTypeAsync(_userId, category.Type)).ReturnsAsync(other);
        _uow.TransactionRepository.Setup(r => r.GetByCategoryIdAsync(_userId, category.Id)).ReturnsAsync([]);
        _uow.BudgetRepository.Setup(r => r.GetByCategoryIdAsync(_userId, category.Id)).ReturnsAsync([budget]);
        _uow.RecurringTransactionRepository.Setup(r => r.GetByCategoryIdAsync(_userId, category.Id)).ReturnsAsync([]);
        _uow.BudgetRepository.Setup(r => r.ExistsAsync(_userId, budget.Month, other.Id, null)).ReturnsAsync(true);

        await _sut.Delete(category.Id);

        _uow.BudgetRepository.Verify(r => r.Delete(budget), Times.Once);
        _uow.BudgetRepository.Verify(r => r.Update(It.IsAny<Budget>()), Times.Never);
    }
}

using API.Controllers;
using API.DTOs.Requests;
using API.DTOs.Responses;
using API.Entities;
using API.Interfaces;
using API.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace API.Tests.Controllers;

public class TransactionsControllerTests
{
    private readonly FakeUnitOfWork _uow = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly TransactionsController _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public TransactionsControllerTests()
    {
        _sut = new TransactionsController(
            _uow.Uow, _notificationService.Object, new Mock<ILogger<TransactionsController>>().Object);
        _sut.SetUser(_userId);
    }

    private static Category MakeCategory(Guid userId, TransactionType type, bool locked = false) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Groceries",
        Icon = "pi-cart",
        Color = "#3b82f6",
        Type = type,
        UserId = userId,
        IsLocked = locked,
    };

    private static TransactionRequestDto MakeDto(Guid categoryId, TransactionType type = TransactionType.Expense) => new()
    {
        Amount = 42.50m,
        Type = type,
        Date = new DateOnly(2026, 9, 1),
        Label = "Courses",
        CategoryId = categoryId,
    };

    [Fact]
    public async Task CreateTransaction_CategoryDoesNotExist_ReturnsNotFound()
    {
        _uow.CategoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Category?)null);

        var result = await _sut.CreateTransaction(MakeDto(Guid.NewGuid()));

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateTransaction_CategoryBelongsToAnotherUser_ReturnsForbid()
    {
        var category = MakeCategory(Guid.NewGuid(), TransactionType.Expense);
        _uow.CategoryRepository.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);

        var result = await _sut.CreateTransaction(MakeDto(category.Id));

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task CreateTransaction_CategoryTypeMismatch_ReturnsBadRequest()
    {
        var category = MakeCategory(_userId, TransactionType.Income);
        _uow.CategoryRepository.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);

        var result = await _sut.CreateTransaction(MakeDto(category.Id, TransactionType.Expense));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateTransaction_ValidExpense_ReturnsCreatedAndChecksBudgetThresholds()
    {
        var category = MakeCategory(_userId, TransactionType.Expense);
        _uow.CategoryRepository.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);

        Transaction? saved = null;
        _uow.TransactionRepository.Setup(r => r.AddTransaction(It.IsAny<Transaction>()))
            .Callback<Transaction>(t => saved = t);
        _uow.TransactionRepository.Setup(r => r.GetTransactionByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() => saved);

        var result = await _sut.CreateTransaction(MakeDto(category.Id, TransactionType.Expense));

        Assert.IsType<CreatedAtActionResult>(result.Result);
        _notificationService.Verify(n => n.CheckBudgetThresholdsAsync(
            _userId, category.Id, "2026-09"), Times.Once);
    }

    [Fact]
    public async Task CreateTransaction_ValidIncome_DoesNotCheckBudgetThresholds()
    {
        var category = MakeCategory(_userId, TransactionType.Income);
        _uow.CategoryRepository.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);

        Transaction? saved = null;
        _uow.TransactionRepository.Setup(r => r.AddTransaction(It.IsAny<Transaction>()))
            .Callback<Transaction>(t => saved = t);
        _uow.TransactionRepository.Setup(r => r.GetTransactionByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() => saved);

        var result = await _sut.CreateTransaction(MakeDto(category.Id, TransactionType.Income));

        Assert.IsType<CreatedAtActionResult>(result.Result);
        _notificationService.Verify(n => n.CheckBudgetThresholdsAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateTransaction_NotificationServiceThrows_StillReturnsCreated()
    {
        // §CheckBudgetThresholdsAsync : ne doit jamais faire échouer l'écriture de
        // la transaction, quelle que soit l'exception levée par le service.
        var category = MakeCategory(_userId, TransactionType.Expense);
        _uow.CategoryRepository.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);
        _notificationService
            .Setup(n => n.CheckBudgetThresholdsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Transaction? saved = null;
        _uow.TransactionRepository.Setup(r => r.AddTransaction(It.IsAny<Transaction>()))
            .Callback<Transaction>(t => saved = t);
        _uow.TransactionRepository.Setup(r => r.GetTransactionByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() => saved);

        var result = await _sut.CreateTransaction(MakeDto(category.Id));

        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    [Fact]
    public async Task GetTransactionById_DoesNotExist_ReturnsNotFound()
    {
        _uow.TransactionRepository.Setup(r => r.GetTransactionByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Transaction?)null);

        var result = await _sut.GetTransactionById(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetTransactionById_BelongsToAnotherUser_ReturnsForbid()
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = 10,
            Type = TransactionType.Expense,
            Date = new DateOnly(2026, 9, 1),
            Label = "x",
            UserId = Guid.NewGuid(),
        };
        _uow.TransactionRepository.Setup(r => r.GetTransactionByIdAsync(transaction.Id)).ReturnsAsync(transaction);

        var result = await _sut.GetTransactionById(transaction.Id);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetTransactionById_Owner_ReturnsOk()
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = 10,
            Type = TransactionType.Expense,
            Date = new DateOnly(2026, 9, 1),
            Label = "x",
            UserId = _userId,
        };
        _uow.TransactionRepository.Setup(r => r.GetTransactionByIdAsync(transaction.Id)).ReturnsAsync(transaction);

        var result = await _sut.GetTransactionById(transaction.Id);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateTransaction_DoesNotExist_ReturnsNotFound()
    {
        _uow.TransactionRepository.Setup(r => r.GetTransactionByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Transaction?)null);

        var result = await _sut.UpdateTransaction(Guid.NewGuid(), MakeDto(Guid.NewGuid()));

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateTransaction_BelongsToAnotherUser_ReturnsForbid()
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = 10,
            Type = TransactionType.Expense,
            Date = new DateOnly(2026, 9, 1),
            Label = "x",
            UserId = Guid.NewGuid(),
        };
        _uow.TransactionRepository.Setup(r => r.GetTransactionByIdAsync(transaction.Id)).ReturnsAsync(transaction);

        var result = await _sut.UpdateTransaction(transaction.Id, MakeDto(Guid.NewGuid()));

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UpdateTransaction_CategoryTypeMismatch_ReturnsBadRequest()
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = 10,
            Type = TransactionType.Expense,
            Date = new DateOnly(2026, 9, 1),
            Label = "x",
            UserId = _userId,
        };
        _uow.TransactionRepository.Setup(r => r.GetTransactionByIdAsync(transaction.Id)).ReturnsAsync(transaction);

        var category = MakeCategory(_userId, TransactionType.Income);
        _uow.CategoryRepository.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);

        var result = await _sut.UpdateTransaction(transaction.Id, MakeDto(category.Id, TransactionType.Expense));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateTransaction_Valid_ReturnsOk()
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = 10,
            Type = TransactionType.Expense,
            Date = new DateOnly(2026, 9, 1),
            Label = "x",
            UserId = _userId,
        };
        var category = MakeCategory(_userId, TransactionType.Expense);
        _uow.TransactionRepository.Setup(r => r.GetTransactionByIdAsync(transaction.Id)).ReturnsAsync(transaction);
        _uow.CategoryRepository.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);

        var result = await _sut.UpdateTransaction(transaction.Id, MakeDto(category.Id, TransactionType.Expense));

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task DeleteTransaction_DoesNotExist_ReturnsNotFound()
    {
        _uow.TransactionRepository.Setup(r => r.GetTransactionByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Transaction?)null);

        var result = await _sut.DeleteTransaction(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteTransaction_BelongsToAnotherUser_ReturnsForbid()
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = 10,
            Type = TransactionType.Expense,
            Date = new DateOnly(2026, 9, 1),
            Label = "x",
            UserId = Guid.NewGuid(),
        };
        _uow.TransactionRepository.Setup(r => r.GetTransactionByIdAsync(transaction.Id)).ReturnsAsync(transaction);

        var result = await _sut.DeleteTransaction(transaction.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteTransaction_Owner_ReturnsNoContent()
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = 10,
            Type = TransactionType.Expense,
            Date = new DateOnly(2026, 9, 1),
            Label = "x",
            UserId = _userId,
        };
        _uow.TransactionRepository.Setup(r => r.GetTransactionByIdAsync(transaction.Id)).ReturnsAsync(transaction);

        var result = await _sut.DeleteTransaction(transaction.Id);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteTransactions_NoneFound_ReturnsNotFound()
    {
        _uow.TransactionRepository.Setup(r => r.GetTransactionsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<Guid>()))
            .ReturnsAsync([]);

        var result = await _sut.DeleteTransactions([Guid.NewGuid()]);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task DeleteTransactions_ReturnsActualDeletedCount_NotRequestedCount()
    {
        // §DeleteTransactions : un id déjà supprimé ailleurs est ignoré par le
        // repository — le client doit voir le compte réel, jamais ids.Count.
        var found = new List<Transaction>
        {
            new() { Id = Guid.NewGuid(), Amount = 1, Type = TransactionType.Expense,
                    Date = new DateOnly(2026, 9, 1), Label = "a", UserId = _userId },
        };
        _uow.TransactionRepository.Setup(r => r.GetTransactionsByIdsAsync(It.IsAny<List<Guid>>(), _userId))
            .ReturnsAsync(found);

        var result = await _sut.DeleteTransactions([found[0].Id, Guid.NewGuid()]);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(1, ok.Value);
    }
}

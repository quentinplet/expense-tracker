using API.Entities;
using API.Interfaces;
using API.Services;
using API.Tests.TestHelpers;
using Moq;

namespace API.Tests.Services;

public class NotificationServiceTests
{
    private readonly FakeUnitOfWork _uow = new();
    private readonly Mock<IBudgetService> _budgetService = new();
    private readonly NotificationService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public NotificationServiceTests()
    {
        _sut = new NotificationService(_uow.Uow, _budgetService.Object);
    }

    private Budget MakeBudget(Guid? categoryId, decimal amountLimit = 100, string month = "2026-09") => new()
    {
        Id = Guid.NewGuid(), AmountLimit = amountLimit, Month = month, CategoryId = categoryId, UserId = _userId,
    };

    [Fact]
    public async Task GetForUserAsync_ReturnsUnreadCountAndMappedItems()
    {
        var notification = new Notification { Id = Guid.NewGuid(), Type = NotificationType.RecurringTransactionGenerated, UserId = _userId };
        _uow.NotificationRepository.Setup(r => r.GetUnreadCountAsync(_userId)).ReturnsAsync(3);
        _uow.NotificationRepository.Setup(r => r.GetRecentAsync(_userId, 20)).ReturnsAsync([notification]);

        var result = await _sut.GetForUserAsync(_userId, 20);

        Assert.Equal(3, result.UnreadCount);
        Assert.Equal(notification.Id, result.Items.Single().Id);
    }

    [Fact]
    public async Task NotifyRecurringTransactionGeneratedAsync_AddsNotificationAndSaves()
    {
        var transactionId = Guid.NewGuid();

        await _sut.NotifyRecurringTransactionGeneratedAsync(_userId, transactionId);

        _uow.NotificationRepository.Verify(r => r.Add(It.Is<Notification>(n =>
            n.Type == NotificationType.RecurringTransactionGenerated &&
            n.TransactionId == transactionId && n.UserId == _userId)), Times.Once);
        _uow.Mock.Verify(u => u.Complete(), Times.Once);
    }

    [Fact]
    public async Task CheckBudgetThresholdsAsync_NoBudgetForMonth_DoesNothing()
    {
        var categoryId = Guid.NewGuid();
        _uow.BudgetRepository.Setup(r => r.GetAllByUserIdAndMonthAsync(_userId, "2026-09")).ReturnsAsync([]);

        await _sut.CheckBudgetThresholdsAsync(_userId, categoryId, "2026-09");

        _uow.NotificationRepository.Verify(r => r.Add(It.IsAny<Notification>()), Times.Never);
        _uow.Mock.Verify(u => u.Complete(), Times.Never);
    }

    [Fact]
    public async Task CheckBudgetThresholdsAsync_BelowNinetyPercent_CreatesNoNotification()
    {
        var categoryId = Guid.NewGuid();
        var budget = MakeBudget(categoryId, amountLimit: 100);
        _uow.BudgetRepository.Setup(r => r.GetAllByUserIdAndMonthAsync(_userId, "2026-09")).ReturnsAsync([budget]);
        _budgetService.Setup(s => s.GetSpentAsync(_userId, budget)).ReturnsAsync(50m);

        await _sut.CheckBudgetThresholdsAsync(_userId, categoryId, "2026-09");

        _uow.NotificationRepository.Verify(r => r.Add(It.IsAny<Notification>()), Times.Never);
    }

    [Fact]
    public async Task CheckBudgetThresholdsAsync_CrossingNinetyPercent_CreatesNinetyNotificationOnly()
    {
        var categoryId = Guid.NewGuid();
        var budget = MakeBudget(categoryId, amountLimit: 100);
        _uow.BudgetRepository.Setup(r => r.GetAllByUserIdAndMonthAsync(_userId, "2026-09")).ReturnsAsync([budget]);
        _budgetService.Setup(s => s.GetSpentAsync(_userId, budget)).ReturnsAsync(90m);
        _uow.NotificationRepository
            .Setup(r => r.ThresholdNotificationExistsAsync(budget.Id, It.IsAny<int>())).ReturnsAsync(false);

        await _sut.CheckBudgetThresholdsAsync(_userId, categoryId, "2026-09");

        _uow.NotificationRepository.Verify(r => r.Add(It.Is<Notification>(n =>
            n.BudgetId == budget.Id && n.ThresholdPercent == 90)), Times.Once);
        _uow.NotificationRepository.Verify(r => r.Add(It.Is<Notification>(n => n.ThresholdPercent == 100)), Times.Never);
        _uow.Mock.Verify(u => u.Complete(), Times.Once);
    }

    [Fact]
    public async Task CheckBudgetThresholdsAsync_AlreadyNotifiedAtThreshold_DoesNotDuplicate()
    {
        // Idempotence : la table Notifications elle-même sert d'état interrogé.
        var categoryId = Guid.NewGuid();
        var budget = MakeBudget(categoryId, amountLimit: 100);
        _uow.BudgetRepository.Setup(r => r.GetAllByUserIdAndMonthAsync(_userId, "2026-09")).ReturnsAsync([budget]);
        _budgetService.Setup(s => s.GetSpentAsync(_userId, budget)).ReturnsAsync(95m);
        _uow.NotificationRepository.Setup(r => r.ThresholdNotificationExistsAsync(budget.Id, 90)).ReturnsAsync(true);

        await _sut.CheckBudgetThresholdsAsync(_userId, categoryId, "2026-09");

        _uow.NotificationRepository.Verify(r => r.Add(It.IsAny<Notification>()), Times.Never);
        _uow.Mock.Verify(u => u.Complete(), Times.Never);
    }

    [Fact]
    public async Task CheckBudgetThresholdsAsync_JumpFromEightyToOneOhFivePercent_CreatesBothThresholdsAtOnce()
    {
        // 90 puis 100 sont deux notifications indépendantes, pas un état à deux
        // valeurs : un saut direct au-delà de 100% crée les deux dans le même appel.
        var categoryId = Guid.NewGuid();
        var budget = MakeBudget(categoryId, amountLimit: 100);
        _uow.BudgetRepository.Setup(r => r.GetAllByUserIdAndMonthAsync(_userId, "2026-09")).ReturnsAsync([budget]);
        _budgetService.Setup(s => s.GetSpentAsync(_userId, budget)).ReturnsAsync(105m);
        _uow.NotificationRepository
            .Setup(r => r.ThresholdNotificationExistsAsync(budget.Id, It.IsAny<int>())).ReturnsAsync(false);

        await _sut.CheckBudgetThresholdsAsync(_userId, categoryId, "2026-09");

        _uow.NotificationRepository.Verify(r => r.Add(It.Is<Notification>(n => n.ThresholdPercent == 90)), Times.Once);
        _uow.NotificationRepository.Verify(r => r.Add(It.Is<Notification>(n => n.ThresholdPercent == 100)), Times.Once);
    }

    [Fact]
    public async Task CheckBudgetThresholdsAsync_ZeroAmountLimitWithSpend_IsTreatedAsOneHundredPercent()
    {
        var categoryId = Guid.NewGuid();
        var budget = MakeBudget(categoryId, amountLimit: 0);
        _uow.BudgetRepository.Setup(r => r.GetAllByUserIdAndMonthAsync(_userId, "2026-09")).ReturnsAsync([budget]);
        _budgetService.Setup(s => s.GetSpentAsync(_userId, budget)).ReturnsAsync(1m);
        _uow.NotificationRepository
            .Setup(r => r.ThresholdNotificationExistsAsync(budget.Id, It.IsAny<int>())).ReturnsAsync(false);

        await _sut.CheckBudgetThresholdsAsync(_userId, categoryId, "2026-09");

        _uow.NotificationRepository.Verify(r => r.Add(It.Is<Notification>(n => n.ThresholdPercent == 90)), Times.Once);
        _uow.NotificationRepository.Verify(r => r.Add(It.Is<Notification>(n => n.ThresholdPercent == 100)), Times.Once);
    }

    [Fact]
    public async Task CheckBudgetThresholdsAsync_ZeroAmountLimitNoSpend_CreatesNoNotification()
    {
        var categoryId = Guid.NewGuid();
        var budget = MakeBudget(categoryId, amountLimit: 0);
        _uow.BudgetRepository.Setup(r => r.GetAllByUserIdAndMonthAsync(_userId, "2026-09")).ReturnsAsync([budget]);
        _budgetService.Setup(s => s.GetSpentAsync(_userId, budget)).ReturnsAsync(0m);

        await _sut.CheckBudgetThresholdsAsync(_userId, categoryId, "2026-09");

        _uow.NotificationRepository.Verify(r => r.Add(It.IsAny<Notification>()), Times.Never);
    }

    [Fact]
    public async Task CheckBudgetThresholdsAsync_ChecksCategoryAndGlobalBudgetIndependently()
    {
        var categoryId = Guid.NewGuid();
        var categoryBudget = MakeBudget(categoryId, amountLimit: 100);
        var globalBudget = MakeBudget(null, amountLimit: 1000);
        _uow.BudgetRepository.Setup(r => r.GetAllByUserIdAndMonthAsync(_userId, "2026-09"))
            .ReturnsAsync([categoryBudget, globalBudget]);
        _budgetService.Setup(s => s.GetSpentAsync(_userId, categoryBudget)).ReturnsAsync(95m); // 95% -> 90 seuil
        _budgetService.Setup(s => s.GetSpentAsync(_userId, globalBudget)).ReturnsAsync(200m);  // 20% -> rien
        _uow.NotificationRepository
            .Setup(r => r.ThresholdNotificationExistsAsync(It.IsAny<Guid>(), It.IsAny<int>())).ReturnsAsync(false);

        await _sut.CheckBudgetThresholdsAsync(_userId, categoryId, "2026-09");

        _uow.NotificationRepository.Verify(r => r.Add(It.Is<Notification>(n => n.BudgetId == categoryBudget.Id)), Times.Once);
        _uow.NotificationRepository.Verify(r => r.Add(It.Is<Notification>(n => n.BudgetId == globalBudget.Id)), Times.Never);
    }
}

using API.Entities;
using API.Interfaces;
using API.Services;
using API.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace API.Tests.Services;

public class RecurringTransactionServiceTests
{
    private readonly FakeUnitOfWork _uow = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly RecurringTransactionService _sut;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateOnly _today = DateOnly.FromDateTime(DateTime.UtcNow);

    public RecurringTransactionServiceTests()
    {
        _sut = new RecurringTransactionService(
            _uow.Uow, _notificationService.Object, new Mock<ILogger<RecurringTransactionService>>().Object);

        // Par défaut, rien n'a encore été généré pour aucune fenêtre demandée.
        _uow.TransactionRepository
            .Setup(r => r.GetGeneratedDatesAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync([]);
    }

    private RecurringTransaction MakeModel(
        DateOnly nextDueDate, Frequency frequency = Frequency.Monthly, TransactionType type = TransactionType.Expense) => new()
    {
        Id = Guid.NewGuid(),
        Label = "Loyer",
        Amount = 800,
        Type = type,
        Frequency = frequency,
        NextDueDate = nextDueDate,
        Active = true,
        CategoryId = Guid.NewGuid(),
        UserId = _userId,
    };

    [Fact]
    public async Task GenerateDueTransactionsAsync_NothingDue_ReturnsZeroAndNeverSaves()
    {
        _uow.RecurringTransactionRepository.Setup(r => r.GetDueAsync(It.IsAny<DateOnly>())).ReturnsAsync([]);

        var created = await _sut.GenerateDueTransactionsAsync();

        Assert.Equal(0, created);
        _uow.Mock.Verify(u => u.Complete(), Times.Never);
    }

    [Fact]
    public async Task GenerateDueTransactionsAsync_SingleOverdueOccurrence_GeneratesOneTransaction()
    {
        var model = MakeModel(_today);
        _uow.RecurringTransactionRepository.Setup(r => r.GetDueAsync(It.IsAny<DateOnly>())).ReturnsAsync([model]);

        Transaction? generated = null;
        _uow.TransactionRepository.Setup(r => r.AddTransaction(It.IsAny<Transaction>()))
            .Callback<Transaction>(t => generated = t);

        var created = await _sut.GenerateDueTransactionsAsync();

        Assert.Equal(1, created);
        Assert.NotNull(generated);
        Assert.Equal(model.Amount, generated!.Amount);
        Assert.Equal(model.Type, generated.Type);
        Assert.Equal(_today, generated.Date);
        Assert.Equal(model.Label, generated.Label);
        Assert.Equal(model.CategoryId, generated.CategoryId);
        Assert.Equal(model.UserId, generated.UserId);
        Assert.Equal(model.Id, generated.RecurringTransactionId);
        Assert.Equal(_today.AddMonths(1), model.NextDueDate);
    }

    [Fact]
    public async Task GenerateDueTransactionsAsync_TwoWeeksOverdueWeekly_CatchesUpAllThreeOccurrences()
    {
        // §History (charges récurrentes) : un modèle hebdomadaire en retard de deux
        // semaines doit générer exactement 3 transactions de rattrapage en un run.
        var model = MakeModel(_today.AddDays(-14), Frequency.Weekly);
        _uow.RecurringTransactionRepository.Setup(r => r.GetDueAsync(It.IsAny<DateOnly>())).ReturnsAsync([model]);

        var generatedDates = new List<DateOnly>();
        _uow.TransactionRepository.Setup(r => r.AddTransaction(It.IsAny<Transaction>()))
            .Callback<Transaction>(t => generatedDates.Add(t.Date));

        var created = await _sut.GenerateDueTransactionsAsync();

        Assert.Equal(3, created);
        Assert.Equal([_today.AddDays(-14), _today.AddDays(-7), _today], generatedDates);
        Assert.Equal(_today.AddDays(7), model.NextDueDate);
    }

    [Fact]
    public async Task GenerateDueTransactionsAsync_RestartAfterFullCatchUp_IsIdempotent()
    {
        // Rejouer le job une fois tout déjà généré ne doit rien recréer, mais doit
        // tout de même avancer NextDueDate jusqu'au curseur calculé.
        var model = MakeModel(_today.AddDays(-14), Frequency.Weekly);
        _uow.RecurringTransactionRepository.Setup(r => r.GetDueAsync(It.IsAny<DateOnly>())).ReturnsAsync([model]);
        _uow.TransactionRepository
            .Setup(r => r.GetGeneratedDatesAsync(model.Id, It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync([_today.AddDays(-14), _today.AddDays(-7), _today]);

        var created = await _sut.GenerateDueTransactionsAsync();

        Assert.Equal(0, created);
        _uow.TransactionRepository.Verify(r => r.AddTransaction(It.IsAny<Transaction>()), Times.Never);
        Assert.Equal(_today.AddDays(7), model.NextDueDate);
    }

    [Fact]
    public async Task GenerateDueTransactionsAsync_LongOverdueDaily_CapsAtTwentyFourIterationsPerRun()
    {
        // §MaxCatchUpIterationsPerRecurringTransaction : une transaction récurrente
        // en retard de 30 jours ne rattrape que 24 occurrences dans ce run, le
        // reste attend le prochain passage du job.
        var model = MakeModel(_today.AddDays(-30), Frequency.Daily);
        _uow.RecurringTransactionRepository.Setup(r => r.GetDueAsync(It.IsAny<DateOnly>())).ReturnsAsync([model]);

        var created = await _sut.GenerateDueTransactionsAsync();

        Assert.Equal(24, created);
        Assert.Equal(_today.AddDays(-6), model.NextDueDate);
    }

    [Fact]
    public async Task GenerateDueTransactionsAsync_ExpenseType_NotifiesGenerationAndChecksBudgetThresholds()
    {
        var model = MakeModel(_today, type: TransactionType.Expense);
        _uow.RecurringTransactionRepository.Setup(r => r.GetDueAsync(It.IsAny<DateOnly>())).ReturnsAsync([model]);

        await _sut.GenerateDueTransactionsAsync();

        _notificationService.Verify(n => n.NotifyRecurringTransactionGeneratedAsync(
            model.UserId, It.IsAny<Guid>()), Times.Once);
        _notificationService.Verify(n => n.CheckBudgetThresholdsAsync(
            model.UserId, model.CategoryId, _today.ToString("yyyy-MM")), Times.Once);
    }

    [Fact]
    public async Task GenerateDueTransactionsAsync_IncomeType_NotifiesGenerationButSkipsBudgetCheck()
    {
        var model = MakeModel(_today, type: TransactionType.Income);
        _uow.RecurringTransactionRepository.Setup(r => r.GetDueAsync(It.IsAny<DateOnly>())).ReturnsAsync([model]);

        await _sut.GenerateDueTransactionsAsync();

        _notificationService.Verify(n => n.NotifyRecurringTransactionGeneratedAsync(
            model.UserId, It.IsAny<Guid>()), Times.Once);
        _notificationService.Verify(n => n.CheckBudgetThresholdsAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GenerateDueTransactionsAsync_NotificationServiceThrows_StillCompletesGeneration()
    {
        // §NotifyGeneratedAsync : ne doit jamais faire échouer la génération, quelle
        // que soit l'exception levée par le service de notification.
        var model = MakeModel(_today);
        _uow.RecurringTransactionRepository.Setup(r => r.GetDueAsync(It.IsAny<DateOnly>())).ReturnsAsync([model]);
        _notificationService
            .Setup(n => n.NotifyRecurringTransactionGeneratedAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var created = await _sut.GenerateDueTransactionsAsync();

        Assert.Equal(1, created);
        Assert.Equal(_today.AddMonths(1), model.NextDueDate);
    }
}

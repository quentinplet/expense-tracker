using API.Interfaces;
using Moq;

namespace API.Tests.TestHelpers;

/// Wraps a mocked IUnitOfWork with its four repository mocks exposed directly,
/// so a test can Setup/Verify against e.g. `uow.TransactionRepository` without
/// re-wiring the IUnitOfWork plumbing in every test class.
public class FakeUnitOfWork
{
    public Mock<ITransactionRepository> TransactionRepository { get; } = new();
    public Mock<ICategoryRepository> CategoryRepository { get; } = new();
    public Mock<IBudgetRepository> BudgetRepository { get; } = new();
    public Mock<IRecurringTransactionRepository> RecurringTransactionRepository { get; } = new();
    public Mock<INotificationRepository> NotificationRepository { get; } = new();

    private readonly Mock<IUnitOfWork> _mock = new();

    public IUnitOfWork Uow => _mock.Object;

    /// Exposed for assertions on IUnitOfWork itself (e.g. Verify(u => u.Complete())
    /// call counts) that don't belong to any single repository mock above.
    public Mock<IUnitOfWork> Mock => _mock;

    public FakeUnitOfWork()
    {
        _mock.SetupGet(u => u.TransactionRepository).Returns(TransactionRepository.Object);
        _mock.SetupGet(u => u.CategoryRepository).Returns(CategoryRepository.Object);
        _mock.SetupGet(u => u.BudgetRepository).Returns(BudgetRepository.Object);
        _mock.SetupGet(u => u.RecurringTransactionRepository).Returns(RecurringTransactionRepository.Object);
        _mock.SetupGet(u => u.NotificationRepository).Returns(NotificationRepository.Object);
        _mock.Setup(u => u.Complete()).ReturnsAsync(true);
    }

    /// Simulates SaveChangesAsync affecting zero rows, the failure branch every
    /// mutating action in this codebase checks for.
    public void FailComplete() => _mock.Setup(u => u.Complete()).ReturnsAsync(false);
}

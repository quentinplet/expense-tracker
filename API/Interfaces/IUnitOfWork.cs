using System;

namespace API.Interfaces;

public interface IUnitOfWork
{
    ICategoryRepository CategoryRepository { get; }
    ITransactionRepository TransactionRepository { get; }
    IBudgetRepository BudgetRepository { get; }
    IRecurringTransactionRepository RecurringTransactionRepository { get; }
    INotificationRepository NotificationRepository { get; }
    IImportBatchRepository ImportBatchRepository { get; }
    Task<bool> Complete();
    bool HasChanges();
}

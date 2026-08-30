using System;

namespace API.Interfaces;

public interface IUnitOfWork
{
    ICategoryRepository CategoryRepository { get; }
    ITransactionRepository TransactionRepository { get; }
    IBudgetRepository BudgetRepository { get; }
    IRecurringExpenseRepository RecurringExpenseRepository { get; }
    Task<bool> Complete();
    bool HasChanges();
}

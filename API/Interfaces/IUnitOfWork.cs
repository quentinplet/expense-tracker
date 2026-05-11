using System;

namespace API.Interfaces;

public interface IUnitOfWork
{
    ICategoryRepository CategoryRepository { get; }
    ITransactionRepository TransactionRepository { get; }
    IBudgetRepository BudgetRepository { get; }
    Task<bool> Complete();
    bool HasChanges();
}

using System;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data.Repositories;

public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    private ICategoryRepository? _categoryRepository;
    private ITransactionRepository? _transactionRepository;
    private IBudgetRepository? _budgetRepository;

    public ICategoryRepository CategoryRepository => _categoryRepository ??= new CategoryRepository(context);
    public ITransactionRepository TransactionRepository => _transactionRepository ??= new TransactionRepository(context);
    public IBudgetRepository BudgetRepository => _budgetRepository ??= new BudgetRepository(context);

    public async Task<bool> Complete()
    {
        try
        {
            return await context.SaveChangesAsync() > 0;
        }
        catch (DbUpdateException ex)
        {

            throw new Exception("An error occurred while saving changes to the database.", ex);
        }
    }

    public bool HasChanges()
    {
        return context.ChangeTracker.HasChanges();
    }
}

using API.Entities;

namespace API.Interfaces;

public interface IRecurringExpenseRepository
{
    /// Triées par NextDueDate croissant (la charge la plus proche de son
    /// échéance en premier) — actives et en pause confondues.
    Task<List<RecurringExpense>> GetAllAsync(Guid userId);
    Task<RecurringExpense?> GetByIdAsync(Guid id);
    Task<List<RecurringExpense>> GetByCategoryIdAsync(Guid userId, Guid categoryId);

    /// Toutes les charges actives en retard, tous utilisateurs confondus — le job
    /// tourne hors contexte HTTP, comme IBudgetRepository.GetAutoRenewCandidatesAsync.
    Task<List<RecurringExpense>> GetDueAsync(DateOnly today);

    void Add(RecurringExpense recurringExpense);
    void Update(RecurringExpense recurringExpense);
    void Delete(RecurringExpense recurringExpense);
}

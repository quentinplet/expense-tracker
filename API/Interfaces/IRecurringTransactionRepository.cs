using API.Entities;

namespace API.Interfaces;

public interface IRecurringTransactionRepository
{
    /// Triées par NextDueDate croissant (la transaction récurrente la plus proche
    /// de son échéance en premier) — actives et en pause confondues.
    Task<List<RecurringTransaction>> GetAllAsync(Guid userId);
    Task<RecurringTransaction?> GetByIdAsync(Guid id);
    Task<List<RecurringTransaction>> GetByCategoryIdAsync(Guid userId, Guid categoryId);

    /// Toutes les transactions récurrentes actives en retard, tous utilisateurs
    /// confondus — le job tourne hors contexte HTTP, comme
    /// IBudgetRepository.GetAutoRenewCandidatesAsync.
    Task<List<RecurringTransaction>> GetDueAsync(DateOnly today);

    void Add(RecurringTransaction recurringTransaction);
    void Update(RecurringTransaction recurringTransaction);
    void Delete(RecurringTransaction recurringTransaction);
}

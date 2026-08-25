using System;
using API.Entities;

namespace API.Interfaces;

public interface IBudgetRepository
{
    Task<IReadOnlyList<Budget>> GetAllByUserIdAsync(Guid userId);
    Task<List<Budget>> GetAllByUserIdAndMonthAsync(Guid userId, int month, int year);
    Task<Budget?> GetByIdAsync(Guid id);
    void Add(Budget budget);
    void Update(Budget budget);
    void Delete(Budget budget);

}

using System;
using API.Entities;

namespace API.Interfaces;

public interface IBudgetRepository
{
    Task<IReadOnlyList<Budget>> GetAllByUserIdAsync(string userId);
    Task<List<Budget>> GetAllByUserIdAndMonthAsync(string userId, int month, int year);
    Task<Budget?> GetByIdAsync(int id);
    void Add(Budget budget);
    void Update(Budget budget);
    void Delete(Budget budget);

}

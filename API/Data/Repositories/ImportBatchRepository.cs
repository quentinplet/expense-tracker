using API.Entities;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data.Repositories;

public class ImportBatchRepository(AppDbContext context) : IImportBatchRepository
{
    public async Task<List<ImportBatch>> GetAllAsync(Guid userId)
    {
        return await context.ImportBatches
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.ImportedAt)
            .ToListAsync();
    }

    public async Task<ImportBatch?> GetByIdAsync(Guid id)
    {
        return await context.ImportBatches.FirstOrDefaultAsync(b => b.Id == id);
    }

    public void Add(ImportBatch batch)
    {
        context.ImportBatches.Add(batch);
    }

    public void Delete(ImportBatch batch)
    {
        context.ImportBatches.Remove(batch);
    }
}

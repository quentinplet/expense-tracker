using API.Entities;

namespace API.Interfaces;

public interface IImportBatchRepository
{
    Task<List<ImportBatch>> GetAllAsync(Guid userId);
    Task<ImportBatch?> GetByIdAsync(Guid id);
    void Add(ImportBatch batch);
    void Delete(ImportBatch batch);
}

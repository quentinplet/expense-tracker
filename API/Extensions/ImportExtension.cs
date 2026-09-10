using API.DTOs.Responses;
using API.Entities;

namespace API.Extensions;

public static class ImportExtension
{
    public static ImportBatchDto ToImportBatchDto(this ImportBatch batch) => new(
        batch.Id, batch.FileName, batch.ImportedAt, batch.RowCount, batch.Status.ToString());
}

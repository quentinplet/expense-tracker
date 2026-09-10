namespace API.DTOs.Responses;

public record ImportBatchDto(
    Guid Id, string FileName, DateTime ImportedAt, int RowCount, string Status);

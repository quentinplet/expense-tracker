namespace API.DTOs.Responses;

public record ImportConfirmResultDto(Guid ImportBatchId, int InsertedCount, int SkippedDuplicateCount);

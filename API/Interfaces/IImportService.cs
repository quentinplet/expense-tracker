using API.DTOs.Requests;
using API.DTOs.Responses;

namespace API.Interfaces;

public interface IImportService
{
    /// Parse le fichier et retourne une prévisualisation — n'écrit rien en base
    /// (§ Flux en deux temps, doc feature Import CSV).
    Task<ImportPreviewDto> PreviewAsync(Guid userId, ImportUploadRequestDto request);

    /// Insère les lignes retenues, recalculées à partir des données brutes reçues —
    /// ne fait jamais confiance aux flags IsDuplicate/HasRecurringConflict de la
    /// prévisualisation.
    Task<ImportConfirmResultDto> ConfirmAsync(Guid userId, ImportConfirmRequestDto request);
}

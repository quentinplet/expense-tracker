namespace API.DTOs.Responses;

public record ColumnMappingDto(
    string? DateColumn, string? LabelColumn,
    string? AmountColumn,                       // rempli si le fichier a une colonne montant signée
    string? DebitColumn, string? CreditColumn,   // remplis si le fichier sépare débit/crédit
    string DateFormat, string CultureName);      // ex. "dd/MM/yyyy", "fr-FR"

public record ImportPreviewRowDto(
    int RowNumber,                  // position 1-based dans le fichier, pour les messages d'erreur
    DateOnly? Date, decimal? Amount, string? Type,
    string RawLabel,                // texte brut de la colonne libellé, jamais modifié — sert au DedupHash
    string SuggestedLabel,          // RawLabel nettoyé (retours à la ligne aplatis) — pré-remplissage du champ éditable
    bool IsDuplicate, bool HasRecurringConflict, Guid? ConflictingTransactionId,
    bool IsValid, string? ParseError);

public record ImportPreviewDto(
    string[] DetectedColumns, ColumnMappingDto SuggestedMapping,
    IReadOnlyList<ImportPreviewRowDto> Rows,
    int TotalRows, int DuplicateCount, int ConflictCount, int InvalidCount);

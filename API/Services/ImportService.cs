using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using API.DTOs.Requests;
using API.DTOs.Responses;
using API.Entities;
using API.Errors;
using API.Interfaces;

namespace API.Services;

public class ImportService(
    IUnitOfWork uow, INotificationService notificationService, ILogger<ImportService> logger) : IImportService
{
    /// Fenêtre de rapprochement pour un conflit avec une transaction récurrente déjà
    /// générée — décidée en §22 Q14 de l'overview, constante du service plutôt qu'une
    /// configuration exposée (doc feature Import CSV, conflit recurring).
    private const int RecurringConflictWindowDays = 3;

    private static readonly string[] DateHeaderCandidates = ["date"];
    private static readonly string[] LabelHeaderCandidates =
        ["libelle", "description", "label", "intitule", "nature", "operation"];
    private static readonly string[] AmountHeaderCandidates = ["montant", "amount"];
    private static readonly string[] DebitHeaderCandidates = ["debit"];
    private static readonly string[] CreditHeaderCandidates = ["credit"];

    public async Task<ImportPreviewDto> PreviewAsync(Guid userId, ImportUploadRequestDto request)
    {
        await using var stream = new MemoryStream();
        await request.File!.CopyToAsync(stream);
        var parsed = CsvBankFileParser.Parse(stream.ToArray());

        var mapping = ResolveMapping(parsed.Headers, request);
        var culture = ResolveCulture(mapping.CultureName);

        var rows = parsed.Rows.Select(r => ParseRow(r, mapping, culture)).ToList();
        var resolvedRows = await ResolveDuplicatesAndConflictsAsync(userId, rows);

        return new ImportPreviewDto(
            parsed.Headers, mapping, resolvedRows,
            resolvedRows.Count,
            resolvedRows.Count(r => r.IsDuplicate),
            resolvedRows.Count(r => r.HasRecurringConflict),
            resolvedRows.Count(r => !r.IsValid));
    }

    public async Task<ImportConfirmResultDto> ConfirmAsync(Guid userId, ImportConfirmRequestDto request)
    {
        var rows = request.Rows!;

        // Toute catégorie explicite doit appartenir à l'utilisateur et correspondre au
        // Type de sa ligne — validée en amont, avant tout insert (même cohérence
        // métier que TransactionsController, mais rejetée en bloc plutôt que ligne par
        // ligne : un import est une seule opération du point de vue de l'utilisateur).
        var categoryIds = rows.Where(r => r.CategoryId != null).Select(r => r.CategoryId!.Value).Distinct();
        var categories = new Dictionary<Guid, Category>();
        foreach (var categoryId in categoryIds)
        {
            var category = await uow.CategoryRepository.GetByIdAsync(categoryId);
            if (category == null || category.UserId != userId)
                throw new ImportValidationException("One or more selected categories could not be found.");
            categories[categoryId] = category;
        }

        foreach (var row in rows.Where(r => r.CategoryId != null))
        {
            if (categories[row.CategoryId!.Value].Type != row.Type!.Value)
                throw new ImportValidationException("The category does not match the transaction type.");
        }

        var otherExpense = await uow.CategoryRepository.GetLockedByTypeAsync(userId, TransactionType.Expense);
        var otherIncome = await uow.CategoryRepository.GetLockedByTypeAsync(userId, TransactionType.Income);

        // Recalcul serveur du DedupHash à partir des données brutes reçues — jamais
        // fait confiance au flag IsDuplicate de la prévisualisation (§ Flux en deux
        // temps, doc feature Import CSV).
        var hashByRow = rows
            .Select(r => (Row: r, Hash: ComputeDedupHash(r.Date!.Value, r.Amount!.Value, r.RawLabelForHash!)))
            .ToList();
        var existingHashes = await uow.TransactionRepository.GetExistingDedupHashesAsync(
            userId, hashByRow.Select(h => h.Hash));

        var batch = new ImportBatch { FileName = request.FileName!, UserId = userId };
        var inserted = new List<Transaction>();
        var skippedDuplicateCount = 0;

        foreach (var (row, hash) in hashByRow)
        {
            // Le hash déjà en base OU déjà vu plus tôt dans ce même lot (la même ligne
            // présente deux fois dans le fichier importé) — les deux comptent comme
            // doublon, jamais une erreur bloquante.
            if (!existingHashes.Add(hash)) { skippedDuplicateCount++; continue; }

            var categoryId = row.CategoryId ?? (row.Type == TransactionType.Expense ? otherExpense?.Id : otherIncome?.Id)
                ?? throw new ImportValidationException("No category available to assign this row.");

            var transaction = new Transaction
            {
                Amount = row.Amount!.Value,
                Type = row.Type!.Value,
                Date = row.Date!.Value,
                Label = row.Label!,
                Note = row.Note,
                CategoryId = categoryId,
                UserId = userId,
                DedupHash = hash,
                ImportBatch = batch,
            };
            inserted.Add(transaction);
            uow.TransactionRepository.AddTransaction(transaction);
        }

        // Un fichier entièrement composé de doublons n'insère rien : SaveChangesAsync
        // renvoie alors légitimement 0 (aucun changement), pas un échec — uow.Complete()
        // n'est donc appelé que s'il y a réellement quelque chose à écrire.
        if (inserted.Count == 0) return new ImportConfirmResultDto(Guid.Empty, 0, skippedDuplicateCount);

        batch.RowCount = inserted.Count;
        uow.ImportBatchRepository.Add(batch);

        if (!await uow.Complete()) throw new ImportValidationException("Failed to confirm import.");

        await CheckBudgetThresholdsAsync(userId, inserted);

        return new ImportConfirmResultDto(batch.Id, inserted.Count, skippedDuplicateCount);
    }

    /// Une seule requête par vérification pour tout le fichier (dédoublonnage, conflit
    /// recurring), jamais une par ligne — même remarque à la prévisualisation qu'à la
    /// confirmation.
    private async Task<List<ImportPreviewRowDto>> ResolveDuplicatesAndConflictsAsync(
        Guid userId, List<ImportPreviewRowDto> rows)
    {
        var validRows = rows.Where(r => r.IsValid).ToList();
        if (validRows.Count == 0) return rows;

        var hashes = validRows.Select(r => ComputeDedupHash(r.Date!.Value, r.Amount!.Value, r.RawLabel)).ToList();
        var existingHashes = await uow.TransactionRepository.GetExistingDedupHashesAsync(userId, hashes);

        var dates = validRows.Select(r => r.Date!.Value).ToList();
        var recurringCandidates = await uow.TransactionRepository.GetRecurringGeneratedInRangeAsync(
            userId, dates.Min().AddDays(-RecurringConflictWindowDays), dates.Max().AddDays(RecurringConflictWindowDays));

        return
        [
            .. rows.Select(row =>
            {
                if (!row.IsValid) return row;

                var hash = ComputeDedupHash(row.Date!.Value, row.Amount!.Value, row.RawLabel);
                var conflict = recurringCandidates.FirstOrDefault(t =>
                    t.Amount == row.Amount!.Value &&
                    t.Type.ToString() == row.Type &&
                    Math.Abs(t.Date.DayNumber - row.Date!.Value.DayNumber) <= RecurringConflictWindowDays);

                return row with
                {
                    IsDuplicate = existingHashes.Contains(hash),
                    HasRecurringConflict = conflict != null,
                    ConflictingTransactionId = conflict?.Id,
                };
            }),
        ];
    }

    /// Seules les dépenses ont un budget à surveiller, une fois par (catégorie, mois)
    /// distinct plutôt qu'une fois par ligne — même principe que
    /// RecurringTransactionService, mais un import insère déjà tout avant de vérifier
    /// (contrairement au job, qui doit voir chaque échéance persistée avant la
    /// suivante). Ne doit jamais faire échouer la confirmation : toute exception est
    /// journalisée et avalée ici, jamais propagée.
    private async Task CheckBudgetThresholdsAsync(Guid userId, List<Transaction> inserted)
    {
        var expenseGroups = inserted
            .Where(t => t.Type == TransactionType.Expense)
            .Select(t => new { t.CategoryId, Month = t.Date.ToString("yyyy-MM") })
            .Distinct();

        foreach (var group in expenseGroups)
        {
            try
            {
                await notificationService.CheckBudgetThresholdsAsync(userId, group.CategoryId, group.Month);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Budget threshold check failed for import, category {CategoryId}.", group.CategoryId);
            }
        }
    }

    private static ColumnMappingDto ResolveMapping(string[] headers, ImportUploadRequestDto request)
    {
        var cultureName = string.IsNullOrWhiteSpace(request.CultureName) ? "fr-FR" : request.CultureName;
        var dateFormat = string.IsNullOrWhiteSpace(request.DateFormat) ? "dd/MM/yyyy" : request.DateFormat;

        var dateColumn = ResolveColumn(headers, request.DateColumn, DateHeaderCandidates);
        var labelColumn = ResolveColumn(headers, request.LabelColumn, LabelHeaderCandidates);
        var debitColumn = ResolveColumn(headers, request.DebitColumn, DebitHeaderCandidates);
        var creditColumn = ResolveColumn(headers, request.CreditColumn, CreditHeaderCandidates);

        // Mutuellement exclusif avec Débit/Crédit (§ Key Gotchas doc feature Import
        // CSV) : ignoré dès que l'un des deux est résolu, même mappé par erreur côté
        // client.
        var amountColumn = debitColumn != null || creditColumn != null
            ? null
            : ResolveColumn(headers, request.AmountColumn, AmountHeaderCandidates);

        return new ColumnMappingDto(dateColumn, labelColumn, amountColumn, debitColumn, creditColumn, dateFormat, cultureName);
    }

    private static string? ResolveColumn(string[] headers, string? overrideValue, string[] candidates)
    {
        if (!string.IsNullOrWhiteSpace(overrideValue))
        {
            var matched = headers.FirstOrDefault(h => string.Equals(h, overrideValue, StringComparison.OrdinalIgnoreCase));
            if (matched != null) return matched;
        }

        return headers.FirstOrDefault(h => candidates.Any(c => Normalize(h).Contains(c)));
    }

    /// Minuscules, accents retirés — les en-têtes bancaires FR varient
    /// ("Libellé"/"Libelle", "Débit"/"Debit"...).
    private static string Normalize(string value)
    {
        var formD = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
        }
        return sb.ToString().ToLowerInvariant();
    }

    private static CultureInfo ResolveCulture(string cultureName)
    {
        try
        {
            return CultureInfo.GetCultureInfo(cultureName);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.GetCultureInfo("fr-FR");
        }
    }

    private static ImportPreviewRowDto ParseRow(ParsedCsvRow row, ColumnMappingDto mapping, CultureInfo culture)
    {
        var rawLabel = GetValue(row, mapping.LabelColumn);
        var date = ParseDate(GetValue(row, mapping.DateColumn), mapping.DateFormat, culture);
        var (amount, type) = ParseAmountAndType(row, mapping, culture);

        var isValid = date != null && amount is > 0 && type != null && !string.IsNullOrWhiteSpace(rawLabel);
        var error = isValid ? null : BuildParseError(mapping, date, rawLabel);

        return new ImportPreviewRowDto(
            row.RowNumber, date, amount, type?.ToString(), rawLabel ?? string.Empty, CleanLabel(rawLabel),
            false, false, null, isValid, error);
    }

    private static string? GetValue(ParsedCsvRow row, string? column) =>
        column != null && row.Values.TryGetValue(column, out var value) ? value : null;

    /// Certains exports bancaires (Crédit Agricole notamment) mettent le libellé dans
    /// un champ CSV multi-lignes ("Paiement par carte\nX6203 ...\n\n\n"). RawLabel
    /// garde ce texte intact pour le DedupHash ; SuggestedLabel aplatit les retours à
    /// la ligne en espaces simples pour préremplir un champ éditable lisible — jamais
    /// l'inverse, la ligne blanche/dupliquée ne doit pas fausser le hash.
    private static string CleanLabel(string? rawLabel)
    {
        if (string.IsNullOrWhiteSpace(rawLabel)) return string.Empty;

        var collapsed = string.Join(
            " ",
            rawLabel.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        return collapsed.Length > 200 ? collapsed[..200] : collapsed;
    }

    private static DateOnly? ParseDate(string? raw, string format, CultureInfo culture)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (DateOnly.TryParseExact(raw, format, culture, DateTimeStyles.None, out var exact)) return exact;
        return DateOnly.TryParse(raw, culture, DateTimeStyles.None, out var fallback) ? fallback : null;
    }

    /// Deux façons mutuellement exclusives de porter le sens (§ Key Gotchas doc
    /// feature Import CSV) : une colonne montant signée, ou deux colonnes Débit/Crédit
    /// séparées — ResolveMapping garantit qu'une seule des deux formes est active ici.
    private static (decimal? Amount, TransactionType? Type) ParseAmountAndType(
        ParsedCsvRow row, ColumnMappingDto mapping, CultureInfo culture)
    {
        if (mapping.DebitColumn != null || mapping.CreditColumn != null)
        {
            var debit = GetValue(row, mapping.DebitColumn);
            if (!string.IsNullOrWhiteSpace(debit) && TryParseAmount(debit, culture, out var debitAmount))
                return (Math.Abs(debitAmount), TransactionType.Expense);

            var credit = GetValue(row, mapping.CreditColumn);
            if (!string.IsNullOrWhiteSpace(credit) && TryParseAmount(credit, culture, out var creditAmount))
                return (Math.Abs(creditAmount), TransactionType.Income);

            return (null, null);
        }

        var signed = GetValue(row, mapping.AmountColumn);
        if (string.IsNullOrWhiteSpace(signed) || !TryParseAmount(signed, culture, out var amount)) return (null, null);
        return (Math.Abs(amount), amount < 0 ? TransactionType.Expense : TransactionType.Income);
    }

    private static bool TryParseAmount(string raw, CultureInfo culture, out decimal amount)
    {
        var cleaned = raw.Replace(" ", "").Replace(" ", ""); // espace insécable (milliers FR)
        return decimal.TryParse(
            cleaned, NumberStyles.Number | NumberStyles.AllowLeadingSign, culture, out amount);
    }

    private static string BuildParseError(ColumnMappingDto mapping, DateOnly? date, string? rawLabel)
    {
        if (mapping.DateColumn == null) return "unmappedDate";
        if (date == null) return "invalidDate";
        if (mapping.LabelColumn == null) return "unmappedLabel";
        if (string.IsNullOrWhiteSpace(rawLabel)) return "missingLabel";
        if (mapping.AmountColumn == null && mapping.DebitColumn == null && mapping.CreditColumn == null) return "unmappedAmount";
        return "invalidAmount";
    }

    /// SHA-256 de (date ISO, montant à 2 décimales, libellé brut) — même définition
    /// qu'au §8 de l'overview, calculée à partir du texte brut (RawLabel/
    /// RawLabelForHash), jamais du libellé édité par l'utilisateur.
    private static string ComputeDedupHash(DateOnly date, decimal amount, string rawLabel)
    {
        var raw = $"{date:yyyy-MM-dd}|{amount:F2}|{rawLabel}";
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }
}

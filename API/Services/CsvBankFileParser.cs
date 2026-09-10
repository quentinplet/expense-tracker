using System.Globalization;
using System.Text;
using API.Errors;
using CsvHelper;
using CsvHelper.Configuration;

namespace API.Services;

public class ParsedCsvRow
{
    public required int RowNumber { get; init; }
    public required IReadOnlyDictionary<string, string> Values { get; init; }
}

public class ParsedCsvFile
{
    public required string[] Headers { get; init; }
    public required List<ParsedCsvRow> Rows { get; init; }
}

/// Encapsule CsvHelper — pas exposé au reste de l'app au-delà d'ImportService.
public static class CsvBankFileParser
{
    /// Un export bancaire réel (Crédit Agricole notamment) fait précéder l'en-tête
    /// d'un préambule — date de téléchargement, titulaire, numéro de compte, solde —
    /// avant la vraie ligne "Date;Libellé;...". On ne cherche l'en-tête que dans les
    /// premières lignes du fichier, jamais au-delà : au-delà, une ligne de donnée
    /// pourrait accidentellement ressembler à un en-tête (ex. un libellé contenant
    /// "Date").
    private const int MaxPreambleLines = 30;

    public static ParsedCsvFile Parse(byte[] content)
    {
        var text = Decode(content);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = DetectDelimiter(text),
            TrimOptions = TrimOptions.Trim,
            BadDataFound = null,
            MissingFieldFound = null,
        };

        using var reader = new StringReader(text);
        using var parser = new CsvParser(reader, config, leaveOpen: false);

        var records = new List<string[]>();
        while (parser.Read())
        {
            if (parser.Record != null) records.Add(parser.Record);
        }

        var headerIndex = records.Take(MaxPreambleLines).ToList().FindIndex(LooksLikeHeader);
        if (headerIndex < 0 || records[headerIndex] is not { Length: > 0 } headers)
            throw new ImportValidationException("Unable to parse the uploaded file as CSV.");

        var rows = new List<ParsedCsvRow>();
        for (var i = headerIndex + 1; i < records.Count; i++)
        {
            var record = records[i];

            // Ignore les lignes entièrement vides (souvent une ligne finale du fichier).
            if (record.All(string.IsNullOrWhiteSpace)) continue;

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var col = 0; col < headers.Length; col++)
            {
                values[headers[col]] = col < record.Length ? record[col] : string.Empty;
            }

            rows.Add(new ParsedCsvRow { RowNumber = i + 1, Values = values });
        }

        return new ParsedCsvFile { Headers = headers, Rows = rows };
    }

    /// Une ligne qui porte à la fois une colonne "date" et une colonne
    /// libellé/montant/débit/crédit est presque certainement l'en-tête, pas une ligne
    /// de préambule ("Solde au ...", "Compte de Dépôt n°...") ni une ligne de donnée.
    private static bool LooksLikeHeader(string[] fields)
    {
        if (fields.Length < 2) return false;

        var normalized = fields.Select(Normalize).ToList();
        var hasDate = normalized.Any(f => f.Contains("date"));
        var hasAmountOrLabel = normalized.Any(f =>
            f.Contains("libelle") || f.Contains("montant") || f.Contains("debit") ||
            f.Contains("credit") || f.Contains("description") || f.Contains("label") ||
            f.Contains("amount"));

        return hasDate && hasAmountOrLabel;
    }

    /// Minuscules, accents retirés — même normalisation que ImportService.ResolveColumn,
    /// dupliquée ici plutôt que partagée : la règle des trois du projet (§7) n'a pas
    /// encore de 3e occurrence.
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

    /// UTF-8 strict d'abord ; retombe sur Windows-1252, fréquent sur les exports
    /// bancaires FR (§14 de l'overview). Approximé par Latin-1 (ISO-8859-1, natif au
    /// runtime) plutôt que le vrai code page 1252 — évite une dépendance NuGet
    /// supplémentaire (System.Text.Encoding.CodePages) ; les deux ne divergent que sur
    /// la plage 0x80-0x9F (guillemets typographiques...), rarissime dans un relevé.
    private static string Decode(byte[] content)
    {
        try
        {
            return new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(content);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.Latin1.GetString(content);
        }
    }

    /// Le format cible est le `;` des relevés bancaires FR (§14 de l'overview), mais
    /// certains exports utilisent `,` — repli si l'en-tête ne contient aucun `;`.
    private static string DetectDelimiter(string text)
    {
        var firstLine = text.Split(['\r', '\n'], 2, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? string.Empty;
        return firstLine.Contains(';') ? ";" : ",";
    }
}

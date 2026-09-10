using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using API.Entities;

namespace API.DTOs.Requests;

public class ImportConfirmRequestDto
{
    // Le fichier original n'est jamais re-uploadé à la confirmation (§ Flux en deux
    // temps, doc feature Import CSV) : le nom vient du premier appel, porté par le
    // client jusqu'ici pour que l'historique (GET /api/import/batches) ait un libellé.
    [Required(ErrorMessage = "The Import File Name is required")]
    public string? FileName { get; set; }

    [Required(ErrorMessage = "The Import Rows are required")]
    [MinLength(1, ErrorMessage = "At least one row must be selected")]
    public List<ImportConfirmRowDto>? Rows { get; set; }
}

public class ImportConfirmRowDto
{
    [Required] public DateOnly? Date { get; set; }

    [Required]
    [Range(0.01, (double)decimal.MaxValue)]
    public decimal? Amount { get; set; }

    [Required]
    [EnumDataType(typeof(TransactionType))]
    [JsonConverter(typeof(JsonStringEnumConverter<TransactionType>))]
    public TransactionType? Type { get; set; }

    /// Libellé affiché, pré-rempli mais éditable par l'utilisateur.
    [Required]
    [StringLength(200)]
    public string? Label { get; set; }

    /// Texte brut d'origine, jamais modifié par l'UI — sert au recalcul du
    /// DedupHash côté serveur. Distinct de Label à dessein (§ Key Gotchas doc
    /// feature Import CSV) : ne jamais fusionner les deux champs.
    [Required]
    public string? RawLabelForHash { get; set; }

    public string? Note { get; set; }

    /// Null = catégorie "Other" du Type déduite côté service.
    public Guid? CategoryId { get; set; }
}

namespace API.DTOs.Requests;

// Pas de Data Annotations : IFormFile n'est pas validé par [ApiController] (§6 de
// l'overview), la présence/taille du fichier se vérifie dans le controller.
public class ImportUploadRequestDto
{
    public IFormFile? File { get; set; }

    /// Mapping manuel optionnel, renseigné seulement au second appel — après que
    /// l'utilisateur a corrigé une détection automatique incomplète. Le fichier est
    /// alors ré-uploadé avec ce mapping (voir doc feature Import CSV, la confirmation
    /// seule n'exige pas de ré-upload).
    public string? DateColumn { get; set; }
    public string? LabelColumn { get; set; }
    public string? AmountColumn { get; set; }
    public string? DebitColumn { get; set; }
    public string? CreditColumn { get; set; }
    public string? DateFormat { get; set; }
    public string? CultureName { get; set; }
}

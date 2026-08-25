using System;
using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Requests;

public class DashboardRequestDto
{
    /// <summary>Mois affiché, au format YYYY-MM.</summary>
    /// <remarks>
    /// Le serveur ne devine jamais « le mois courant » : le client l'envoie toujours.
    /// L'API tourne en UTC alors que l'utilisateur est à Paris — le 1er à 00h30 heure
    /// de Paris, UTC est encore sur le mois précédent.
    /// </remarks>
    [Required(ErrorMessage = "The month is required")]
    [RegularExpression(
        @"^\d{4}-(0[1-9]|1[0-2])$",
        ErrorMessage = "The month must use the YYYY-MM format")]
    public string Month { get; set; } = null!;

    /// L'expression régulière garantit le format : ces deux lectures ne peuvent pas échouer
    /// une fois le modèle validé.
    public int Year => int.Parse(Month[..4]);

    public int MonthNumber => int.Parse(Month[5..]);
}
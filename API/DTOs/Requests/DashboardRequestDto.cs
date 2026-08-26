using System;
using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Requests;

public class DashboardRequestDto
{
    /// <summary>Mois affiché, au format `YYYY-MM`.</summary>
    /// <remarks>
    /// Le serveur ne devine jamais « le mois courant » : le client l'envoie toujours.
    /// L'API tourne en UTC alors que l'utilisateur est à Paris — le 1er à 00h30 heure
    /// de Paris, UTC est encore sur le mois précédent.
    ///
    /// La portée « tout l'historique » n'est plus une valeur de ce paramètre : elle est
    /// devenue un réglage propre à chaque widget, et la réponse porte les deux variantes.
    /// </remarks>
    [Required(ErrorMessage = "The month is required")]
    [RegularExpression(
        @"^\d{4}-(0[1-9]|1[0-2])$",
        ErrorMessage = "The month must use the YYYY-MM format")]
    public string Month { get; set; } = null!;

    /// <summary>Premier jour du mois demandé.</summary>
    /// <remarks>
    /// Une méthode et non une propriété calculée : le ValidationVisitor de MVC lit
    /// toutes les propriétés publiques du modèle pour descendre y valider les enfants,
    /// et il le fait avant que l'échec de validation ne soit constaté. Une propriété
    /// qui découpe `Month` lèverait donc sur une valeur nulle ou mal formée, produisant
    /// un 500 là où le contrat annonce un 400.
    /// </remarks>
    public DateOnly ResolveMonth() =>
        new(int.Parse(Month[..4]), int.Parse(Month[5..]), 1);
}
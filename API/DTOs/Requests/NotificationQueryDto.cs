using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Requests;

/// Paramètres de `GET /api/notifications?take=`, même logique que
/// `BudgetQueryDto` : un `int` brut sur l'action laissait passer `take=-1`
/// jusqu'au `LIMIT` Postgres généré par EF Core, qui refuse une valeur
/// négative et renvoie un 500 au lieu d'un 400.
public class NotificationQueryDto
{
    [Range(1, 100, ErrorMessage = "take must be between 1 and 100.")]
    public int Take { get; set; } = 20;
}

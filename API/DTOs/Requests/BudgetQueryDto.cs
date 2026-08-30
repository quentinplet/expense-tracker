using System;
using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Requests;

/// Paramètres de `GET /api/budgets?month=`, même format et même validation que
/// `DashboardRequestDto` — le serveur ne devine jamais le mois courant.
public class BudgetQueryDto
{
    [Required(ErrorMessage = "The month is required")]
    [RegularExpression(
        @"^\d{4}-(0[1-9]|1[0-2])$",
        ErrorMessage = "The month must use the YYYY-MM format")]
    public string Month { get; set; } = null!;
}
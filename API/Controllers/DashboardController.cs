using System;
using API.DTOs.Requests;
using API.DTOs.Responses;
using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
public class DashboardController(IDashboardService dashboardService) : BaseApiController
{
    /// <summary>GET /api/dashboard?month=2026-08</summary>
    /// <remarks>
    /// Un seul appel pour tout l'écran : quatre endpoints, ce serait quatre allers-retours,
    /// quatre états de chargement, et une fenêtre pendant laquelle une transaction créée
    /// en cours de séquence ferait diverger les widgets entre eux.
    /// </remarks>
    [HttpGet]
    public async Task<ActionResult<DashboardResponseDto>> GetDashboardData(
        [FromQuery] DashboardRequestDto request)
    {
        var userId = User.GetMemberId();

        return Ok(await dashboardService.GetDashboardDataAsync(
            userId, request.Year, request.MonthNumber));
    }
}
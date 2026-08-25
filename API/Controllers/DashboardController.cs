using System;
using API.DTOs.Requests;
using API.DTOs.Responses;
using API.Extensions;
using API.Interfaces;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
public class DashboardController(IDashboardService dashboardService) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<DashboardResponseDto>> GetDashboardData([FromQuery] DashboardRequestDto requestDto)
    {
        var userId = User.GetMemberId();
        var dashboardData = await dashboardService.GetDashboardDataAsync(userId, requestDto.Month, requestDto.Year);
        return Ok(dashboardData);
    }

}

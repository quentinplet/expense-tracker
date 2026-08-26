using System;
using API.DTOs.Requests;
using API.DTOs.Responses;

namespace API.Interfaces;

public interface IDashboardService
{
    Task<DashboardResponseDto> GetDashboardDataAsync(Guid userId, DashboardRequestDto request);

}

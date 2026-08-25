using System;
using API.DTOs.Responses;

namespace API.Interfaces;

public interface IDashboardService
{
    Task<DashboardResponseDto> GetDashboardDataAsync(Guid userId, int year, int month);

}

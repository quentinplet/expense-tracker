using System;
using API.DTOs.Responses;

namespace API.Interfaces;

public interface IDashboardService
{
    Task<DashboardResponseDto> GetDashboardDataAsync(string userId, int month, int year);

}

using System;
using API.DTOs.Requests;
using API.DTOs.Responses;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
public class BudgetsController(IUnitOfWork uow) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<List<BudgetResponseDto>>> GetAllBudgets()
    {
        var userId = User.GetMemberId();

        var budgets = await uow.BudgetRepository.GetAllByUserIdAsync(userId);
        return Ok(budgets.Select(b => b.ToBudgetResponseDto()));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BudgetResponseDto>> GetBudgetById(int id)
    {
        var userId = User.GetMemberId();

        if (userId == null) return Unauthorized();
        var budget = await uow.BudgetRepository.GetByIdAsync(id);
        if (budget == null) return NotFound();
        if (budget.UserId != userId) return Forbid();
        return Ok(budget.ToBudgetResponseDto());
    }

    [HttpPost]
    public async Task<ActionResult<BudgetResponseDto>> CreateBudget([FromBody] BudgetRequestDto dto)
    {
        var userId = User.GetMemberId();
        if (userId == null) return Unauthorized();
        var budget = new Budget
        {
            Amount = dto.Amount,
            Month = dto.Month,
            Year = dto.Year,
            CategoryId = dto.CategoryId,
            UserId = userId
        };
        uow.BudgetRepository.Add(budget);
        if (await uow.Complete()) return CreatedAtAction(nameof(GetBudgetById), new { id = budget.Id }, budget.ToBudgetResponseDto());
        return BadRequest("Failed to create budget");
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateBudget(int id, [FromBody] BudgetRequestDto dto)
    {
        var userId = User.GetMemberId();
        if (userId == null) return Unauthorized();
        var budget = await uow.BudgetRepository.GetByIdAsync(id);
        if (budget == null) return NotFound();
        if (budget.UserId != userId) return Forbid();

        budget.Amount = dto.Amount;
        budget.Month = dto.Month;
        budget.Year = dto.Year;
        budget.CategoryId = dto.CategoryId;
        uow.BudgetRepository.Update(budget);
        if (await uow.Complete()) return NoContent();
        return BadRequest("Failed to update budget");
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteBudget(int id)
    {
        var userId = User.GetMemberId();
        if (userId == null) return Unauthorized();
        var budget = await uow.BudgetRepository.GetByIdAsync(id);
        if (budget == null) return NotFound();
        if (budget.UserId != userId) return Forbid();
        uow.BudgetRepository.Delete(budget);
        if (await uow.Complete()) return NoContent();
        return BadRequest("Failed to delete budget");
    }
}

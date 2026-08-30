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
public class BudgetsController(IUnitOfWork uow, IBudgetService budgetService) : BaseApiController
{
    /// GET /api/budgets?month=2026-08 — le budget global d'abord s'il existe, puis
    /// les budgets catégorie, chacun avec Spent/Remaining calculés en un aller-retour.
    [HttpGet]
    public async Task<ActionResult<List<BudgetResponseDto>>> GetAllBudgets([FromQuery] BudgetQueryDto query)
    {
        var userId = User.GetMemberId();
        return Ok(await budgetService.GetBudgetsForMonthAsync(userId, query.Month));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BudgetResponseDto>> GetBudgetById(Guid id)
    {
        var userId = User.GetMemberId();

        var budget = await uow.BudgetRepository.GetByIdAsync(id);
        if (budget == null) return NotFound();
        if (budget.UserId != userId) return Forbid();

        var spent = await budgetService.GetSpentAsync(userId, budget);
        return Ok(budget.ToBudgetResponseDto(spent));
    }

    [HttpPost]
    public async Task<ActionResult<BudgetResponseDto>> CreateBudget([FromBody] BudgetRequestDto dto)
    {
        var userId = User.GetMemberId();

        // Validation croisée uniquement si une catégorie est fournie : un budget
        // global n'a rien à charger ni à valider.
        if (dto.CategoryId.HasValue)
        {
            var category = await uow.CategoryRepository.GetByIdAsync(dto.CategoryId.Value);
            if (category == null) return NotFound();
            if (category.UserId != userId) return Forbid();
            if (category.Type != TransactionType.Expense) return BadRequest("Budgets can only target expense categories.");
        }

        if (await uow.BudgetRepository.ExistsAsync(userId, dto.Month!, dto.CategoryId))
            return BadRequest(dto.CategoryId.HasValue
                ? "A budget already exists for this category and month."
                : "A global budget already exists for this month.");

        var budget = new Budget
        {
            AmountLimit = dto.AmountLimit!.Value,
            Month = dto.Month!,
            AutoRenew = dto.AutoRenew!.Value,
            CategoryId = dto.CategoryId,
            UserId = userId
        };
        uow.BudgetRepository.Add(budget);
        if (!await uow.Complete()) return BadRequest("Failed to create budget");

        // Rien à dépenser sur un budget qui vient d'être créé.
        return CreatedAtAction(nameof(GetBudgetById), new { id = budget.Id }, budget.ToBudgetResponseDto(0));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<BudgetResponseDto>> UpdateBudget(Guid id, [FromBody] BudgetRequestDto dto)
    {
        var userId = User.GetMemberId();
        var budget = await uow.BudgetRepository.GetByIdAsync(id);
        if (budget == null) return NotFound();
        if (budget.UserId != userId) return Forbid();

        // CategoryId et Month sont immuables après création : reçus dans le DTO
        // (partagé POST/PUT), mais toujours ignorés ici.
        budget.AmountLimit = dto.AmountLimit!.Value;
        budget.AutoRenew = dto.AutoRenew!.Value;

        uow.BudgetRepository.Update(budget);
        if (!await uow.Complete()) return BadRequest("Failed to update budget");

        var spent = await budgetService.GetSpentAsync(userId, budget);
        return Ok(budget.ToBudgetResponseDto(spent));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteBudget(Guid id)
    {
        var userId = User.GetMemberId();
        var budget = await uow.BudgetRepository.GetByIdAsync(id);
        if (budget == null) return NotFound();
        if (budget.UserId != userId) return Forbid();

        uow.BudgetRepository.Delete(budget);
        if (await uow.Complete()) return NoContent();
        return BadRequest("Failed to delete budget");
    }
}
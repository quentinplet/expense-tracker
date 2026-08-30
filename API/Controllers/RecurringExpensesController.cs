using API.DTOs.Requests;
using API.DTOs.Responses;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[Route("api/recurring-expenses")]
public class RecurringExpensesController(IUnitOfWork uow) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<List<RecurringExpenseResponseDto>>> GetAll()
    {
        var userId = User.GetMemberId();
        var expenses = await uow.RecurringExpenseRepository.GetAllAsync(userId);
        return Ok(expenses.Select(e => e.ToRecurringExpenseResponseDto()));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RecurringExpenseResponseDto>> GetById(Guid id)
    {
        var userId = User.GetMemberId();
        var expense = await uow.RecurringExpenseRepository.GetByIdAsync(id);
        if (expense == null) return NotFound();
        if (expense.UserId != userId) return Forbid();
        return Ok(expense.ToRecurringExpenseResponseDto());
    }

    [HttpPost]
    public async Task<ActionResult<RecurringExpenseResponseDto>> Create([FromBody] RecurringExpenseRequestDto dto)
    {
        var userId = User.GetMemberId();

        var category = await uow.CategoryRepository.GetByIdAsync(dto.CategoryId!.Value);
        if (category == null) return NotFound();
        if (category.UserId != userId) return Forbid();
        if (category.Type != dto.Type!.Value) return BadRequest("The category does not match the recurring expense type.");

        var expense = new RecurringExpense
        {
            Label = dto.Label!,
            Amount = dto.Amount!.Value,
            Type = dto.Type.Value,
            Frequency = dto.Frequency!.Value,
            NextDueDate = dto.NextDueDate!.Value,
            Active = dto.Active!.Value,
            CategoryId = dto.CategoryId.Value,
            UserId = userId,
        };

        uow.RecurringExpenseRepository.Add(expense);
        if (!await uow.Complete()) return BadRequest("Failed to create recurring expense");

        // Recharger avec la catégorie incluse
        var created = await uow.RecurringExpenseRepository.GetByIdAsync(expense.Id);
        if (created == null) return BadRequest("Failed to retrieve created recurring expense");
        return CreatedAtAction(nameof(GetById), new { id = expense.Id }, created.ToRecurringExpenseResponseDto());
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<RecurringExpenseResponseDto>> Update(Guid id, [FromBody] RecurringExpenseRequestDto dto)
    {
        var userId = User.GetMemberId();
        var expense = await uow.RecurringExpenseRepository.GetByIdAsync(id);
        if (expense == null) return NotFound();
        if (expense.UserId != userId) return Forbid();

        // Type est immuable après création : reçu dans le DTO (partagé POST/PUT),
        // mais toujours ignoré ici. CategoryId, lui, reste modifiable — revalidé
        // contre le Type existant (immuable) de la charge, pas contre dto.Type.
        var category = await uow.CategoryRepository.GetByIdAsync(dto.CategoryId!.Value);
        if (category == null) return NotFound();
        if (category.UserId != userId) return Forbid();
        if (category.Type != expense.Type) return BadRequest("The category does not match the recurring expense type.");

        expense.Label = dto.Label!;
        expense.Amount = dto.Amount!.Value;
        expense.Frequency = dto.Frequency!.Value;
        expense.NextDueDate = dto.NextDueDate!.Value;
        expense.Active = dto.Active!.Value;
        expense.CategoryId = dto.CategoryId.Value;

        uow.RecurringExpenseRepository.Update(expense);
        if (!await uow.Complete()) return BadRequest("Failed to update recurring expense");

        var updated = await uow.RecurringExpenseRepository.GetByIdAsync(expense.Id);
        if (updated == null) return BadRequest("Failed to retrieve updated recurring expense");
        return Ok(updated.ToRecurringExpenseResponseDto());
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var userId = User.GetMemberId();
        var expense = await uow.RecurringExpenseRepository.GetByIdAsync(id);
        if (expense == null) return NotFound();
        if (expense.UserId != userId) return Forbid();

        // Les transactions déjà générées ne sont jamais supprimées — seule leur
        // référence (RecurringExpenseId) est détachée, via OnDelete(SetNull) sur
        // la relation (AppDbContext).
        uow.RecurringExpenseRepository.Delete(expense);
        if (await uow.Complete()) return NoContent();
        return BadRequest("Failed to delete recurring expense");
    }
}

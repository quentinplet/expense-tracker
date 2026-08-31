using API.DTOs.Requests;
using API.DTOs.Responses;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[Route("api/recurring-transactions")]
public class RecurringTransactionsController(IUnitOfWork uow) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<List<RecurringTransactionResponseDto>>> GetAll()
    {
        var userId = User.GetMemberId();
        var recurringTransactions = await uow.RecurringTransactionRepository.GetAllAsync(userId);
        return Ok(recurringTransactions.Select(e => e.ToRecurringTransactionResponseDto()));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RecurringTransactionResponseDto>> GetById(Guid id)
    {
        var userId = User.GetMemberId();
        var recurringTransaction = await uow.RecurringTransactionRepository.GetByIdAsync(id);
        if (recurringTransaction == null) return NotFound();
        if (recurringTransaction.UserId != userId) return Forbid();
        return Ok(recurringTransaction.ToRecurringTransactionResponseDto());
    }

    [HttpPost]
    public async Task<ActionResult<RecurringTransactionResponseDto>> Create([FromBody] RecurringTransactionRequestDto dto)
    {
        var userId = User.GetMemberId();

        var category = await uow.CategoryRepository.GetByIdAsync(dto.CategoryId!.Value);
        if (category == null) return NotFound();
        if (category.UserId != userId) return Forbid();
        if (category.Type != dto.Type!.Value) return BadRequest("The category does not match the recurring transaction type.");

        var recurringTransaction = new RecurringTransaction
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

        uow.RecurringTransactionRepository.Add(recurringTransaction);
        if (!await uow.Complete()) return BadRequest("Failed to create recurring transaction");

        // Recharger avec la catégorie incluse
        var created = await uow.RecurringTransactionRepository.GetByIdAsync(recurringTransaction.Id);
        if (created == null) return BadRequest("Failed to retrieve created recurring transaction");
        return CreatedAtAction(nameof(GetById), new { id = recurringTransaction.Id }, created.ToRecurringTransactionResponseDto());
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<RecurringTransactionResponseDto>> Update(Guid id, [FromBody] RecurringTransactionRequestDto dto)
    {
        var userId = User.GetMemberId();
        var recurringTransaction = await uow.RecurringTransactionRepository.GetByIdAsync(id);
        if (recurringTransaction == null) return NotFound();
        if (recurringTransaction.UserId != userId) return Forbid();

        // Type est immuable après création : reçu dans le DTO (partagé POST/PUT),
        // mais toujours ignoré ici. CategoryId, lui, reste modifiable — revalidé
        // contre le Type existant (immuable) de la charge, pas contre dto.Type.
        var category = await uow.CategoryRepository.GetByIdAsync(dto.CategoryId!.Value);
        if (category == null) return NotFound();
        if (category.UserId != userId) return Forbid();
        if (category.Type != recurringTransaction.Type) return BadRequest("The category does not match the recurring transaction type.");

        recurringTransaction.Label = dto.Label!;
        recurringTransaction.Amount = dto.Amount!.Value;
        recurringTransaction.Frequency = dto.Frequency!.Value;
        recurringTransaction.NextDueDate = dto.NextDueDate!.Value;
        recurringTransaction.Active = dto.Active!.Value;
        recurringTransaction.CategoryId = dto.CategoryId.Value;

        uow.RecurringTransactionRepository.Update(recurringTransaction);
        if (!await uow.Complete()) return BadRequest("Failed to update recurring transaction");

        var updated = await uow.RecurringTransactionRepository.GetByIdAsync(recurringTransaction.Id);
        if (updated == null) return BadRequest("Failed to retrieve updated recurring transaction");
        return Ok(updated.ToRecurringTransactionResponseDto());
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var userId = User.GetMemberId();
        var recurringTransaction = await uow.RecurringTransactionRepository.GetByIdAsync(id);
        if (recurringTransaction == null) return NotFound();
        if (recurringTransaction.UserId != userId) return Forbid();

        // Les transactions déjà générées ne sont jamais supprimées — seule leur
        // référence (RecurringTransactionId) est détachée, via OnDelete(SetNull) sur
        // la relation (AppDbContext).
        uow.RecurringTransactionRepository.Delete(recurringTransaction);
        if (await uow.Complete()) return NoContent();
        return BadRequest("Failed to delete recurring transaction");
    }
}
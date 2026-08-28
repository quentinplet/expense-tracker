using System;
using API.DTOs.Requests;
using API.DTOs.Responses;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
public class TransactionsController(IUnitOfWork uow) : BaseApiController
{

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<TransactionResponseDto>>> GetAllTransactions([FromQuery] TransactionParams transactionParams)
    {
        var userId = User.GetMemberId();
        transactionParams.CurrentUserId = userId;
        var result = await uow.TransactionRepository.GetAllTransactionsByUserIdAsync(transactionParams);
        return Ok(new PaginatedResult<TransactionResponseDto>
        {
            Metadata = result.Metadata,
            Items = [.. result.Items.Select(t => t.ToTransactionResponseDto())]
        });
    }

    [HttpGet("type/{transactionType}")]
    public async Task<ActionResult<List<TransactionResponseDto>>> GetTransactionsByType(TransactionType transactionType)
    {
        var userId = User.GetMemberId();

        var transactions = await uow.TransactionRepository.GetTransactionsByTypeAsync(userId, transactionType);
        return Ok(transactions.Select(t => t.ToTransactionResponseDto()));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionResponseDto>> GetTransactionById(Guid id)
    {
        var userId = User.GetMemberId();
        var transaction = await uow.TransactionRepository.GetTransactionByIdAsync(id);
        if (transaction == null) return NotFound();
        if (transaction.UserId != userId) return Forbid();
        return Ok(transaction.ToTransactionResponseDto());
    }

    [HttpPost]
    public async Task<ActionResult<TransactionResponseDto>> CreateTransaction([FromBody] TransactionRequestDto dto)
    {
        var userId = User.GetMemberId();

        var category = await uow.CategoryRepository.GetByIdAsync(dto.CategoryId!.Value);
        if (category == null) return NotFound();
        if (category.UserId != userId) return Forbid();
        if (category.Type != dto.Type!.Value) return BadRequest("The category does not match the transaction type.");

        var transaction = dto.ToEntity(userId);

        uow.TransactionRepository.AddTransaction(transaction);
        if (await uow.Complete())
        {
            // Recharger avec la catégorie incluse
            var created = await uow.TransactionRepository.GetTransactionByIdAsync(transaction.Id);
            if (created == null) return BadRequest("Failed to retrieve created transaction");
            return CreatedAtAction(nameof(GetTransactionById), new { id = transaction.Id }, created.ToTransactionResponseDto());
        }
        return BadRequest("Failed to create transaction");
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateTransaction(Guid id, TransactionRequestDto dto)
    {
        var userId = User.GetMemberId();
        var transaction = await uow.TransactionRepository.GetTransactionByIdAsync(id);
        if (transaction == null) return NotFound();
        if (transaction.UserId != userId) return Forbid();

        var category = await uow.CategoryRepository.GetByIdAsync(dto.CategoryId!.Value);
        if (category == null) return NotFound();
        if (category.UserId != userId) return Forbid();
        if (category.Type != dto.Type!.Value) return BadRequest("The category does not match the transaction type.");

        dto.Apply(transaction);
        uow.TransactionRepository.UpdateTransaction(transaction);

        if (await uow.Complete())
        {
            // Recharger avec la catégorie incluse
            var updated = await uow.TransactionRepository.GetTransactionByIdAsync(transaction.Id);
            if (updated == null) return BadRequest("Failed to retrieve updated transaction");
            return Ok(updated.ToTransactionResponseDto());
        }
        ;
        return BadRequest("Failed to update transaction");
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTransaction(Guid id)
    {
        var userId = User.GetMemberId();
        var transaction = await uow.TransactionRepository.GetTransactionByIdAsync(id);
        if (transaction == null) return NotFound();
        if (transaction.UserId != userId) return Forbid();

        uow.TransactionRepository.DeleteTransaction(transaction);
        if (await uow.Complete()) return NoContent();
        return BadRequest("Failed to delete transaction");
    }

    [HttpDelete]
    public async Task<ActionResult<int>> DeleteTransactions([FromBody] List<Guid> ids)
    {
        var userId = User.GetMemberId();
        var transactions = await uow.TransactionRepository.GetTransactionsByIdsAsync(ids, userId);
        if (transactions.Count == 0) return NotFound();
        if (transactions.Any(t => t.UserId != userId)) return Forbid();

        uow.TransactionRepository.DeleteTransactions(transactions);
        // Le nombre réellement supprimé peut être inférieur à ids.Count (un id déjà
        // supprimé ailleurs est silencieusement ignoré par GetTransactionsByIdsAsync) :
        // le client en a besoin pour ne jamais deviner un compte.
        if (await uow.Complete()) return Ok(transactions.Count);
        return BadRequest("Failed to delete transactions");
    }

}

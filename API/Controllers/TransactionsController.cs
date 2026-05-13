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
    public async Task<ActionResult<List<TransactionResponseDto>>> GetAllTransactions([FromQuery] TransactionParams transactionParams)
    {
        var userId = User.GetMemberId();
        var result = await uow.TransactionRepository.GetAllTransactionsByUserIdAsync(transactionParams);
        return Ok(new PaginatedResult<TransactionResponseDto>
        {
            Metadata = result.Metadata,
            Items = [.. result.Items.Select(t => t.ToTransactionResponseDto())]
        });
    }

    [HttpGet("type/{transactionType}")]
    public async Task<ActionResult<List<TransactionResponseDto>>> GetTransactionsByType(TransactionTypeName transactionType)
    {
        var userId = User.GetMemberId();

        var transactions = await uow.TransactionRepository.GetTransactionsByTypeAsync(userId, transactionType);
        return Ok(transactions.Select(t => t.ToTransactionResponseDto()));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionResponseDto>> GetTransactionById(int id)
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

        var transaction = new Transaction
        {
            Amount = dto.Amount!.Value,
            Date = dto.Date!.Value,
            Description = dto.Description,
            CategoryId = dto.CategoryId!.Value,
            UserId = userId
        };

        uow.TransactionRepository.AddTransaction(transaction);
        if (await uow.Complete()) return CreatedAtAction(nameof(GetTransactionById), new { id = transaction.Id }, transaction.ToTransactionResponseDto());
        return BadRequest("Failed to create transaction");
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateTransaction(int id, TransactionRequestDto dto)
    {
        var userId = User.GetMemberId();
        var transaction = await uow.TransactionRepository.GetTransactionByIdAsync(id);
        if (transaction == null) return NotFound();
        if (transaction.UserId != userId) return Forbid();

        transaction.Amount = dto.Amount!.Value;
        transaction.Date = dto.Date!.Value;
        transaction.Description = dto.Description;
        transaction.CategoryId = dto.CategoryId!.Value;
        uow.TransactionRepository.UpdateTransaction(transaction);
        if (await uow.Complete()) return NoContent();
        return BadRequest("Failed to update transaction");
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTransaction(int id)
    {
        var userId = User.GetMemberId();
        var transaction = await uow.TransactionRepository.GetTransactionByIdAsync(id);
        if (transaction == null) return NotFound();
        if (transaction.UserId != userId) return Forbid();

        uow.TransactionRepository.DeleteTransaction(transaction);
        if (await uow.Complete()) return NoContent();
        return BadRequest("Failed to delete transaction");
    }

}

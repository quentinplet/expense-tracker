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
public class TransactionsController(ITransactionRepository transactionRepository) : BaseApiController
{

    [HttpGet]
    public async Task<ActionResult<List<TransactionResponseDto>>> GetAllTransactions()
    {
        var userId = User.GetMemberId();

        var transactions = await transactionRepository.GetAllTransactionsByUserIdAsync(userId);
        return Ok(transactions.Select(t => t.ToTransactionResponseDto()));
    }

    [HttpGet("type/{transactionType}")]
    public async Task<ActionResult<List<TransactionResponseDto>>> GetTransactionsByType(TransactionTypeName transactionType)
    {
        var userId = User.GetMemberId();

        var transactions = await transactionRepository.GetTransactionsByTypeAsync(userId, transactionType);
        return Ok(transactions.Select(t => t.ToTransactionResponseDto()));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionResponseDto>> GetTransactionById(int id)
    {
        var userId = User.GetMemberId();
        var transaction = await transactionRepository.GetTransactionByIdAsync(id);
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

        transactionRepository.AddTransaction(transaction);
        if (await transactionRepository.SaveAllAsync()) return CreatedAtAction(nameof(GetTransactionById), new { id = transaction.Id }, transaction.ToTransactionResponseDto());
        return BadRequest("Failed to create transaction");
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateTransaction(int id, TransactionRequestDto dto)
    {
        var userId = User.GetMemberId();
        var transaction = await transactionRepository.GetTransactionByIdAsync(id);
        if (transaction == null) return NotFound();
        if (transaction.UserId != userId) return Forbid();

        transaction.Amount = dto.Amount!.Value;
        transaction.Date = dto.Date!.Value;
        transaction.Description = dto.Description;
        transaction.CategoryId = dto.CategoryId!.Value;
        transactionRepository.UpdateTransaction(transaction);
        if (await transactionRepository.SaveAllAsync()) return NoContent();
        return BadRequest("Failed to update transaction");
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTransaction(int id)
    {
        var userId = User.GetMemberId();
        var transaction = await transactionRepository.GetTransactionByIdAsync(id);
        if (transaction == null) return NotFound();
        if (transaction.UserId != userId) return Forbid();

        transactionRepository.DeleteTransaction(transaction);
        if (await transactionRepository.SaveAllAsync()) return NoContent();
        return BadRequest("Failed to delete transaction");
    }

}

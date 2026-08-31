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
public class CategoriesController(IUnitOfWork uow) : BaseApiController
{
    [HttpGet] // api/category?type=Expense
    public async Task<ActionResult<List<CategoryResponseDto>>> GetAll([FromQuery] TransactionType? type)
    {
        var categories = await uow.CategoryRepository.GetAllAsync(User.GetMemberId(), type);
        return Ok(categories.Select(c => c.ToCategoryResponseDto()));
    }

    [HttpGet("{id}")] // api/category/5
    public async Task<ActionResult<CategoryResponseDto>> GetById(Guid id)
    {
        var userId = User.GetMemberId();
        var category = await uow.CategoryRepository.GetByIdAsync(id);
        if (category == null) return NotFound();
        if (category.UserId != userId) return Forbid();
        return Ok(category.ToCategoryResponseDto());
    }

    [HttpPost] // api/category
    public async Task<ActionResult<CategoryResponseDto>> Create(CategoryRequestDto categoryRequestDto)
    {
        var userId = User.GetMemberId();

        if (await uow.CategoryRepository.ExistsAsync(userId, categoryRequestDto.Name!, categoryRequestDto.Type!.Value))
            return BadRequest("A category with this name already exists for this type.");

        var category = new Category
        {
            Name = categoryRequestDto.Name!,
            Icon = categoryRequestDto.Icon!,
            Color = categoryRequestDto.Color!,
            Type = categoryRequestDto.Type.Value,
            UserId = userId,
            IsLocked = false,
            Enabled = true
        };

        uow.CategoryRepository.Add(category);
        var categoryResponseDto = category.ToCategoryResponseDto();
        if (await uow.Complete()) return CreatedAtAction(nameof(GetById), new { id = category.Id }, categoryResponseDto);
        return BadRequest("Failed to create category");
    }

    [HttpPut("{id}")] // api/category/5
    public async Task<ActionResult<CategoryResponseDto>> Update(Guid id, CategoryRequestDto categoryRequestDto)
    {
        var userId = User.GetMemberId();
        var category = await uow.CategoryRepository.GetByIdAsync(id);
        if (category == null) return NotFound();
        if (category.UserId != userId) return Forbid();

        if (category.Name != categoryRequestDto.Name &&
            await uow.CategoryRepository.ExistsAsync(userId, categoryRequestDto.Name!, category.Type, id))
            return BadRequest("A category with this name already exists for this type.");

        // Type est immuable après création : reçu dans le DTO (partagé POST/PUT),
        // mais toujours ignoré ici, jamais validé contre l'ancienne valeur.
        if (category.Name != categoryRequestDto.Name) category.TranslationKey = null;
        category.Name = categoryRequestDto.Name!;
        category.Icon = categoryRequestDto.Icon!;
        category.Color = categoryRequestDto.Color!;

        uow.CategoryRepository.Update(category);
        if (await uow.Complete()) return Ok(category.ToCategoryResponseDto());
        return BadRequest("Failed to update category");
    }

    [HttpPatch("{id}/toggle")] // api/category/5/toggle
    public async Task<ActionResult> ToggleEnabled(Guid id)
    {
        var userId = User.GetMemberId();
        var category = await uow.CategoryRepository.GetByIdAsync(id);
        if (category == null) return NotFound();
        if (category.UserId != userId) return Forbid();
        category.Enabled = !category.Enabled;
        uow.CategoryRepository.Update(category);
        if (await uow.Complete()) return NoContent();
        return BadRequest("Failed to toggle category");
    }

    [HttpDelete("{id}")] // api/category/5
    public async Task<ActionResult<CategoryDeletedDto>> Delete(Guid id)
    {
        var userId = User.GetMemberId();
        var category = await uow.CategoryRepository.GetByIdAsync(id);
        if (category == null) return NotFound();
        if (category.UserId != userId) return Forbid();
        if (category.IsLocked) return Forbid();

        var transactions = await uow.TransactionRepository.GetByCategoryIdAsync(userId, id);
        var budgets = await uow.BudgetRepository.GetByCategoryIdAsync(userId, id);
        var recurringTransactions = await uow.RecurringTransactionRepository.GetByCategoryIdAsync(userId, id);

        if (transactions.Count > 0 || budgets.Count > 0 || recurringTransactions.Count > 0)
        {
            var other = await uow.CategoryRepository.GetLockedByTypeAsync(userId, category.Type)
                ?? throw new InvalidOperationException($"No locked category found for user {userId} and type {category.Type}.");

            foreach (var transaction in transactions)
            {
                transaction.CategoryId = other.Id;
                uow.TransactionRepository.UpdateTransaction(transaction);
            }

            foreach (var budget in budgets)
            {
                // Réaffecter vers "Other" violerait l'index unique partiel
                // (UserId, Month, CategoryId) si "Other" a déjà son propre budget
                // ce mois-ci — un budget n'a rien d'historique à préserver comme
                // une transaction, donc on le supprime plutôt que de planter sur
                // une contrainte au SaveChanges.
                if (await uow.BudgetRepository.ExistsAsync(userId, budget.Month, other.Id))
                {
                    uow.BudgetRepository.Delete(budget);
                }
                else
                {
                    budget.CategoryId = other.Id;
                    uow.BudgetRepository.Update(budget);
                }
            }

            // Contrairement aux budgets, plusieurs transactions récurrentes peuvent
            // partager "Other" sans contrainte d'unicité — une réaffectation
            // directe suffit, comme pour les transactions.
            foreach (var recurringTransaction in recurringTransactions)
            {
                recurringTransaction.CategoryId = other.Id;
                uow.RecurringTransactionRepository.Update(recurringTransaction);
            }
        }

        uow.CategoryRepository.Delete(category);

        if (await uow.Complete())
            return Ok(new CategoryDeletedDto { ReassignedTransactionCount = transactions.Count });

        return BadRequest("Failed to delete category");
    }

}

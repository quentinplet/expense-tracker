using System;
using API.DTOs.Requests;
using API.DTOs.Responses;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class CategoriesController(IUnitOfWork uow) : BaseApiController
{
    [HttpGet] // api/category
    public async Task<ActionResult<List<CategoryResponseDto>>> GetAll()
    {
        var categories = await uow.CategoryRepository.GetAllAsync();
        return Ok(categories.Select(c => c.ToCategoryResponseDto()));
    }

    [HttpGet("type/{transactionTypeName}")] // api/category/by-type/Expense
    public async Task<ActionResult<List<CategoryResponseDto>>> GetByType(TransactionTypeName transactionTypeName)
    {
        var categories = await uow.CategoryRepository.GetByTypeAsync(transactionTypeName);
        return Ok(categories.Select(c => c.ToCategoryResponseDto()));
    }

    [HttpGet("{id}")] // api/category/5
    public async Task<ActionResult<CategoryResponseDto>> GetById(int id)
    {
        var category = await uow.CategoryRepository.GetByIdAsync(id);
        if (category == null) return NotFound();
        return Ok(category.ToCategoryResponseDto());
    }

    [HttpPost] // api/category
    public async Task<ActionResult<CategoryResponseDto>> Create(CategoryRequestDto categoryRequestDto)
    {

        var transactionType = await uow.CategoryRepository.GetTransactionTypeByNameAsync(categoryRequestDto.TransactionType);
        if (transactionType == null)
        {
            return BadRequest($"Transaction type '{categoryRequestDto.TransactionType}' does not exist.");
        }

        var category = new Category
        {
            Name = categoryRequestDto.Name,
            TransactionTypeId = transactionType.Id,
            Enabled = true
        };

        uow.CategoryRepository.Add(category);
        var categoryResponseDto = category.ToCategoryResponseDto();
        if (await uow.Complete()) return CreatedAtAction(nameof(GetById), new { id = category.Id }, categoryResponseDto);
        return BadRequest("Failed to create category");
    }

    [HttpPut("{id}")] // api/category/5
    public async Task<ActionResult> Update(int id, CategoryRequestDto categoryRequestDto)
    {
        var category = await uow.CategoryRepository.GetByIdAsync(id);
        if (category == null) return NotFound();

        var transactionType = await uow.CategoryRepository.GetTransactionTypeByNameAsync(categoryRequestDto.TransactionType);
        if (transactionType == null)

        {
            return BadRequest($"Transaction type '{categoryRequestDto.TransactionType}' does not exist.");
        }

        category.Name = categoryRequestDto.Name;
        category.TransactionTypeId = transactionType.Id;

        uow.CategoryRepository.Update(category);
        if (await uow.Complete()) return NoContent();
        return BadRequest("Failed to update category");
    }

    [HttpPatch("{id}/toggle")] // api/category/5/toggle
    public async Task<ActionResult> ToggleEnabled(int id)
    {
        var category = await uow.CategoryRepository.GetByIdAsync(id);
        if (category == null) return NotFound();
        category.Enabled = !category.Enabled;
        uow.CategoryRepository.Update(category);
        if (await uow.Complete()) return NoContent();
        return BadRequest("Failed to toggle category");
    }

    [HttpDelete("{id}")] // api/category/5
    public async Task<ActionResult> Delete(int id)
    {
        var category = await uow.CategoryRepository.GetByIdAsync(id);
        if (category == null) return NotFound();
        uow.CategoryRepository.Delete(category);
        if (await uow.Complete()) return NoContent();
        return BadRequest("Failed to delete category");
    }

}

using API.DTOs.Requests;
using API.DTOs.Responses;
using API.Errors;
using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
public class ImportController(IUnitOfWork uow, IImportService importService, IConfiguration configuration) : BaseApiController
{
    [HttpPost("csv")]
    public async Task<ActionResult<ImportPreviewDto>> Preview([FromForm] ImportUploadRequestDto dto)
    {
        if (dto.File == null || dto.File.Length == 0) return BadRequest("The Import File is required");

        var maxFileSizeMb = configuration.GetValue("Import:MaxFileSizeMb", 5);
        if (dto.File.Length > maxFileSizeMb * 1024 * 1024)
            return BadRequest($"The Import File must be {maxFileSizeMb} MB or smaller");

        try
        {
            var userId = User.GetMemberId();
            var preview = await importService.PreviewAsync(userId, dto);
            return Ok(preview);
        }
        catch (ImportValidationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("csv/confirm")]
    public async Task<ActionResult<ImportConfirmResultDto>> Confirm([FromBody] ImportConfirmRequestDto dto)
    {
        try
        {
            var userId = User.GetMemberId();
            var result = await importService.ConfirmAsync(userId, dto);
            return Ok(result);
        }
        catch (ImportValidationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("batches")]
    public async Task<ActionResult<List<ImportBatchDto>>> GetBatches()
    {
        var userId = User.GetMemberId();
        var batches = await uow.ImportBatchRepository.GetAllAsync(userId);
        return Ok(batches.Select(b => b.ToImportBatchDto()));
    }

    [HttpDelete("batches/{id}")]
    public async Task<ActionResult> DeleteBatch(Guid id)
    {
        var userId = User.GetMemberId();
        var batch = await uow.ImportBatchRepository.GetByIdAsync(id);
        if (batch == null) return NotFound();
        if (batch.UserId != userId) return Forbid();

        // Les transactions du batch disparaissent en cascade (OnDelete Cascade sur
        // Transaction.ImportBatchId, AppDbContext) — pas de suppression manuelle ici.
        uow.ImportBatchRepository.Delete(batch);
        if (await uow.Complete()) return NoContent();
        return BadRequest("Failed to delete import batch");
    }
}

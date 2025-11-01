using LexiFlow.Api.Data;
using LexiFlow.Api.Dtos;
using LexiFlow.Api.Entities;
using LexiFlow.Api.Infrastructure.Extensions;
using LexiFlow.Api.Infrastructure.Options;
using LexiFlow.Api.Infrastructure.Security;
using LexiFlow.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LexiFlow.Api.Controllers;

[ApiController]
[Route("api/upload")]
[Authorize]
public class UploadController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly StorageOptions _storageOptions;
    private readonly OcrClient _ocrClient;
    private readonly ILogger<UploadController> _logger;

    public UploadController(
        ApplicationDbContext dbContext,
        IOptions<StorageOptions> storageOptions,
        OcrClient ocrClient,
        ILogger<UploadController> logger)
    {
        _dbContext = dbContext;
        _storageOptions = storageOptions.Value;
        _ocrClient = ocrClient;
        _logger = logger;
    }

    [HttpPost]
    [RequestFormLimits(MultipartBodyLengthLimit = 10_000_000)]
    [ProducesResponseType(typeof(ReceiptDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadReceipt(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("File is required.");
        }

        var uploadsPath = _storageOptions.UploadsPath;
        Directory.CreateDirectory(uploadsPath);

        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{extension}";
        var storedPath = Path.Combine(uploadsPath, fileName);

        await using (var stream = System.IO.File.Create(storedPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var ocrResult = await _ocrClient.ExtractAsync(storedPath, cancellationToken);
        if (ocrResult is null)
        {
            _logger.LogWarning("OCR extraction failed, falling back to defaults.");
        }

        var receipt = new Receipt
        {
            Vendor = ocrResult?.Vendor ?? "Uploaded Receipt",
            InvoiceDate = ocrResult?.InvoiceDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Total = ocrResult?.Total ?? 0m,
            Vat = ocrResult?.Vat ?? 0m,
            Currency = ocrResult?.Currency ?? "EUR",
            RawText = ocrResult?.RawText,
            FilePath = storedPath,
            CreatedAt = DateTime.UtcNow,
            CreatedById = User.GetUserId(),
            Status = ReceiptStatus.Pending
        };

        _dbContext.Receipts.Add(receipt);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(receipt.ToDto());
    }
}

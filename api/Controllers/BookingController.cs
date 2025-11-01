using System.Collections.Generic;
using LexiFlow.Api.Data;
using LexiFlow.Api.Dtos;
using LexiFlow.Api.Entities;
using LexiFlow.Api.Infrastructure.Extensions;
using LexiFlow.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LexiFlow.Api.Controllers;

[ApiController]
[Route("api/book")]
[Authorize]
public class BookingController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly LexOfficeClient _lexOfficeClient;
    private readonly ILogger<BookingController> _logger;

    public BookingController(ApplicationDbContext dbContext, LexOfficeClient lexOfficeClient, ILogger<BookingController> logger)
    {
        _dbContext = dbContext;
        _lexOfficeClient = lexOfficeClient;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(BookReceiptResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BookReceipt([FromBody] BookReceiptRequestDto request, CancellationToken cancellationToken)
    {
        var receipt = await _dbContext.Receipts
            .Include(r => r.Booking)
            .FirstOrDefaultAsync(r => r.Id == request.ReceiptId, cancellationToken);

        if (receipt is null)
        {
            return NotFound();
        }

        if (receipt.Status == ReceiptStatus.Booked && receipt.Booking is not null)
        {
            return Ok(new BookReceiptResponseDto(receipt.Booking.VoucherId));
        }

        ApplyCorrections(receipt, request.Corrections);
        receipt.UpdatedAt = DateTime.UtcNow;

        var voucherId = await _lexOfficeClient.CreateVoucherAsync(
            new LexOfficeClient.VoucherRequest(
                receipt.Vendor,
                receipt.InvoiceDate,
                receipt.Total,
                receipt.Vat,
                receipt.Currency,
                receipt.RawText ?? string.Empty),
            cancellationToken);

        if (string.IsNullOrWhiteSpace(voucherId))
        {
            _logger.LogWarning("LexOffice booking failed for receipt {ReceiptId}", receipt.Id);
            return StatusCode(StatusCodes.Status502BadGateway, "Failed to book receipt");
        }

        var booking = new Booking
        {
            Receipt = receipt,
            VoucherId = voucherId,
            BookedAt = DateTime.UtcNow
        };

        receipt.Status = ReceiptStatus.Booked;
        receipt.Booking = booking;
        _dbContext.Bookings.Add(booking);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new BookReceiptResponseDto(voucherId));
    }

    private static void ApplyCorrections(Receipt receipt, Dictionary<string, string>? corrections)
    {
        if (corrections is null)
        {
            return;
        }

        if (corrections.TryGetValue("vendor", out var vendor) && !string.IsNullOrWhiteSpace(vendor))
        {
            receipt.Vendor = vendor;
        }

        if (corrections.TryGetValue("invoiceDate", out var invoiceDateRaw) &&
            DateOnly.TryParse(invoiceDateRaw, out var parsedDate))
        {
            receipt.InvoiceDate = parsedDate;
        }

        if (corrections.TryGetValue("total", out var totalRaw) && decimal.TryParse(totalRaw, out var total))
        {
            receipt.Total = total;
        }

        if (corrections.TryGetValue("vat", out var vatRaw) && decimal.TryParse(vatRaw, out var vat))
        {
            receipt.Vat = vat;
        }

        if (corrections.TryGetValue("currency", out var currency) && !string.IsNullOrWhiteSpace(currency))
        {
            receipt.Currency = currency;
        }

        if (corrections.TryGetValue("rawText", out var raw))
        {
            receipt.RawText = raw;
        }
    }
}

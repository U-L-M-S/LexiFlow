using LexiFlow.Api.Dtos;
using LexiFlow.Api.Entities;

namespace LexiFlow.Api.Infrastructure.Extensions;

public static class ReceiptMappingExtensions
{
    public static ReceiptDto ToDto(this Receipt receipt) =>
        new(
            receipt.Id,
            receipt.Vendor,
            receipt.InvoiceDate,
            receipt.Total,
            receipt.Vat,
            receipt.Currency,
            receipt.Status.ToString(),
            receipt.CreatedAt,
            receipt.UpdatedAt,
            receipt.RawText,
            receipt.FilePath,
            receipt.Booking?.VoucherId
        );
}

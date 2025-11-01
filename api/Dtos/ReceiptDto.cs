using System;

namespace LexiFlow.Api.Dtos;

public record ReceiptDto(
    Guid Id,
    string Vendor,
    DateOnly InvoiceDate,
    decimal Total,
    decimal Vat,
    string Currency,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string? RawText,
    string? FilePath,
    string? VoucherId
);

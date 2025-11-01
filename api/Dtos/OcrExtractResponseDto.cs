using System;

namespace LexiFlow.Api.Dtos;

public record OcrExtractResponseDto(
    string Vendor,
    DateOnly InvoiceDate,
    decimal Total,
    decimal Vat,
    string Currency,
    string RawText
);

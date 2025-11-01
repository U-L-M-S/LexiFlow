using System;

namespace LexiFlow.Api.Entities;

public class Receipt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Vendor { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; }
    public decimal Total { get; set; }
    public decimal Vat { get; set; }
    public string Currency { get; set; } = "EUR";
    public ReceiptStatus Status { get; set; } = ReceiptStatus.Pending;
    public string? RawText { get; set; }
    public string? FilePath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedById { get; set; }
    public AppUser? CreatedBy { get; set; }
    public Booking? Booking { get; set; }
}

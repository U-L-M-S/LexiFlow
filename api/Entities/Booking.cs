using System;

namespace LexiFlow.Api.Entities;

public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReceiptId { get; set; }
    public Receipt Receipt { get; set; } = default!;
    public string VoucherId { get; set; } = string.Empty;
    public DateTime BookedAt { get; set; } = DateTime.UtcNow;
}

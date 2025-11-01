using LexiFlow.Api.Entities;
using LexiFlow.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LexiFlow.Api.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");

        await context.Database.MigrateAsync(cancellationToken);

        var demoUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "demo", cancellationToken);
        if (demoUser is null)
        {
            demoUser = new AppUser
            {
                Username = "demo",
                DisplayName = "Demo User",
                PasswordHash = PasswordHasher.Hash("demo123!")
            };
            context.Users.Add(demoUser);
            logger.LogInformation("Created demo user.");
        }

        var receiptsToSeed = new[]
        {
            new
            {
                Vendor = "Demo Supermarket GmbH",
                Date = new DateOnly(2025, 1, 15),
                Total = 23.85m,
                Vat = 19.00m,
                Status = ReceiptStatus.Booked,
                RawText = "Demo Supermarket GmbH\nRechnung 2025-01-15\nGesamt 23,85 EUR\nMwSt 19%"
            },
            new
            {
                Vendor = "Office Depot AG",
                Date = new DateOnly(2025, 1, 16),
                Total = 89.90m,
                Vat = 19.00m,
                Status = ReceiptStatus.Pending,
                RawText = "Office Depot AG\nRechnung 2025-01-16\nGesamt 89,90 EUR\nMwSt 19%"
            },
            new
            {
                Vendor = "Bäckerei Sonnig",
                Date = new DateOnly(2025, 1, 17),
                Total = 5.40m,
                Vat = 7.00m,
                Status = ReceiptStatus.Pending,
                RawText = "Bäckerei Sonnig\nRechnung 2025-01-17\nGesamt 5,40 EUR\nMwSt 7%"
            }
        };

        foreach (var receiptInfo in receiptsToSeed)
        {
            var existing = await context.Receipts
                .FirstOrDefaultAsync(r => r.Vendor == receiptInfo.Vendor && r.InvoiceDate == receiptInfo.Date, cancellationToken);

            if (existing is not null)
            {
                continue;
            }

            var receipt = new Receipt
            {
                Vendor = receiptInfo.Vendor,
                InvoiceDate = receiptInfo.Date,
                Total = receiptInfo.Total,
                Vat = receiptInfo.Vat,
                Currency = "EUR",
                Status = receiptInfo.Status,
                RawText = receiptInfo.RawText,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = demoUser
            };
            context.Receipts.Add(receipt);

            if (receipt.Status == ReceiptStatus.Booked)
            {
                var booking = new Booking
                {
                    Receipt = receipt,
                    VoucherId = "demo-voucher-001",
                    BookedAt = DateTime.UtcNow
                };
                context.Bookings.Add(booking);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}

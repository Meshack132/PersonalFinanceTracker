using Microsoft.EntityFrameworkCore;
using PersonalFinanceTracker.Domain.Entities;
using PersonalFinanceTracker.Domain.Enums;

namespace PersonalFinanceTracker.Infrastructure.Persistence;

/// <summary>
/// EF Core context for SQLite persistence.
///
/// Notable design choice: <see cref="Transaction.Amount"/> is a <c>Money</c> value
/// object (a readonly record struct). Rather than modelling it as an owned *entity*
/// (which implies identity semantics it doesn't have), we use EF Core 8's Complex
/// Types feature — introduced specifically for value objects like this. It maps
/// Money's two fields to plain columns on the Transactions table with no shadow
/// key, no separate identity, exactly matching what a value object should be.
/// </summary>
public class AppDbContext : DbContext
{
    public DbSet<Transaction> Transactions => Set<Transaction>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("Transactions");
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Date)
                .IsRequired();

            entity.Property(t => t.Description)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(t => t.Category)
                .HasMaxLength(100)
                .HasDefaultValue("Uncategorized");

            entity.Property(t => t.SourceBank)
                .HasMaxLength(50);

            // Store enums as strings — readable in the DB, resilient to enum
            // reordering (unlike storing the underlying int).
            entity.Property(t => t.Type)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(t => t.TaxCategory)
                .HasConversion<string>()
                .HasMaxLength(30);

            // Money as a Complex Type (EF Core 8+) — see class remarks above.
            entity.ComplexProperty(t => t.Amount, money =>
            {
                money.Property(m => m.Amount)
                    .HasColumnName("Amount")
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                money.Property(m => m.Currency)
                    .HasColumnName("Currency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

            // DedupeKey is derived at runtime from Date/Amount/Description —
            // it has no business being a column.
            entity.Ignore(t => t.DedupeKey);

            // Helpful for the "list transactions" and dedupe-check queries.
            entity.HasIndex(t => t.Date);
        });
    }
}

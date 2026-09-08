using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LibrarySystem.Service.Domain;

namespace LibrarySystem.Service.Infrastructure.Persistence.Configurations;

internal sealed class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public const string ActiveLoanPerBookIndexName = "IX_loans_BookId_Active";

    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.ToTable("loans");
        builder.HasKey(l => l.Id);

        // Maps to Postgres' xmin system column, no extra column needed. 
        // Protects concurrent returns of the same loan.
        builder.Property<uint>("Version").IsRowVersion();

        builder.HasIndex(l => l.BorrowedAtUtc);

        // Enforces "one active loan per book" at the database level so two
        // concurrent borrow requests for the same book can't both succeed.
        builder.HasIndex(l => l.BookId)
            .IsUnique()
            .HasFilter($"\"{nameof(Loan.ReturnedAtUtc)}\" IS NULL")
            .HasDatabaseName(ActiveLoanPerBookIndexName);

        builder.HasOne<Book>()
            .WithMany()
            .HasForeignKey(l => l.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Reader>()
            .WithMany()
            .HasForeignKey(l => l.ReaderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
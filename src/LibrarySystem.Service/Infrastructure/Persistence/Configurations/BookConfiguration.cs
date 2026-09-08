using LibrarySystem.Contracts.Common.Constraints;
using LibrarySystem.Service.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibrarySystem.Service.Infrastructure.Persistence.Configurations;

internal sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("books");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Title).HasMaxLength(BookConstraints.MaxTitleLength).IsRequired();
        builder.Property(b => b.Author).HasMaxLength(BookConstraints.MaxAuthorLength).IsRequired();
    }
}
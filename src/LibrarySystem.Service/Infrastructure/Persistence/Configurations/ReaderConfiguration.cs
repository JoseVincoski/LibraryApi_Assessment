using LibrarySystem.Contracts.Common.Constraints;
using LibrarySystem.Service.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibrarySystem.Service.Infrastructure.Persistence.Configurations;

internal sealed class ReaderConfiguration : IEntityTypeConfiguration<Reader>
{
    public void Configure(EntityTypeBuilder<Reader> builder)
    {
        builder.ToTable("readers");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name).HasMaxLength(ReaderConstraints.MaxNameLength).IsRequired();
        builder.Property(m => m.Email).HasMaxLength(ReaderConstraints.MaxEmailLength).IsRequired();

        builder.HasIndex(m => m.Email).IsUnique();
    }
}
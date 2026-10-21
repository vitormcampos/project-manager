using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectHub.Domain.Models;

namespace ProjectHub.Data.Configurations;

/// <summary>Configures persistence for tenant accounts.</summary>
public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");
        builder.HasKey(account => account.Id);
        builder.Property(account => account.Name).HasMaxLength(200).IsRequired();
        builder
            .Property(account => account.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(account => account.CreatedAt).IsRequired();
        builder.Property(account => account.UpdatedAt).IsConcurrencyToken();
        builder.HasIndex(account => account.Role);
    }
}

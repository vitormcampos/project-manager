using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectHub.Domain.Models;

namespace ProjectHub.Data.Configurations;

/// <summary>Configures persistence for account members.</summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.AccountId).IsRequired();
        builder.Property(user => user.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(user => user.LastName).HasMaxLength(100).IsRequired();
        builder.Property(user => user.Email).HasMaxLength(320).IsRequired();
        builder.Property(user => user.NormalizedEmail).HasMaxLength(320).IsRequired();
        builder.Property(user => user.PasswordHash).HasColumnType("text");
        builder.Property(user => user.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(user => user.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(user => user.CreatedAt).IsRequired();
        builder
            .HasIndex(user => user.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("UX_Users_NormalizedEmail");
        builder
            .HasIndex(user => new { user.AccountId, user.Status })
            .HasDatabaseName("IX_Users_AccountId_Status");
        builder
            .HasOne<Account>()
            .WithMany()
            .HasForeignKey(user => user.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

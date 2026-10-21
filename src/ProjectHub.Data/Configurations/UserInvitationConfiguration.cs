using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectHub.Domain.Models;

namespace ProjectHub.Data.Configurations;

/// <summary>Configures persistence for single-use user invitations.</summary>
public sealed class UserInvitationConfiguration : IEntityTypeConfiguration<UserInvitation>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserInvitation> builder)
    {
        builder.ToTable("UserInvitations");
        builder.HasKey(invitation => invitation.Id);
        builder.Property(invitation => invitation.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(invitation => invitation.ExpiresAt).IsRequired();
        builder.Property(invitation => invitation.CreatedAt).IsRequired();
        builder.Property(invitation => invitation.AcceptedAt).IsConcurrencyToken();
        builder
            .HasIndex(invitation => invitation.TokenHash)
            .IsUnique()
            .HasDatabaseName("UX_UserInvitations_TokenHash");
        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(invitation => invitation.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

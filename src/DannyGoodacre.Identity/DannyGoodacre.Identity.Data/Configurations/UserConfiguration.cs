using DannyGoodacre.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DannyGoodacre.Identity.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.PublicId).IsRequired();
        builder.HasIndex(u => u.PublicId).IsUnique();

        builder.Property(u => u.Username).HasMaxLength(256).IsRequired();
        builder.HasIndex(u => u.Username).IsUnique();

        builder.Property(u => u.PasswordHash).HasMaxLength(1024).IsRequired();
        builder.Property(u => u.SecurityStamp).HasMaxLength(256).IsRequired();
        builder.Property(u => u.ConcurrencyStamp).HasMaxLength(256).IsConcurrencyToken().IsRequired();
    }
}

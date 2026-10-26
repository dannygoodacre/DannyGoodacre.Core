using DannyGoodacre.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DannyGoodacre.Identity.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.PublicId)
            .IsRequired();

        builder.HasIndex(r => r.PublicId)
            .IsUnique();

        builder.Property(r => r.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(r => r.Name)
            .IsUnique();

        // Relationship is ALREADY configured inside RoleClaimConfiguration.
        // DO NOT add builder.HasMany(r => r.Claims) here.
    }
}

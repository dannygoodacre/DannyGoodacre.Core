using DannyGoodacre.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DannyGoodacre.Identity.Data.Configurations;

public class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.ToTable("Claims");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.PublicId)
            .IsRequired();

        builder.Property(c => c.Type)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(c => c.Value)
            .HasMaxLength(512)
            .IsRequired();

        builder.HasIndex(c => new { c.Type, c.Value })
            .IsUnique();
    }
}

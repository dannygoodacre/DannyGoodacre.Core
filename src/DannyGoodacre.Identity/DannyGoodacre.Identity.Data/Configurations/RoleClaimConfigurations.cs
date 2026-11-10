using DannyGoodacre.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DannyGoodacre.Identity.Data.Configurations;

public class RoleClaimConfiguration : IEntityTypeConfiguration<RoleClaim>
{
    public void Configure(EntityTypeBuilder<RoleClaim> builder)
    {
        builder.ToTable("RoleClaims");

        // Set composite primary key
        builder.HasKey(rc => new { rc.RoleId, rc.ClaimId });

        // Map foreign key back to Role navigation
        builder.HasOne<Role>()
            .WithMany(r => r.Claims)
            .HasForeignKey(rc => rc.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Map foreign key to Claim navigation (if RoleClaim has public Claim Claim { get; set; })
        builder.HasOne(rc => rc.Claim)
            .WithMany()
            .HasForeignKey(rc => rc.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

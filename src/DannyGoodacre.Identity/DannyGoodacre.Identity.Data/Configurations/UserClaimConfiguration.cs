using DannyGoodacre.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DannyGoodacre.Identity.Data.Configurations;

public class UserClaimConfiguration : IEntityTypeConfiguration<UserClaim>
{
    public void Configure(EntityTypeBuilder<UserClaim> builder)
    {
        builder.ToTable("UserClaims");

        builder.HasKey(uc => new { uc.UserId, uc.ClaimId });

        builder.HasOne<User>()
            .WithMany(u => u.Claims)
            .HasForeignKey(uc => uc.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(uc => uc.Claim)
            .WithMany()
            .HasForeignKey(uc => uc.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

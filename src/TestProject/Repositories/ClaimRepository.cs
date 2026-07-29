namespace TestProject.Repositories;

internal interface IClaimRepository
{
    void Add(Claim claim);
}

internal sealed class ClaimRepository(IdentityContext context) : IClaimRepository
{

    public void Add(Claim claim)
        => context.Add(claim);
}

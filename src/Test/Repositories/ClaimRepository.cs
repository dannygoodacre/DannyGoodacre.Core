namespace Test.Repositories;

internal interface IClaimRepository
{
    void Add(Claim claim);
}

internal sealed class ClaimRepository(ApplicationContext context) : IClaimRepository
{

    public void Add(Claim claim)
        => context.Add(claim);
}

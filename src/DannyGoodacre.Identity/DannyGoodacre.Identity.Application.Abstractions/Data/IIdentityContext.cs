namespace DannyGoodacre.Identity.Application.Abstractions.Data;

public interface IIdentityContext
{
    public Task<int> SaveChangesAsync();
}

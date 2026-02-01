namespace DannyGoodacre.Identity.Services;

internal interface ISeedAdminService
{
    Task SeedAdmin();
}

internal sealed class SeedAdminService() : ISeedAdminService
{
    public Task SeedAdmin()

}

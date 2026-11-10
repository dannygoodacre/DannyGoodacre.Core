using DannyGoodacre.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Test;

public class ApplicationContext(DbContextOptions<ApplicationContext> options) : IdentityContext(options);

public class ApplicationContextFactory : IDesignTimeDbContextFactory<ApplicationContext>
{
    public ApplicationContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationContext>();

        optionsBuilder.UseSqlite("Data Source=app.db");

        return new ApplicationContext(optionsBuilder.Options);
    }
}

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DannyGoodacre.Core.Identity;

public class IdentityContext(DbContextOptions options) : IdentityDbContext<ApplicationUser>(options);

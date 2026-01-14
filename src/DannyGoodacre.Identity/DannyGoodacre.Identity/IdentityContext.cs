using DannyGoodacre.Identity.Core;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DannyGoodacre.Identity;

public class IdentityContext(DbContextOptions options) : IdentityDbContext<IdentityUser>(options);

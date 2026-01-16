using DannyGoodacre.Identity.Core;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DannyGoodacre.Identity;

public class IdentityContext(DbContextOptions options)
    : IdentityDbContext<IdentityUser>(options);

public class IdentityContext<TUser>(DbContextOptions options)
    : IdentityDbContext<TUser>(options) where TUser : IdentityUser;

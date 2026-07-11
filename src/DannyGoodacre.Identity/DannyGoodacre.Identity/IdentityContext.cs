using Microsoft.EntityFrameworkCore;

namespace DannyGoodacre.Identity;

public class IdentityContext(DbContextOptions options)
    : Data.IdentityContext(options);

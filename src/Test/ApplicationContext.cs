using DannyGoodacre.Identity;
using Microsoft.EntityFrameworkCore;

namespace Test;

public class ApplicationContext(DbContextOptions<ApplicationContext> options)
    : IdentityContext(options);

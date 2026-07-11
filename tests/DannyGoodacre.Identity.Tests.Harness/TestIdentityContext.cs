using Microsoft.EntityFrameworkCore;

namespace DannyGoodacre.Identity.Tests.Harness;

public sealed class TestIdentityContext(DbContextOptions<TestIdentityContext> options)
    : IdentityContext(options);

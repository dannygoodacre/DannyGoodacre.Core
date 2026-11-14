using DannyGoodacre.Cqrs.Examples.Plans;
using DannyGoodacre.Cqrs.Examples.Tests.DuplicateRegistrations;
using Microsoft.Extensions.DependencyInjection;

namespace DannyGoodacre.Cqrs.Examples.Tests;

public sealed class DuplicateRegistrationTests
{
    // The example application does not scan this test assembly, so the types in DuplicateRegistrationTypes.cs
    // cannot break it.
    [Theory]
    [InlineData("IQueryHandler<GetPlanQuery, PlanDetails> (GetPlanQueryHandler, CachedGetPlanQueryHandler)")]
    [InlineData("ICommandHandler<ArchivePlanCommand> (ArchivePlanCommandHandler, AuditedArchivePlanCommandHandler : ArchivePlanCommandHandler)")]
    [InlineData("StrictArchivePlanCommandValidator : ArchivePlanCommandValidator (IValidator<ArchivePlanCommand>)")]
    [InlineData("PlanArchivedAndOwnerNotifiedHook : PlanArchivedHook (IPostCommitHook<ArchivePlanCommand>)")]
    public void AddCqrsHandlers_WhenScanningDuplicateRegistrations_ThrowsNamingThem(string expected)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddCqrsHandlers(typeof(GetPlanQuery).Assembly, typeof(CachedGetPlanQueryHandler).Assembly));

        Assert.Contains(expected, exception.Message);
    }
}

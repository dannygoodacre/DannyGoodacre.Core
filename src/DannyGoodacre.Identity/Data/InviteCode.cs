namespace DannyGoodacre.Identity.Data;

/// <summary>
/// The single code that registration requires. One row, created by
/// <see cref="ServiceProviderExtensions.InitializeIdentityAsync"/>.
/// </summary>
internal sealed class InviteCode
{
    public const int SingletonId = 1;

    public int Id { get; init; } = SingletonId;

    public Guid Code { get; set; }
}

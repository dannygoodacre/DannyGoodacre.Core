namespace Test;

public static class BlackjackPermissions
{
    public const string TableCreate = "Blackjack.Tables.Create";
    public const string TableDelete = "Blackjack.Tables.Delete";
    public const string TableRead   = "Blackjack.Tables.Read";
}

public static class BlackjackRoles
{
    public readonly static Dictionary<string, List<string>> Mappings = new()
    {
        ["PitBoss"] = [BlackjackPermissions.TableCreate, BlackjackPermissions.TableDelete, BlackjackPermissions.TableRead],
        ["Dealer"]  = [BlackjackPermissions.TableCreate, BlackjackPermissions.TableRead],
        ["Player"]  = [BlackjackPermissions.TableRead]
    };
}

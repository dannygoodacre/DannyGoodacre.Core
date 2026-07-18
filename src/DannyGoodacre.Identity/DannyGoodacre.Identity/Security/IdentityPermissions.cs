namespace DannyGoodacre.Identity;

public static class IdentityPermissions
{
    public static class Claims
    {
        public const string Create = "Claims.Create";

        public const string Read = "Claims.Read";

        public const string Delete = "Claims.Delete";
    }

    public static class Roles
    {
        public const string Create = "Roles.Create";

        public const string Read = "Roles.Read";

        public const string Delete = "Roles.Delete";
    }

    public static class Users
    {
        public const string Delete = "Users.Delete";
    }
}

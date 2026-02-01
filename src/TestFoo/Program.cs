namespace TestFoo;

class Program
{
    static void Main(string[] args)
    {
        var hasher = new PasswordHasherService();

        const string password = "TestPassword";

        var hash = hasher.HashPassword(password);

        Console.WriteLine(hasher.VerifyPassword(password, hash));
    }
}

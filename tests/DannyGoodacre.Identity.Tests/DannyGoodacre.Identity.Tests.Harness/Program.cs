namespace DannyGoodacre.Identity.Tests.Harness;

public class Program
{
    public async static Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var app = builder.Build();

        await app.RunAsync();
    }
}

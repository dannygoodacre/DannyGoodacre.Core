namespace DannyGoodacre.Cqrs.Examples;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(string email, CancellationToken cancellationToken = default);
}

public sealed class EmailService : IEmailService
{
    public Task SendWelcomeEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Email sent");

        return Task.CompletedTask;
    }
}

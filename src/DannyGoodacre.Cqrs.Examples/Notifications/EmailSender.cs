using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs.Examples.Notifications;

public interface IEmailSender
{
    Task SendWelcomeEmailAsync(string email, string name, CancellationToken cancellationToken = default);
}

public sealed partial class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendWelcomeEmailAsync(string email, string name, CancellationToken cancellationToken = default)
    {
        LogWelcomeEmailSentToNameAtEmail(name, email);

        return Task.CompletedTask;
    }

    [LoggerMessage(LogLevel.Information, "Welcome email sent to {Name} at {Email}")]
    partial void LogWelcomeEmailSentToNameAtEmail(string name, string email);
}

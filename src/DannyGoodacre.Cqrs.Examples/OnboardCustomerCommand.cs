using DannyGoodacre.Primitives;

namespace DannyGoodacre.Cqrs.Examples;

// 1. Child Commands & Queries
public record CreateCustomerCommand(string Name, string Email) : IStateCommand;

public record GetWelcomeEmailTemplateQuery(string LanguageCode) : IQuery<string>;

public record ScheduleEmailCommand(int CustomerId, string EmailBody) : IStateCommand;

// 2. Orchestrator Command
public record OnboardCustomerCommand(string Name, string Email, string LanguageCode) : ITransactedStateCommand;

// 3. Orchestrator Handler
public sealed class OnboardCustomerCommandHandler(
    ICommandHandler<CreateCustomerCommand, int> createCustomerHandler,
    IQueryHandler<GetWelcomeEmailTemplateQuery, string> getTemplateHandler,
    ICommandHandler<ScheduleEmailCommand> scheduleEmailHandler)
    : ICommandHandler<OnboardCustomerCommand>
{
    public async Task<IResult> HandleAsync(
        OnboardCustomerCommand command,
        CancellationToken cancellationToken = default)
    {
        // 1. Create the customer.
        // The database creates a customer ID when this state command saves the data.
        IResult<int> createResult = await createCustomerHandler.HandleAsync(
            new CreateCustomerCommand(command.Name, command.Email), cancellationToken);

        if (createResult is not Success<int>(var customerId))
        {
            return createResult;
        }

        // 2. Get the welcome email template with a query.
        IResult<string> templateResult = await getTemplateHandler.HandleAsync(
            new GetWelcomeEmailTemplateQuery(command.LanguageCode), cancellationToken);

        if (templateResult is not Success<string> templateSuccess)
        {
            return templateResult;
        }

        // 3. Schedule the email with the new customer ID and the template.
        IResult scheduleResult = await scheduleEmailHandler.HandleAsync(
            new ScheduleEmailCommand(customerId, templateSuccess.Value), cancellationToken);

        if (scheduleResult.IsFailure)
        {
            return scheduleResult;
        }

        // The outer transaction decorator commits all changes in a single operation.
        return Result.Success();
    }
}

using DannyGoodacre.Cqrs.Examples.Customers;
using DannyGoodacre.Cqrs.Examples.Notifications;
using DannyGoodacre.Cqrs.Examples.Persistence;
using DannyGoodacre.Cqrs.Examples.Plans;
using DannyGoodacre.Primitives;

namespace DannyGoodacre.Cqrs.Examples.Onboarding;

// A transacted state command. All steps run in one database transaction.
// If a step fails, the transaction rolls back the changes from all steps.
public sealed record OnboardCustomerCommand(string Name, string Email, string PlanCode) : ITransactedStateCommand;

public sealed class OnboardCustomerCommandValidator(IValidator<RegisterCustomerCommand> customerValidator)
    : IValidator<OnboardCustomerCommand>
{
    public void Validate(OnboardCustomerCommand request, ValidationState validationState)
    {
        // Reuse the rules of the child command, so the outer command fails before the transaction starts.
        customerValidator.Validate(new RegisterCustomerCommand(request.Name, request.Email), validationState);

        if (string.IsNullOrWhiteSpace(request.PlanCode))
        {
            validationState.AddError(nameof(request.PlanCode), "Plan code is required.");
        }
    }
}

public sealed class OnboardCustomerCommandHandler(
    ICommandHandler<RegisterCustomerCommand, Guid> registerCustomerHandler,
    IQueryHandler<GetPlanQuery, PlanDetails> getPlanHandler,
    ICustomerRepository customerRepository,
    ISubscriptionRepository subscriptionRepository,
    TimeProvider timeProvider)
    : ICommandHandler<OnboardCustomerCommand, Guid>
{
    public async Task<IResult<Guid>> HandleAsync(
        OnboardCustomerCommand command,
        CancellationToken cancellationToken = default)
    {
        // Step 1: a child state command. Its SaveChangesAsync call writes the customer
        // inside this command's transaction. It does not start or commit a transaction.
        IResult<Guid> registerResult = await registerCustomerHandler.HandleAsync(
            new RegisterCustomerCommand(command.Name, command.Email), cancellationToken);

        if (registerResult is not Success<Guid>(var customerPublicId))
        {
            return registerResult;
        }

        // Step 2: a child query. If the plan does not exist, the transaction rolls back the new customer.
        IResult<PlanDetails> planResult = await getPlanHandler.HandleAsync(
            new GetPlanQuery(command.PlanCode), cancellationToken);

        if (planResult is not Success<PlanDetails>(var plan))
        {
            return planResult.MapFailure<Guid>();
        }

        // Step 3: the save in step 1 inserted the customer row inside this transaction,
        // so the database has already generated the customer's Id. Read it to link the subscription.
        int customerId = await customerRepository.GetIdAsync(customerPublicId, cancellationToken);

        // Step 4: a change made by this handler. The transaction decorator saves it before the commit.
        subscriptionRepository.Add(new Subscription
        {
            PublicId = Guid.NewGuid(),
            CustomerId = customerId,
            PlanCode = plan.Code,
            StartedOn = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime)
        });

        return new Success<Guid>(customerPublicId);
    }
}

// Runs only after the transaction commits. If any step fails, the email is not sent.
public sealed class SendWelcomeEmailHook(IEmailSender emailSender)
    : IPostCommitHook<OnboardCustomerCommand, Guid>
{
    public Task AfterCommitAsync(
        OnboardCustomerCommand command,
        IResult<Guid> result,
        CancellationToken cancellationToken = default)
        => emailSender.SendWelcomeEmailAsync(command.Email, command.Name, cancellationToken);
}

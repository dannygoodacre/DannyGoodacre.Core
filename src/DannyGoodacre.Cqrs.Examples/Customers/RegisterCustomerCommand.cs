using System.Net.Mail;
using DannyGoodacre.Cqrs.Examples.Persistence;
using DannyGoodacre.Primitives;

namespace DannyGoodacre.Cqrs.Examples.Customers;

// A state command. The pipeline calls SaveChangesAsync after the handler returns a success result.
public sealed record RegisterCustomerCommand(string Name, string Email) : IStateCommand;

public sealed class RegisterCustomerCommandValidator : IValidator<RegisterCustomerCommand>
{
    public void Validate(RegisterCustomerCommand request, ValidationState validationState)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            validationState.AddError(nameof(request.Name), "Name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            validationState.AddError(nameof(request.Email), "Email is required.");
        }
        else if (!MailAddress.TryCreate(request.Email, out _))
        {
            validationState.AddError(nameof(request.Email), "Email format is invalid.");
        }
    }
}

public sealed class RegisterCustomerCommandHandler(ICustomerRepository customerRepository)
    : ICommandHandler<RegisterCustomerCommand, Guid>
{
    public async Task<IResult<Guid>> HandleAsync(
        RegisterCustomerCommand command,
        CancellationToken cancellationToken = default)
    {
        string email = command.Email.Trim().ToLowerInvariant();

        if (await customerRepository.EmailExistsAsync(email, cancellationToken))
        {
            return Result<Guid>.Conflict("A customer with this email address already exists.");
        }

        var customer = new Customer
        {
            PublicId = Guid.NewGuid(),
            Name = command.Name.Trim(),
            Email = email
        };

        customerRepository.Add(customer);

        return Result<Guid>.Success(customer.PublicId);
    }
}

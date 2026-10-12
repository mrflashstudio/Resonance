using FluentValidation;
using Resonance.Models;

namespace Resonance.Http.Validation;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Login).NotEmpty();
        RuleFor(request => request.PasswordProof).NotNull().NotEqual(string.Empty);
        RuleFor(request => request.Client).NotNull().NotEqual(string.Empty);
    }
}

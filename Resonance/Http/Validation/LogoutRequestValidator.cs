using FluentValidation;
using Resonance.Models;

namespace Resonance.Http.Validation;

public sealed class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    public LogoutRequestValidator()
    {
        RuleFor(request => request.Session).NotNull().NotEqual(string.Empty);
    }
}

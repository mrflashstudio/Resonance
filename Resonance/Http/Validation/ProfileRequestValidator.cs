using FluentValidation;
using Resonance.Models;

namespace Resonance.Http.Validation;

public sealed class ProfileRequestValidator : AbstractValidator<ProfileRequest>
{
    public ProfileRequestValidator()
    {
        RuleFor(request => request.Id).NotNull().GreaterThan(0);
    }
}

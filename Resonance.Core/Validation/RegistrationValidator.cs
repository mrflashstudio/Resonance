using FluentValidation;
using Resonance.Core.Models;

namespace Resonance.Core.Validation;

public sealed class RegistrationValidator : AbstractValidator<RegistrationInput>
{
    public RegistrationValidator()
    {
        RuleFor(input => input.Password).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("The Password field is required.")
            .Length(8, 128).WithMessage("The field Password must be a string with a minimum length of 8 and a maximum length of 128.");

        RuleFor(input => input.Username).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("The Username field is required.")
            .Length(3, 32).WithMessage("Username must be between 3 and 32 characters.")
            .Matches(@"\A[A-Za-z0-9_ -]+\z")
            .WithMessage("Username can only contain letters (A-Z), numbers, underscores, hyphens, and spaces.")
            .Must(username => !username!.StartsWith(' ') && !username.EndsWith(' '))
            .WithMessage("Username cannot start or end with a space.");
    }
}

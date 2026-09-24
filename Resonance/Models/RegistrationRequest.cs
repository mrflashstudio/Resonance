using System.ComponentModel.DataAnnotations;

namespace Resonance.Models;

public sealed record RegistrationRequest(
    [Required, RegularExpression(@"\A[A-Za-z0-9_]{3,32}\z")] string Username,
    [Required, StringLength(128, MinimumLength = 8)] string Password);

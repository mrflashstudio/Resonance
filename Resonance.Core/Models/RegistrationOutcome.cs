namespace Resonance.Core.Models;

public sealed record RegistrationOutcome(RegistrationResult? Account, IReadOnlyDictionary<string, string[]> Errors);

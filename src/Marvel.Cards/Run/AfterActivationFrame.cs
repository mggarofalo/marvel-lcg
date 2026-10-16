namespace Marvel.Cards.Run;

// A delayed body has its own lifetime. Completing it never resumes the sentence
// that registered it, which has already continued independently.
internal sealed record AfterActivationFrame : AbilityStructuralFrame;

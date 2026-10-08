namespace Marvel.Cards.Run;

/// <summary>Schedules one Special, or closes a sequence whose remaining cards left play.</summary>
internal sealed record NextSpecialCommand(int? Card, bool FinalStep) : AbilityStructuralTransition;

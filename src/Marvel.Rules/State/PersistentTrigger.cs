namespace Marvel.Rules.State;

/// <summary>The authored timing envelope; roles refer to this ability, not to a guessed player.</summary>
public sealed record PersistentTrigger(string Timing, string? Event, string? Subject, string? Actor, string? Target, string? Form, string? Player, bool AnyPlayer);

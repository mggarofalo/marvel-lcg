namespace Marvel.View;

/// <summary>The authored triggering roles and form restriction; any-player access does not imply current legality.</summary>
public sealed record CardPersistentTriggerDescriptor(string Timing, string? Event, string? Subject, string? Actor, string? Target, string? Form, string? Player, bool AnyPlayer);

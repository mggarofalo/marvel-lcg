namespace Marvel.Rules.Events;

/// <summary>An applied cancellation of one card's When Revealed effects, not its revelation.</summary>
/// <remarks>The engine chooses this emitted-only wire fact; a digest cannot identify its source or scope.</remarks>
public sealed record WhenRevealedCanceled(int Card, int? Source) : GameEvent;

namespace Marvel.Rules.Play;

/// <summary>One applicable rule of the separated table, and the card it concerns, if any.</summary>
/// <param name="Kind">Which rule applies.</param>
/// <param name="Card">The central or local scheme the rule concerns, or null for the whole table.</param>
public sealed record KangNote(KangNoteKind Kind, int? Card);

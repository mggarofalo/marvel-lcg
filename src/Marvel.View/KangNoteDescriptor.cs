using Marvel.Rules.Play;

namespace Marvel.View;

/// <summary>One engine-identified rule of the separated table; clients word it from its kind.</summary>
/// <param name="Kind">The rule; clients address a note by this kind, never by position.</param>
/// <param name="Card">The central or local scheme it concerns, or null for the whole table.</param>
public sealed record KangNoteDescriptor(KangNoteKind Kind, int? Card);

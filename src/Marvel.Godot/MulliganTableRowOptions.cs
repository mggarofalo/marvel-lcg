using Godot;

namespace Marvel.Godot;

/// <summary>Optional public workspace content carried by one tabletop row.</summary>
internal sealed record MulliganTableRowOptions(bool AddEmptyDiscard, Control? Companion);

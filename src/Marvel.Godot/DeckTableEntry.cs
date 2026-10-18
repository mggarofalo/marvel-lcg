namespace Marvel.Godot;

/// <summary>One persistent row's display values, supplied recipes and host quantity offers.</summary>
internal sealed record DeckTableEntry(string Face, string Title, string Detail, int Count, bool Required, int[] Quantities);

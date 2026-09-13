namespace Marvel.Godot;

/// <summary>Issues tokens that make deferred work belonging to an older render inert.</summary>
internal sealed class InteractionGeneration
{
    private int current;

    internal int Current => current;

    internal int Advance() => checked(++current);

    internal bool IsCurrent(int token) => token == current;
}

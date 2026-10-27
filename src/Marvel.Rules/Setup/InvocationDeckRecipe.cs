using System.Collections.Immutable;

namespace Marvel.Rules.Setup;

/// <summary>Doctor Strange's separate physical Invocations in authored allocation order.</summary>
public sealed class InvocationDeckRecipe
{
    private static readonly ImmutableArray<string> Faces = ["09032", "09033", "09034", "09035", "09036"];

    /// <summary>Validates exact printed membership and freezes the recipe.</summary>
    public InvocationDeckRecipe(IReadOnlyList<string> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);
        // pack:mc08:the-invocation-deck: "shuffle all five ... Invocation cards together".
        if (cards.Count != Faces.Length || !cards.Order(StringComparer.Ordinal).SequenceEqual(Faces))
            throw new ArgumentException("an Invocation recipe requires exactly one of each of 09032 through 09036", nameof(cards));
        Cards = cards.ToImmutableArray();
    }

    /// <summary>The ordered five physical cards, outside the ordinary forty-card deck.</summary>
    public IReadOnlyList<string> Cards { get; }

    internal static bool IsInvocation(string face) => Faces.Contains(face);
}

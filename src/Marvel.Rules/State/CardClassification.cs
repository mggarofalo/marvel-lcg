namespace Marvel.Rules.State;

/// <summary>A card's printed classification, as the card dataset records it.</summary>
public static class CardClassification
{
    /// <summary>
    /// The class spelling for identity-specific cards. rr:identity-specific-card:
    /// the classification is "also sometimes called hero-specific".
    /// </summary>
    public const string HeroSpecific = "Hero";

    /// <summary>Whether the face prints <paramref name="classification"/> among its classes.</summary>
    /// <remarks>The dataset separates several printed classes with <c>;</c>.</remarks>
    public static bool Is(ICardFacts facts, string faceId, string classification)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return facts.Attributes(faceId).GetValueOrDefault("Class", string.Empty).Split(';')
            .Contains(classification, StringComparer.Ordinal);
    }

    /// <summary>The player-facing name of a printed classification.</summary>
    public static string Describe(string classification) =>
        classification == HeroSpecific ? "hero-specific" : classification;
}

namespace Marvel.Rules.State;

/// <summary>A card's printed classification, as the card dataset records it.</summary>
public static class CardClassification
{
    /// <summary>
    /// The class spelling for identity-specific cards. rr:identity-specific-card:
    /// the classification is "also sometimes called hero-specific".
    /// </summary>
    public const string HeroSpecific = "Hero";

    /// <summary>Whether the face prints <paramref name="classification"/>.</summary>
    public static bool Is(ICardFacts facts, string faceId, string classification)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return string.Equals(facts.Attributes(faceId).GetValueOrDefault("Class"), classification, StringComparison.Ordinal);
    }
}

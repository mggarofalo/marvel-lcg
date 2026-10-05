namespace Marvel.Rules.State;

/// <summary>The active identity of a card, independent of its physical printed face.</summary>
public static class EffectiveCards
{
    /// <summary>Whether an effect explicitly assigned a temporary identity.</summary>
    public static bool HasProfile(Card card) => card.InstanceState.Profile is not null;

    /// <summary>The active identity's type.</summary>
    public static CardKind Kind(Card card, ICardFacts facts) =>
        card.InstanceState.Profile?.Kind ?? facts.Kind(card.FaceId);

    /// <summary>The active identity's public title.</summary>
    public static string Title(Card card, ICardFacts facts) =>
        card.InstanceState.Profile?.Title ?? facts.Title(card.FaceId);

    /// <summary>The public face id, without the concealed physical identity.</summary>
    public static string FaceId(Card card) => card.InstanceState.Profile?.Id ?? card.FaceId;

    /// <summary>The active identity's base value before modifiers.</summary>
    public static long BaseValue(
        Card card, ICardFacts facts, string attribute, int players, long fallback = 0) =>
        card.InstanceState.Profile is { } profile
            ? profile.BaseValues.GetValueOrDefault(attribute, fallback)
            : facts.PrintedValue(card.FaceId, attribute, players, fallback);

    /// <summary>Active printed attributes; temporary identities expose only their own base values.</summary>
    public static IReadOnlyDictionary<string, string> Attributes(Card card, ICardFacts facts) =>
        card.InstanceState.Profile is { } profile
            ? profile.BaseValues.ToDictionary(pair => pair.Key,
                pair => pair.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparer.Ordinal)
            : facts.Attributes(card.FaceId);

    /// <summary>Traits on the active identity before granted traits.</summary>
    public static IReadOnlyList<string> InherentTraits(Card card, ICardFacts facts) =>
        card.InstanceState.Profile is { } profile ? profile.Traits : facts.Traits(card.FaceId);
}

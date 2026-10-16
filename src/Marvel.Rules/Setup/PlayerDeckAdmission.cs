using Marvel.Rules.State;

namespace Marvel.Rules.Setup;

/// <summary>Admits ordinary player decks before any game state is allocated.</summary>
public static class PlayerDeckAdmission
{
    /// <summary>Classifies an ordered recipe and verifies its exact signature-card membership.</summary>
    public static IReadOnlyList<Creation> Admit(
        PlayerDeckRecipe recipe, IReadOnlyList<string> signature, int player, ICardFacts facts)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentNullException.ThrowIfNull(signature);
        ArgumentNullException.ThrowIfNull(facts);
        var remaining = signature.GroupBy(card => card, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var cards = new List<Creation>();
        foreach (string spec in recipe.Cards)
        {
            if (spec.Contains(',', StringComparison.Ordinal))
                throw new ArgumentException("custom player cards require one admitted printed face", nameof(recipe));
            bool belongs = remaining.TryGetValue(spec, out int count) && count > 0;
            if (belongs) remaining[spec] = count - 1;
            cards.Add(new(spec, belongs ? CreationSource.HeroDeck : CreationSource.PlayerDeck, player));
        }
        // Core customization: "The exact quantity of each card included in that
        // hero set must be included in the deck."
        if (remaining.Values.Any(count => count != 0))
            throw new ArgumentException("the deck does not include the complete identity-specific set", nameof(recipe));
        Validate(cards, facts);
        return cards;
    }

    private static void Validate(IReadOnlyList<Creation> cards, ICardFacts facts)
    {
        // pack:mvc01:customization-rules: "minimum of 40 cards and a maximum
        // of 50 cards", one aspect plus basic, and at most three non-unique copies.
        int size = DeckConstruction.DeckSize(cards, facts);
        if (size is < 40 or > 50)
            throw new ArgumentException($"player deck has {size} cards; expected 40 through 50", nameof(cards));
        var aspects = new HashSet<string>(StringComparer.Ordinal);
        foreach (Creation card in cards)
        {
            string face = card.Faces[0];
            if (facts.Kind(face) is not (CardKind.Ally or CardKind.Event or CardKind.Resource
                or CardKind.Support or CardKind.Upgrade))
                throw new ArgumentException($"'{face}' is not an admitted player-deck card", nameof(cards));
            if (card.Source == CreationSource.HeroDeck) continue;
            string classification = facts.Attributes(face).GetValueOrDefault("Class", "");
            if (classification == "Basic") continue;
            if (classification is not ("Aggression" or "Justice" or "Leadership" or "Protection"))
                throw new ArgumentException($"'{face}' has unsupported deck classification '{classification}'", nameof(cards));
            aspects.Add(classification);
        }
        if (aspects.Count > 1)
            throw new ArgumentException("a player deck may use only one aspect", nameof(cards));
        foreach (var group in cards.Where(card => card.Source == CreationSource.PlayerDeck)
                     .GroupBy(card => facts.Title(card.Faces[0]), StringComparer.Ordinal))
        {
            long maximum = group.Select(card => facts.PrintedValue(card.Faces[0], "MaxPerDeck", 1, 3)).Min();
            if (group.Count() > maximum)
                throw new ArgumentException($"'{group.Key}' exceeds its deck copy limit of {maximum}", nameof(cards));
        }
    }
}

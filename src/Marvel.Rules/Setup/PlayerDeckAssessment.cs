using Marvel.Rules.State;

namespace Marvel.Rules.Setup;

/// <summary>Construction rules for ordinary decks made from admitted printed faces.</summary>
/// <remarks>The content owner must check face existence and supported content before calling.</remarks>
public static class PlayerDeckAssessment
{
    /// <summary>Reports corrections without consuming randomness or creating a game.</summary>
    public static DeckAssessment Assess(PlayerDeckRecipe recipe, IReadOnlyList<string> signature,
        string identity, ICardFacts facts)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentNullException.ThrowIfNull(signature);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentException.ThrowIfNullOrWhiteSpace(identity);
        var diagnostics = new List<DeckDiagnostic>();
        var cards = recipe.Cards.Where(face => !face.Contains(',', StringComparison.Ordinal)).ToList();
        foreach (string face in recipe.Cards.Where(face => face.Contains(',', StringComparison.Ordinal))
                     .Distinct(StringComparer.Ordinal))
            diagnostics.Add(new("single-face-required", face, "Choose one printed player-card face."));

        // Core Learn to Play, Customization Rules (p. 22): "The exact quantity
        // of each card included in that hero set must be included in the deck."
        var required = signature.GroupBy(face => face, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        foreach (var entry in required.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            int actual = cards.Count(face => face == entry.Key);
            if (actual != entry.Value)
                diagnostics.Add(new("signature-quantity", entry.Key,
                    $"Include exactly {entry.Value} copies of this identity-specific face.", actual, entry.Value));
        }

        int size = cards.Count(face => !facts.Attributes(face).ContainsKey("Permanent"));
        // Core Learn to Play, Customization Rules (p. 22): "a minimum of
        // 40 cards and a maximum of 50 cards"; RR Permanent excludes those cards.
        if (size is < 40 or > 50)
            diagnostics.Add(new("deck-size", "", "Include 40 through 50 counted cards.", size));

        foreach (string face in cards.Distinct(StringComparer.Ordinal))
            if (facts.Kind(face) is not (CardKind.Ally or CardKind.Event or CardKind.Resource
                or CardKind.Support or CardKind.Upgrade))
                diagnostics.Add(new("player-card-required", face, "Choose a player-deck card."));

        var customization = cards.Where(face => !required.ContainsKey(face)).ToList();
        AssessCustomization(customization, facts, diagnostics);
        AssessUniqueness(cards, identity, facts, diagnostics);
        return new(size, diagnostics);
    }

    private static void AssessCustomization(IReadOnlyList<string> cards, ICardFacts facts,
        List<DeckDiagnostic> diagnostics)
    {
        var aspects = new HashSet<string>(StringComparer.Ordinal);
        foreach (string face in cards.Distinct(StringComparer.Ordinal))
        {
            string classification = facts.Attributes(face).GetValueOrDefault("Class", "");
            if (classification == "Basic") continue;
            if (classification is "Aggression" or "Justice" or "Leadership" or "Protection")
                aspects.Add(classification);
            else
                diagnostics.Add(new("unsupported-classification", face,
                    "Choose a Basic card or a card from one supported aspect."));
        }
        // Core Learn to Play, Customization Rules (p. 22): "exactly one aspect"
        // and "cards that belong to that aspect and/or basic cards".
        if (aspects.Count > 1)
            diagnostics.Add(new("multiple-aspects", "", "Choose cards from at most one aspect.", aspects.Count, 1));

        // "No more than three copies (by title) of each non-unique card";
        // a printed MaxPerDeck overrides that general limit.
        foreach (var group in cards.GroupBy(facts.Title, StringComparer.Ordinal))
        {
            long maximum = group.Select(face => facts.PrintedValue(face, "MaxPerDeck", 1, 3)).Min();
            if (group.Count() > maximum)
                diagnostics.Add(new("copy-limit", group.First(),
                    $"Include at most {maximum} copies of {group.Key}.", group.Count(), maximum));
        }
    }

    private static void AssessUniqueness(IReadOnlyList<string> cards, string identity, ICardFacts facts,
        List<DeckDiagnostic> diagnostics)
    {
        var specifications = cards.Prepend(identity).ToArray();
        // rr:unique-icon.2: "a player cannot include multiple matching cards
        // in their deck. The identity is included in this evaluation."
        for (int right = 1; right < specifications.Length; right++)
        {
            for (int left = 0; left < right; left++)
            {
                if (!Uniqueness.Matches(facts, specifications[left].Split(','), specifications[right].Split(',')))
                    continue;
                diagnostics.Add(new("unique-match", specifications[right],
                    $"Remove the unique card matching {facts.Title(specifications[left].Split(',')[0])}."));
                break;
            }
        }
    }
}

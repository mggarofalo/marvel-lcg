using Marvel.Rules.State;

namespace Marvel.Content.Setup;

/// <summary>Product-independent deck construction checks.</summary>
public static class DeckConstruction
{
    private static readonly HashSet<string> Aspects =
        ["Aggression", "Justice", "Leadership", "Protection", "'Pool"];

    /// <summary>Validates the rules that every supported identity deck shares.</summary>
    public static void Validate(IReadOnlyList<Creation> dealt, ICardFacts facts)
    {
        ArgumentNullException.ThrowIfNull(dealt);
        ArgumentNullException.ThrowIfNull(facts);

        foreach (var player in dealt.Where(card => card.Player >= 0)
                     .GroupBy(card => card.Player))
        {
            var identities = player.Where(card => card.Source == CreationSource.Identity).ToList();
            if (identities.Count != 1)
            {
                throw new ArgumentException(
                    $"player {player.Key} has {identities.Count} identities; expected one",
                    nameof(dealt));
            }

            var identity = identities[0];
            string identitySet = facts.EncounterSet(identity.Faces[0]);
            foreach (var signature in player.Where(card =>
                         card.Source == CreationSource.HeroDeck))
            {
                if (!string.Equals(
                        identitySet, facts.EncounterSet(signature.Faces[0]),
                        StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                        $"player {player.Key}'s identity-specific card "
                        + $"'{signature.Faces[0]}' does not share the identity set icon",
                        nameof(dealt));
                }
            }

            foreach (var associated in player.Where(card =>
                         card.Source is CreationSource.Obligation or CreationSource.Nemesis))
            {
                string associatedSet = associated.Source == CreationSource.Obligation
                    ? identitySet
                    : identitySet + "_nemesis";
                if (!string.Equals(
                        associatedSet, facts.EncounterSet(associated.Faces[0]),
                        StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                        $"player {player.Key}'s obligation or nemesis card "
                        + $"'{associated.Faces[0]}' does not share the identity set icon",
                        nameof(dealt));
                }
            }

            foreach (var customized in player.Where(card =>
                         card.Source == CreationSource.PlayerDeck))
            {
                string classification = facts.Attributes(customized.Faces[0])
                    .GetValueOrDefault("Class", string.Empty);
                if (!string.Equals(classification, "Basic", StringComparison.Ordinal)
                    && !Aspects.Contains(classification))
                {
                    throw new ArgumentException(
                        $"player {player.Key}'s customizable card '{customized.Faces[0]}' "
                        + $"has non-player classification '{classification}'",
                        nameof(dealt));
                }

                if (facts.Attributes(customized.Faces[0]).TryGetValue(
                        "TeamUp", out string? named)
                    && !named.Split(';', StringSplitOptions.RemoveEmptyEntries)
                        .Any(name => IdentityMatches(facts, identity.Faces, name)))
                {
                    throw new ArgumentException(
                        $"player {player.Key}'s team-up card '{customized.Faces[0]}' "
                        + "names neither form of their identity",
                        nameof(dealt));
                }
            }

            ValidateUniqueCards(player, identity, facts, dealt);
        }

        ValidateIdentitySelection(dealt, facts);
    }

    /// <summary>Counts cards for the deck-size rule, excluding Permanent cards.</summary>
    public static int DeckSize(IEnumerable<Creation> cards, ICardFacts facts)
    {
        ArgumentNullException.ThrowIfNull(cards);
        ArgumentNullException.ThrowIfNull(facts);
        return cards.Count(card =>
            (card.Source is CreationSource.HeroDeck or CreationSource.PlayerDeck)
            && !facts.Attributes(card.Faces[0]).ContainsKey("Permanent"));
    }

    private static void ValidateUniqueCards(
        IEnumerable<Creation> player, Creation identity, ICardFacts facts,
        IReadOnlyList<Creation> dealt)
    {
        var cards = player.Where(card =>
                card.Source is CreationSource.HeroDeck or CreationSource.PlayerDeck)
            .Prepend(identity)
            .ToList();
        for (int left = 0; left < cards.Count; left++)
        {
            for (int right = left + 1; right < cards.Count; right++)
            {
                if (Uniqueness.Matches(facts, cards[left].Faces, cards[right].Faces))
                {
                    throw new ArgumentException(
                        $"player {identity.Player}'s deck contains matching unique cards "
                        + $"'{cards[left].Faces[0]}' and '{cards[right].Faces[0]}'",
                        nameof(dealt));
                }
            }
        }
    }

    private static void ValidateIdentitySelection(
        IReadOnlyList<Creation> dealt, ICardFacts facts)
    {
        var identities = dealt.Where(card => card.Source == CreationSource.Identity).ToList();
        for (int left = 0; left < identities.Count; left++)
        {
            for (int right = left + 1; right < identities.Count; right++)
            {
                if (Uniqueness.Matches(facts, identities[left].Faces, identities[right].Faces))
                {
                    throw new ArgumentException(
                        $"players {identities[left].Player} and {identities[right].Player} "
                        + "selected matching unique identities",
                        nameof(dealt));
                }
            }
        }
    }

    private static bool IdentityMatches(
        ICardFacts facts, IReadOnlyList<string> identityFaces, string printedName)
    {
        string[] names = printedName.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return names.All(name => identityFaces.Any(face =>
            string.Equals(facts.Title(face), name, StringComparison.Ordinal)
            || string.Equals(facts.Subtitle(face), name, StringComparison.Ordinal)));
    }
}

namespace Marvel.Cards.Extract;

/// <summary>Resolves printed Linked titles to stable face ids in generated data.</summary>
internal static class LinkedCards
{
    /// <summary>Adds the exact bringing face to every card with Linked.</summary>
    public static IReadOnlyList<Card> Resolve(IReadOnlyList<Card> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);
        var resolved = new List<Card>(cards.Count);
        foreach (Card card in cards)
        {
            if (!card.Attributes.TryGetValue("Linked", out string? printed))
            {
                resolved.Add(card);
                continue;
            }

            // rr:linked-card-title.3 limits the relation to the product from
            // which the linked card came. The parenthetical printed title,
            // including a type when present, identifies the bringing face
            // within that product. Generation resolves this once so gameplay
            // never guesses from names.
            Card[] candidates =
            [
                .. cards.Where(candidate =>
                    string.Equals(candidate.Pack, card.Pack, StringComparison.Ordinal)
                    && (string.Equals(printed, candidate.Name, StringComparison.Ordinal)
                        || string.Equals(
                            printed,
                            $"{candidate.Name} {KindWord(candidate.Kind)}",
                            StringComparison.OrdinalIgnoreCase))),
            ];
            if (candidates.Length != 1)
            {
                throw new InvalidDataException(
                    $"{card.Id}: Linked ({printed}) resolves to {candidates.Length} cards "
                    + $"in product '{card.Pack}'");
            }

            resolved.Add(card with { LinkedTo = [candidates[0].Id] });
        }

        return resolved;
    }

    private static string KindWord(string kind) => kind switch
    {
        "SideScheme" => "side scheme",
        "PlayerSideScheme" => "player side scheme",
        _ => kind.ToLowerInvariant(),
    };
}

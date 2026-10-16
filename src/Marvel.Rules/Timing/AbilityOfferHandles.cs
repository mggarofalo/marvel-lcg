namespace Marvel.Rules.Timing;

/// <summary>Assigns deterministic prompt-local handles to exact ability instances.</summary>
public static class AbilityOfferHandles
{
    /// <summary>Keep the first source id and give each additional instance a distinct handle.</summary>
    public static IReadOnlyList<PendingAbility> Assign(IEnumerable<PendingAbility> abilities, int cardCount)
    {
        var seen = new HashSet<int>();
        int next = cardCount;
        return [.. abilities.Select(ability => ability with
        {
            OfferId = seen.Add(ability.Card) ? null : next++,
        })];
    }
}

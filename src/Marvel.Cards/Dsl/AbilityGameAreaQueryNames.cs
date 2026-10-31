namespace Marvel.Cards.Dsl;

/// <summary>The card-query spellings that resolve within the source's game area.</summary>
/// <remarks>
/// The spellings are engine choices for the Kang scenario's "Kang", "each
/// obligation in play" and "a minion is in play" (docs/card-dsl.md).
/// </remarks>
internal static class AbilityGameAreaQueryNames
{
    internal static AbilityCardQuery? Lower(string name) => name switch
    {
        "villainInThisGameArea" => AbilityCardQuery.VillainInThisGameArea,
        "obligationsInThisGameArea" => AbilityCardQuery.ObligationsInThisGameArea,
        "minionsInThisGameArea" => AbilityCardQuery.MinionsInThisGameArea,
        _ => null,
    };
}

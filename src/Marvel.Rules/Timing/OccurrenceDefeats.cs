namespace Marvel.Rules.Timing;

/// <summary>Ordered defeat facts and unambiguous singular access for one occurrence.</summary>
internal sealed class OccurrenceDefeats
{
    private readonly List<State.Defeated> items = [];
    internal IReadOnlyList<State.Defeated> Items => items;
    internal void Add(State.Defeated defeat) => items.Add(defeat);
    internal State.Defeated? Single => items.Count switch
    {
        0 => null,
        1 => items[0],
        _ => throw new Play.RulesNotImplementedException(
            "a singular defeated-card reference requires its exact triggering instance in a multiple-defeat window"),
    };
}

namespace Marvel.Session;

/// <summary>
/// History facts collected while replaying an active prefix: every complete
/// unit, and the facts of a trailing open unit that a later decision extends.
/// </summary>
internal sealed class ActiveHistory
{
    public ActiveHistory()
    {
    }

    /// <summary>The history a construction collected through its new decision.</summary>
    public ActiveHistory(IEnumerable<HistoryUnitInspection> complete, ReplayHistory? open)
    {
        Complete.AddRange(complete);
        Open = open;
    }

    public List<HistoryUnitInspection> Complete { get; } = [];

    public ReplayHistory? Open { get; private set; }

    public void Record(ReplayHistory facts, JournalUnit unit, int unitIndex)
    {
        if (unit.Status == "complete")
        {
            facts.Append(Complete, unit, unitIndex);
        }
        else
        {
            Open = facts;
        }
    }
}

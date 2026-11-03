namespace Marvel.Session;

/// <summary>
/// History facts collected while replaying an active prefix: every complete
/// unit, and the facts of a trailing open unit that a later decision extends.
/// </summary>
internal sealed class ActiveHistory
{
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

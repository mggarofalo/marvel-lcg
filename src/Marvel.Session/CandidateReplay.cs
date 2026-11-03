using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Session;

/// <summary>
/// A verified active game that becomes the construction-mode candidate for one
/// more decision, and the history facts the verifying replay collected.
/// </summary>
/// <remarks>
/// The candidate observes its one new decision exactly as a verifying replay
/// observes a recorded one, so <see cref="History"/> equals
/// <see cref="SessionReplay.InspectActiveHistory"/> of the proposed save without
/// replaying that save a second time.
/// </remarks>
public sealed class CandidateReplay
{
    private readonly ActiveHistory history;
    private ReplayHistory? unit;

    internal CandidateReplay(Game game, ActiveHistory history)
    {
        Game = game;
        this.history = history;
    }

    /// <summary>The verified game that the new decision resolves on.</summary>
    public Game Game { get; }

    /// <summary>Resolves the new decision and observes its history facts.</summary>
    public Resolution Resolve(int decidingSeat, Decision decision, bool startsUnit)
    {
        ArgumentNullException.ThrowIfNull(decision);
        Prompt prompt = Game.Pending
            ?? throw new ReplayDivergenceException("candidate has no pending prompt");
        ReplayHistory facts = startsUnit
            ? new ReplayHistory()
            : history.Open ?? throw new ReplayDivergenceException(
                "a dependent decision has no open history unit");
        if (startsUnit)
        {
            facts.ObserveRoot(decidingSeat, Game, prompt, decision);
        }

        facts.ObserveResources(decidingSeat, decision, Game);
        Resolution resolved = Game.Resolve(decision);
        facts.ObserveEvents(resolved.Events);
        unit = facts;
        return resolved;
    }

    /// <summary>The active history of the save that records the new decision.</summary>
    public IReadOnlyList<HistoryUnitInspection> History(SessionSave proposed)
    {
        ArgumentNullException.ThrowIfNull(proposed);
        ReplayHistory facts = unit ?? throw new InvalidOperationException(
            "the candidate has not resolved a decision");
        int index = proposed.Cursor - 1;
        JournalUnit last = proposed.Units[index];
        var complete = new List<HistoryUnitInspection>(history.Complete);
        if (last.Status == "complete")
        {
            facts.Append(complete, last, index);
        }

        return complete;
    }
}

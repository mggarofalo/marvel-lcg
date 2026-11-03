using Marvel.Rules.Play;

namespace Marvel.Session;

using static Marvel.Session.SessionReplayVerification;

/// <summary>
/// Verifies a save's newest decision on a game that reached the save before it,
/// exactly as a full replay verifies that decision.
/// </summary>
internal static class CommittedDecisionVerification
{
    /// <summary>
    /// Advances <paramref name="game"/>, verified at the end of
    /// <paramref name="live"/>, through the one decision
    /// <paramref name="committed"/> appends.
    /// </summary>
    /// <remarks>
    /// The decision either starts the committed save's last unit or extends the
    /// open unit that ended the live save, so the unit's position, role and
    /// earlier exposures come from the live save, where they were verified.
    /// </remarks>
    internal static void Replay(SessionSave live, SessionSave committed, Game game)
    {
        int unitIndex = committed.Cursor - 1;
        JournalUnit unit = committed.Units[unitIndex];
        int decisionIndex = unit.Decisions.Count - 1;
        JournalUnit? extended = decisionIndex == 0 ? null : live.Units[unitIndex];
        if (extended is null)
        {
            RequireUnitPosition(unit, unitIndex, game);
        }

        string? role = extended?.Role;
        IReadOnlyList<InformationExposure> exposures = extended?.Exposures ?? [];
        _ = ReplayUnitDecision(
            unit, unitIndex, decisionIndex, game, inspection: null, ref role, ref exposures);
        RequireUnitBoundary(unit, unitIndex, game, role);
        RequireReplayExposures(requireExposures: true, unit, unitIndex, exposures);
        RequireReplayFrontier(committed, committed.Units.Count, requireExposures: true);
        SessionReplay.RequireCurrentPrompt(committed.CurrentPrompt, game.Pending);
    }
}

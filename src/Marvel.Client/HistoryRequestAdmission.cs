namespace Marvel.Client;

/// <summary>Owns the progress-state permission shared by history admission and its explanation.</summary>
internal static class HistoryRequestAdmission
{
    internal static bool Allows(GameProgressPresentation? progress) =>
        progress?.OperationalLock is null
        && (progress?.LocksDecisions == false || progress?.Kind is GameProgressKind.WaitingForOtherPlayer
            or GameProgressKind.PlayersWin or GameProgressKind.PlayersLose or GameProgressKind.VillainWins);
}

using System.Collections.Immutable;

namespace Marvel.Rules.Campaign;

/// <summary>Applies the five scenarios' victory and defeat instructions to verified terminal facts.</summary>
public static class CampaignOutcomeRules
{
    /// <summary>Records a terminal attempt once and exposes its ordered reward continuation.</summary>
    public static CampaignState Finish(
        CampaignState state, CampaignDefinition definition, CampaignAttemptOutcome outcome)
    {
        CampaignRules.RequirePhase(state, CampaignPhase.Playing);
        Validate(state, definition, outcome);
        if (!outcome.Won)
            return state with { Phase = state.Expert && state.Stage == CampaignStage.RedSkull
                ? CampaignPhase.Failed : CampaignPhase.Retry };
        if (state.Stage == CampaignStage.RedSkull)
            return state with { Phase = CampaignPhase.Complete };
        state = RecordPlayers(state, outcome);
        state = RecordScenario(state, outcome);
        ImmutableArray<int> seats = [.. Enumerable.Range(0, state.Players.Length)
            .Where(seat => Eligible(state, outcome.Seats[seat]))];
        state = state with { PendingOutcome = outcome, RewardSeats = seats, Phase = CampaignPhase.Rewards };
        return seats.IsEmpty ? Advance(state) : state;
    }

    internal static CampaignState Advance(CampaignState state) => state with
    {
        Stage = state.Stage + 1,
        Phase = CampaignPhase.Preparing,
        Attempt = checked(state.Attempt + 1),
        RewardSeats = [],
        PendingOutcome = null,
    };

    private static bool Eligible(CampaignState state, CampaignSeatOutcome seat)
    {
        if (state.Expert && seat.Defeated) return false;
        return state.Stage switch
        {
            CampaignStage.Crossbones or CampaignStage.AbsorbingMan => true,
            CampaignStage.Zola => !state.PendingOutcome!.HydraPrisonInPlay && seat.HeroForm
                && state.Players[seat.Seat].Cards.Any(card => card.Role == CampaignCardRole.Condition),
            _ => false,
        };
    }

    private static CampaignState RecordPlayers(CampaignState state, CampaignAttemptOutcome outcome)
    {
        var players = state.Players;
        foreach (var seat in outcome.Seats)
        {
            CampaignPlayerState player = players[seat.Seat];
            // pack:mc10:persistent-damage: "record their base hit points" when remaining HP exceeds it.
            if (state.Expert)
                player = player with { RecordedHitPoints = seat.Defeated ? 0
                    : Math.Min(seat.RemainingHitPoints, seat.BaseHitPoints), RequiresRecovery = seat.Defeated };
            if (state.Stage == CampaignStage.Zola && (!state.Expert || !seat.Defeated))
                player = player with { EngagedWithEnemy = seat.EngagedWithEnemy };
            if (state.Stage == CampaignStage.Taskmaster && (!state.Expert || !seat.Defeated))
                player = player with { Cards = player.Cards.AddRange(seat.RescuedAllies) };
            players = players.SetItem(seat.Seat, player);
        }
        return state with { Players = players, PendingOutcome = outcome };
    }

    private static CampaignState RecordScenario(CampaignState state, CampaignAttemptOutcome outcome)
    {
        return state.Stage switch
        {
            CampaignStage.Crossbones => state with { ExperimentalWeapons = outcome.ExperimentalWeapons },
            CampaignStage.AbsorbingMan => state with { DelayCounters = outcome.DelayCounters },
            CampaignStage.Zola when outcome.HydraPrisonInPlay => BanPrisoners(state, outcome.ImprisonedAllyFaces),
            _ => state,
        };
    }

    private static CampaignState BanPrisoners(CampaignState state, ImmutableArray<string> faces) => state with
    {
        BannedAllyFaces = [.. state.BannedAllyFaces.Concat(faces).Distinct(StringComparer.Ordinal)],
        Players = [.. state.Players.Select(player => player with
        {
            Cards = [.. player.Cards.Where(card => !faces.Contains(card.Face))],
            Deck = new Setup.PlayerDeckRecipe([.. player.Deck.Cards.Where(face => !faces.Contains(face))]),
        })],
    };

    private static void Validate(
        CampaignState state, CampaignDefinition definition, CampaignAttemptOutcome outcome)
    {
        if (outcome.Seats.Length != state.Players.Length || outcome.DelayCounters < 0)
            throw new ArgumentException("terminal campaign facts have invalid dimensions", nameof(outcome));
        for (int seat = 0; seat < outcome.Seats.Length; seat++)
            ValidateSeat(state, definition, outcome.Seats[seat], seat);
        if (outcome.ExperimentalWeapons.Any(face => !definition.ExperimentalWeaponFaces.Contains(face)))
            throw new ArgumentException("unknown experimental weapon", nameof(outcome));
    }

    private static void ValidateSeat(
        CampaignState state, CampaignDefinition definition, CampaignSeatOutcome outcome, int seat)
    {
        if (outcome.Seat != seat || outcome.BaseHitPoints != state.Players[seat].BaseHitPoints
            || outcome.RemainingHitPoints < 0)
            throw new ArgumentException("terminal campaign seat facts are inconsistent", nameof(outcome));
        if (outcome.RescuedAllies.Any(card => card.Role != CampaignCardRole.RescuedAlly
            || !definition.CaptiveAllyFaces.Contains(card.Face)))
            throw new ArgumentException("an unadmitted captive ally was reported", nameof(outcome));
    }
}

using System.Collections.Immutable;
using Marvel.Rules.Campaign;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Server;

/// <summary>Reads campaign facts from authoritative engine state without replay side effects.</summary>
internal static class CampaignInspection
{
    internal static CampaignGameProgress Read(Game game, CampaignDefinition definition)
    {
        var world = game.State;
        var attempt = world.Campaign ?? throw new ArgumentException("game is not a campaign attempt", nameof(game));
        var tracked = attempt.Cards;
        return new CampaignGameProgress(
            [.. tracked.Where(card => card.Removed)
                .Select(card => new CampaignCardRemoval(RosterSeat(attempt, card.Binding.Seat), card.Binding.Card.Key))],
            [.. tracked.Where(card => card.Assigned)
                .Select(card => new CampaignObligationAssignment(RosterSeat(attempt, card.Binding.Seat), card.Binding.Card))],
            world.IsOver ? Outcome(world, attempt, definition) : null);
    }

    private static CampaignAttemptOutcome Outcome(World world, CampaignAttemptState attempt, CampaignDefinition definition)
    {
        var prison = world.Cards.FirstOrDefault(card => card.FaceId == definition.HydraPrisonFace
            && DeckTypes.IsInPlay(card.Area.Type));
        return new CampaignAttemptOutcome(world.Result == Marvel.Rules.Play.Outcome.PlayersWin,
            [.. world.Seats.Select((seat, index) => Seat(world, attempt, index))],
            [.. attempt.EnteredExperimentalWeapons.Select(card => card.FaceId)],
            checked((int)(world.TheCardIn(DeckType.MainSchemesArea)?.Tokens.GetValueOrDefault(definition.DelayCounterField) ?? 0)),
            prison is not null,
            prison is null ? [] : [.. world.Areas.Where(area => area.Host == prison.ObjectId
                    && !DeckTypes.IsInPlay(area.Type)).SelectMany(area => area.Cards)
                .Where(card => world.Facts.Kind(card.FaceId) == CardKind.Ally).Select(card => card.FaceId)]);
    }

    private static int RosterSeat(CampaignAttemptState attempt, int liveSeat)
    {
        var seat = attempt.Setup.Seats.Single(value => value.Seat == liveSeat);
        return seat.CampaignSeat ?? seat.Seat;
    }

    private static CampaignSeatOutcome Seat(World world, CampaignAttemptState attempt, int index)
    {
        var seat = world.Seats[index];
        var identity = seat.IdentityCard;
        int baseHealth = checked((int)world.Facts.PrintedValue(identity.FaceId, "HP", world.Players));
        // ruling:1516351f87fc4382 keeps the recorded cap at starting hit points.
        // The campaign Condition grants its extra HP at the next game's setup.
        return new CampaignSeatOutcome(RosterSeat(attempt, index), seat.Eliminated,
            world.Facts.Kind(identity.FaceId) == CardKind.Hero,
            seat.Eliminated ? 0 : checked((int)Math.Max(0, DamagePlacement.Health(world, world.Facts, identity) - identity.Damage)),
            baseHealth,
            world.Areas.Any(area => area.Type == DeckType.EngagedEnemiesArea
                && area.PlayArea == PlayArea.Of(index) && area.Cards.Count > 0),
            [.. attempt.Cards.Where(card => card.Rescued && card.Binding.Seat == index)
                .Select(card => card.Binding.Card)]);
    }
}

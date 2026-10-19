using Marvel.Rules.Campaign;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Campaign;

public sealed class CampaignObligationDrawTests
{
    [Fact]
    [Rule("rr:draw-drawing-cards")]
    public void OrdinaryDrawCountsObligationWithoutReplacementAndContinuesRemainingDraws()
    {
        // pack:mc10:obligations-in-player-decks: immediately put into play,
        // do not draw a replacement. Drawing two still processes the second draw.
        var game = Started();
        var world = game.State;
        PrepareEmptyHand(world);
        Card obligation = StageObligation(world);
        int before = world.Seats[0].Deck.Cards.Count;
        var events = new List<GameEvent>();
        Draw.Cards(world, 0, 2, "test draw", events);
        Assert.Single(world.Seats[0].Hand.Cards);
        Assert.Equal(before - 2, world.Seats[0].Deck.Cards.Count);
        Assert.Equal(DeckType.ObligationsArea, obligation.Area.Type);
        Assert.Equal(0, obligation.Area.PlayArea.Player);
        Assert.True(obligation.FaceUp);
        Assert.Contains(events.OfType<CardsMoved>(), moved => moved.Verb == "Put_Into_Play"
            && moved.Cards.Any(landing => landing.Card == obligation.ObjectId));
    }

    [Fact]
    [Rule("rr:appendix-ii-setup.step.14")]
    public void OpeningHandObligationEntersPlayWithoutReplacement()
    {
        // A synthetic villain Setup places a known obligation atop the deck before step14.
        var game = CampaignRecoveryFixture.Start(OwnedSetup(), forceOpeningObligation: true);
        Assert.Equal(GamePhase.Mulligan, game.Phase);
        Assert.Equal(2, game.State.Seats[0].Hand.Cards.Count);
        Assert.Equal(DeckType.ObligationsArea, game.State.Cards.Single(card => card.FaceId == "obligation-a").Area.Type);
        Assert.DoesNotContain(game.State.Seats[0].Hand.Cards, card => card.FaceId.StartsWith("obligation", StringComparison.Ordinal));
    }

    [Fact]
    [Rule("rr:appendix-ii-setup.step.15")]
    public void MulliganObligationConsumesOneDrawWithoutStoppingRemainingDraws()
    {
        var game = Started();
        var world = game.State;
        PrepareEmptyHand(world);
        foreach (Card card in world.Seats[0].Deck.Cards.Where(card => card.FaceId == "player").Take(3).ToArray())
            World.MoveToTop(card, world.Seats[0].Hand);
        var replaced = world.Seats[0].Hand.Cards.Take(2).Select(card => card.ObjectId).ToArray();
        Card obligation = StageObligation(world);
        int before = world.Seats[0].Deck.Cards.Count;
        game = Game.BeginWithoutCardAbilities(world, world.Facts);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances).Id, replaced, []));
        Assert.Equal(2, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(before - 2, world.Seats[0].Deck.Cards.Count);
        Assert.Equal(DeckType.ObligationsArea, obligation.Area.Type);
    }

    [Fact]
    [Rule("rr:end-of-player-phase.step.2")]
    public void EndPhaseDrawToHandSizeCountsObligationsWithoutReplacingThem()
    {
        var game = Started();
        var world = game.State;
        PrepareEmptyHand(world);
        Card obligation = StageObligation(world);
        int before = world.Seats[0].Deck.Cards.Count;
        PhaseEnd.DrawToHandSize(world, world.Facts, []);
        Assert.Equal(2, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(before - 3, world.Seats[0].Deck.Cards.Count);
        Assert.Equal(DeckType.ObligationsArea, obligation.Area.Type);
    }

    [Fact]
    [Rule("rr:appendix-ii-setup.step.14")]
    [Rule("rr:hand-size")]
    public void OpeningDrawObservesHandSizePenaltyOfObligationThatJustEntered()
    {
        // Synthetic constant models the printed Martial Law hand-size penalty.
        var game = CampaignRecoveryFixture.Start(OwnedSetup(), forceOpeningObligation: true, handPenalty: true);
        Assert.Single(game.State.Seats[0].Hand.Cards);
        Assert.Equal(2, PhaseEnd.HandSize(game.State, game.State.Seats[0], game.State.Facts));
    }

    [Fact]
    [Rule("rr:end-of-player-phase.step.2")]
    [Rule("rr:hand-size")]
    public void EndPhaseDrawObservesHandSizePenaltyOfObligationThatJustEntered()
    {
        var game = CampaignRecoveryFixture.Start(OwnedSetup(), handPenalty: true);
        var world = game.State;
        PrepareEmptyHand(world);
        StageObligation(world);
        PhaseEnd.DrawToHandSize(world, world.Facts, []);
        Assert.Single(world.Seats[0].Hand.Cards);
        Assert.Equal(2, PhaseEnd.HandSize(world, world.Seats[0], world.Facts));
    }

    private static Game Started() => CampaignRecoveryFixture.Start(OwnedSetup());

    private static CampaignGameSetup OwnedSetup()
    {
        var setup = CampaignRecoveryFixture.Setup(stage: CampaignStage.Crossbones, available: false);
        return setup with { Seats = [setup.Seats[0] with { Cards = [CampaignRecoveryFixture.ObligationA] }] };
    }

    private static void PrepareEmptyHand(World world)
    {
        foreach (var card in world.Seats[0].Hand.Cards.ToArray()) World.MoveToTop(card, world.Seats[0].Deck);
        foreach (var card in world.Cards.Where(card => card.FaceId.StartsWith("obligation", StringComparison.Ordinal)))
            World.MoveToTop(card, world.Seats[0].SetAside);
    }

    private static Card StageObligation(World world)
    {
        Card card = world.Cards.Single(value => value.FaceId == "obligation-a");
        World.MoveToTop(card, world.Seats[0].Deck);
        return card;
    }
}

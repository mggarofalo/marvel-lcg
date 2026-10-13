using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

public sealed class ComposedActivityProjectionTests
{
    [Rule("rr:cost.3")]
    [Rule("rr:forced.1")]
    [Fact]
    public void BlackCatPaymentAndForcedDeckDiscardHaveSeparateInspectableResults()
    {
        // Resources are spent "by discarding cards from their hand"; forced responses
        // "must be resolved when their triggering conditions are met". Core 01002:
        // "After you play Black Cat, discard the top 2 cards of your deck."
        var (game, world, cat, strength, discarded) = BlackCatGame();
        Affordance play = Assert.Single(game.Pending!.Affordances,
            option => option.AnchorId == cat.ObjectId && option.Verb == CardPlay.Verb);
        Resolution result = game.Resolve(Decision.Take(play.Id, [], [strength.ObjectId]));
        ActivityCompleted[] receipts = result.Events.OfType<ActivityCompleted>().ToArray();
        Assert.Equal(2, receipts.Length);
        Assert.NotEqual(receipts[0].Batch, receipts[1].Batch);
        Assert.Equal([strength.ObjectId], receipts[0].Participants[0].Cards);
        Assert.Equal(discarded, receipts[1].Participants[0].Cards);
        Assert.Equal(DeckType.AlliesArea, cat.Area.Type);
        foreach (int card in discarded.Append(strength.ObjectId))
            Assert.Equal(DeckType.DiscardPile, world.Cards[card].Area.Type);

        VisibleResult visible = WorldProjection.For(world, result.Prompt, result.Events,
            new PermissiveVisibilityPolicy().Authorize(null, world.Players));
        EventBatchPresentation presentation = EventCuePlanner.Plan(visible.Events, visible.World, Outcome.Unfinished);
        EventPresentation payment = Assert.Single(presentation.Cues,
            cue => cue.Summary == "Spider-Man discarded Strength.");
        Assert.Equal([strength.ObjectId], Assert.Single(payment.Details).Anchors);
        EventPresentation response = Assert.Single(presentation.Cues, cue => cue.ForcedAbilityNotification is not null);
        Assert.Contains("Black Cat", response.Summary);
        Assert.DoesNotContain("Strength", response.Summary);
        Assert.Equal(discarded, Assert.Single(response.Details).Anchors);
    }

    [Fact]
    public void ComposedDrawInstructionsRetainPublicCountsWithoutLeakingAnotherSeatsCards()
    {
        // Synthetic composition contract using Core printed cards and the real draw/filter paths.
        var world = new World(PersistentFixture.Cards, 2, seed: 7);
        for (int player = 0; player < 2; player++)
        {
            Seat seat = world.CreateSeat(player == 0 ? "Spider-Man" : "Captain Marvel");
            seat.IdentityCard = world.CreateCard(player == 0 ? "01001a" : "01010a", seat.Hero);
            for (int card = 0; card < 3; card++) world.CreateCard(player == 0 ? "01002" : "01092", seat.Deck);
        }
        var events = new List<GameEvent>();
        var child = new List<GameEvent>();
        Draw.Cards(world, 0, 1, "same cause", events);
        Draw.Cards(world, 1, 2, "same cause", child);
        ActivityBatches.Append(events, child);
        VisibleResult visible = WorldProjection.For(world, null, events,
            new RestrictedVisibilityPolicy(0).Authorize(null, world.Players));
        GameEvent[] transported = visible.Events.Select(item => EventJson.Read(EventJson.Write(item))).ToArray();
        ActivityCompleted[] receipts = transported.OfType<ActivityCompleted>().ToArray();
        Assert.Equal(2, receipts.Length);
        Assert.NotEqual(receipts[0].Batch, receipts[1].Batch);
        Assert.Single(receipts[0].Participants[0].Cards);
        Assert.Equal(2, receipts[1].Participants[0].Count);
        Assert.Empty(receipts[1].Participants[0].Cards);
        Assert.Null(receipts[1].Subjects);
        Assert.DoesNotContain("Helicarrier", string.Join("", transported.Select(EventJson.Write)));
        EventBatchPresentation presentation = EventCuePlanner.Plan(transported, visible.World, Outcome.Unfinished);
        Assert.Equal(2, presentation.Cues.Count);
        Assert.Single(presentation.Cues[0].Details);
        Assert.Empty(presentation.Cues[1].Details);
        Assert.Equal("Captain Marvel drew 2 cards.", presentation.Cues[1].Summary);
    }

    private static (Game Game, World World, Card Cat, Card Strength, int[] Discarded) BlackCatGame()
    {
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var runner = new AbilityRunner(PersistentFixture.Abilities);
        World world = WorldSetup.Deal(PersistentFixture.Cards,
            Blueprints.From(Dealer.DealOrder(setup, "rhino", ["spider_man"]), PersistentFixture.Cards),
            ["Spider-Man"], 12345, runner);
        Seat seat = world.Seats[0];
        Card cat = world.Cards.Single(card => card.FaceId == "01002");
        Card strength = world.Cards.Single(card => card.FaceId == "01090");
        Card helicarrier = world.Cards.Single(card => card.FaceId == "01092");
        Card shooter = world.Cards.First(card => card.FaceId == "01008");
        foreach (Card card in seat.Hand.Cards.ToArray()) World.MoveToTop(card, seat.Deck);
        World.MoveToTop(cat, seat.Hand);
        World.MoveToTop(strength, seat.Hand);
        while (seat.Hand.Cards.Count < 6)
            World.MoveToTop(seat.Deck.Cards.First(card => card != helicarrier && card != shooter), seat.Hand);
        World.MoveToTop(helicarrier, seat.Deck);
        World.MoveToTop(shooter, seat.Deck);
        // Prepared Core hand/deck state; the play itself and its forced response use the public game loop.
        Game game = Game.Begin(world, PersistentFixture.Cards, runner);
        game.Resolve(Decision.Decline);
        game.Resolve(Decision.Take(game.Pending!.Affordances.Single(option => option.Verb == Game.ChangeForm).Id));
        return (game, world, cat, strength, [shooter.ObjectId, helicarrier.ObjectId]);
    }
}

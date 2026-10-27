using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic card-level restriction and action; no Hulk production admission.</summary>
public sealed class HandResourceFormTests
{
    [Theory]
    [InlineData("10001a", true)]
    [InlineData("10001b", false)]
    [Rule("rr:resource.1")]
    [Rule("rr:the-golden-rules")]
    public void PrintedHandResourcesRequireTheOwnersLiveHeroForm(string face, bool allowed)
    {
        // Generate resources "by discarding cards from their hand"; card text takes precedence.
        // Printed 10007: "Spend this card only in hero form."
        var (world, runner, source, payment) = Board(face);
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        var offers = CardPayment.Generators(world, world.Facts, world.Seats[0], runner);
        Assert.Equal(allowed, offers.Any(item => item.Effect == payment.ObjectId));
        Assert.Equal(allowed, runner.Actions(world, 0).Any(item => item.Card == source.ObjectId));
        Assert.Empty(runner.ResourceAbilities(world, 0));
        Assert.Equal("RRR", Resources.GeneratedBy(payment.FaceId, world.Facts));
        Assert.Equal(3, Resources.PrintedCount([payment], 'R', world.Facts));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(pending, world.Agenda.Outstanding);
        if (allowed)
        {
            var action = Assert.Single(runner.Actions(world, 0));
            runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], []));
            Assert.Equal(1, source.Tokens["c_test"]);
            Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        }
        else
        {
            // Same-cost unrestricted cards distinguish form rejection from an unusable action.
            world.CreateCard("01090", world.Seats[0].Hand);
            world.CreateCard("10002", world.Seats[0].Hand);
            Assert.Single(runner.Actions(world, 0));
        }
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void AStaleHeroPaymentRejectsBeforeMutationAndCanBeRetriedInHeroForm()
    {
        // Step 5 aborts "without paying any costs" if costs cannot be paid.
        var (world, runner, source, payment) = Board("10001a");
        var action = Assert.Single(runner.Actions(world, 0));
        var price = Assert.Single(runner.Describe(world, action).CostOptions);
        var answer = new PlayerAction(action, [payment.ObjectId], [],
            Allocations: ResourcePayment.Allocate(price, [payment.ObjectId]));
        world.Seats[0].IdentityCard.TurnTo("10001b");
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world, answer));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Equal(DeckType.HandsArea, payment.Area.Type);
        world.Seats[0].IdentityCard.TurnTo("10001a");
        runner.ExecuteAction(world, answer);
        Assert.Equal(1, source.Tokens["c_test"]);
    }

    [Fact]
    public void RestrictionAlsoAppliesToPrintedCardPayment()
    {
        var (world, runner, _, payment) = Board("10001a");
        var eventCard = world.CreateCard("10014", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == eventCard.ObjectId);
        world.Seats[0].IdentityCard.TurnTo("10001b");
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
            new PlayerAction(action, [payment.ObjectId], [])));
        Assert.Equal(DeckType.HandsArea, eventCard.Area.Type);
        Assert.Equal(DeckType.HandsArea, payment.Area.Type);
        world.Seats[0].IdentityCard.TurnTo("10001a");
        runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], []));
        Assert.Equal(DeckType.DiscardPile, eventCard.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
    }

    [Fact]
    public void BlankingPrintedTextDoesNotEraseThePrintedResourceIcons()
    {
        var (world, runner, _, payment) = Board("10001a");
        // Synthetic in-play blank isolates icon inspection from payment generation.
        World.MoveToTop(payment, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, PrintedText.Blanked,
            Card: payment.ObjectId, Affects: payment.ObjectId,
            Lasts: Duration.UntilEndOf(TimingPoints.EndOfPhase)) { TargetIncarnation = payment.Incarnation });
        Assert.True(PrintedText.IsBlanked(world, payment));
        Assert.Equal("RRR", Resources.GeneratedBy(payment.FaceId, world.Facts));
        Assert.Equal(3, Resources.PrintedCount([payment], 'R', world.Facts));
        Assert.Equal("RRR", runner.ResourcesGeneratedBy(world, payment, null));
    }

    private static (World World, AbilityRunner Runner, Card Source, Card Payment) Board(string face)
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"10007","handResourceForm":"hero","abilities":[]},
              {"card":"01006","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "cost":{"spend":"RRR"},
                "effect":{"placeCounters":{"card":"this","counter":"test","count":1}}}]},
              {"card":"10014","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "effect":{"placeCounters":{"card":"you","counter":"test","count":1}}}]}
            ]}
            """));
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 1, 17);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("10001a,10001b", seat.Hero);
        seat.IdentityCard.TurnTo(face);
        for (int i = 0; i < 8; i++) world.CreateCard("01003", seat.Deck);
        var source = world.CreateCard("01006", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var payment = world.CreateCard("10007", seat.Hand);
        world.Abilities = runner;
        return (world, runner, source, payment);
    }
}

using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Production Astral body on nonpublic legal-state probes; public games are separate evidence.
public sealed class AstralOfferedSchemeTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [Rule("rr:target.2")]
    [Rule("rr:target.3.3")]
    [Rule("rr:confuse-confused.5.1")]
    [Rule("rr:crisis-icon.1")]
    public void EveryOfferedSchemeCanInitiateBeforeAnyCostIsPaid(bool confused, bool crisis)
    {
        // An ability can initiate only with "at least one valid target"; its
        // cost "is not considered" when deciding whether it affects a target.
        // A confused character may thwart "even if it has no valid target".
        // A crisis icon prevents removal "from the main scheme".
        var (world, runner, source, payment, main, side) = Board(confused, crisis);
        var (control, controlRunner, controlSource, controlPayment, controlMain, controlSide) = Board(confused, crisis);
        string before = Observe(world);
        for (int repeat = 0; repeat < 3; repeat++)
        {
            var action = Assert.Single(runner.Actions(world, 0), item => item.Card == source.ObjectId);
            var offer = runner.Describe(world, action);
            Assert.Equal(confused ? new[] { main.ObjectId, side.ObjectId } : new[] { side.ObjectId },
                offer.Targets!.Legal);
            Assert.Equal(1, offer.Targets.Min);
            Assert.Equal(1, offer.Targets.Max);
            Assert.Contains("Choose one scheme", offer.CostDescription);
        }
        Assert.Equal(before, Observe(world));
        if (!confused)
        {
            var action = Assert.Single(runner.Actions(world, 0), item => item.Card == source.ObjectId);
            Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
                new PlayerAction(action, [payment.ObjectId], [main.ObjectId])));
            Assert.Equal(before, Observe(world));
            Assert.Same(world.Seats[0].Hand, source.Area);
            Assert.Same(world.Seats[0].Hand, payment.Area);
        }
        int target = confused ? main.ObjectId : side.ObjectId;
        int controlTarget = confused ? controlMain.ObjectId : controlSide.ObjectId;
        var events = Begin(world, runner, source, payment, target);
        var controlEvents = Begin(control, controlRunner, controlSource, controlPayment, controlTarget);
        Assert.Equal(controlEvents, events);
        Assert.Equal(Observe(control), Observe(world));
        Assert.Equal(crisis ? 8 : 0, main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(confused ? 8 : 4, side.Tokens.GetValueOrDefault("k_threat"));
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Confused));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
    }

    private static string[] Begin(World world, AbilityRunner runner, Card source, Card payment, int target)
    {
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == source.ObjectId);
        var events = runner.ExecuteAction(world,
            new PlayerAction(action, [payment.ObjectId], [target])).Events.ToList();
        if (Sequence.Work(world, world.Facts, runner, events) is { } order)
        {
            Assert.Equal(Question.Order, order.Asking);
            Sequence.Answer(world, world.Facts, runner, order,
                Decision.Take(Assert.Single(order.Affordances).Id, [0, 1], []), events);
            var look = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
            Assert.Equal(PublicDecisionKind.CardLook, look.PublicKind);
            Assert.False(world.Cards[Assert.Single(look.ContextCardIds)].FaceUp);
            Sequence.Answer(world, world.Facts, runner, look,
                Decision.Take(Assert.Single(look.Affordances).Id), events);
        }
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.False(world.Agenda.IsBusy);
        return events.Select(item => item.ToString()).ToArray();
    }

    private static (World, AbilityRunner, Card, Card, Card, Card) Board(bool confused, bool crisis)
    {
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        if (crisis) main.PlaceTokens("k_threat", 8);
        var side = world.CreateCard(crisis ? "01108" : "01107", world.AreaOf(DeckType.SideSchemesArea));
        side.PlaceTokens("k_threat", 8);
        var source = world.CreateCard("09003", world.Seats[0].Hand);
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        if (confused) Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Confused);
        return (world, runner, source, payment, main, side);
    }

    private static string Observe(World world) => System.Text.Json.JsonSerializer.Serialize(new
    {
        Digest = world.Digest().Canonical(), Random = world.Random.Generator.GetState(),
        world.Random.Generator.WordsConsumed, world.Agenda.Current,
        Outstanding = world.Agenda.Outstanding.ToArray(),
    });
}

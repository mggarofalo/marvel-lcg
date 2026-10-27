using System.Text.Json;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Production 09003 and 04154 on a prepared legal board; public games are separate evidence.
public sealed class AstralDefeatReceiptTests
{
    [Theory]
    [InlineData(false, 1, false)]
    [InlineData(true, 1, false)]
    [InlineData(false, 2, false)]
    [InlineData(true, 2, false)]
    [InlineData(false, 1, true)]
    [InlineData(true, 1, true)]
    [Rule("rr:and.1")]
    [Rule("rr:when-defeated-abilities.2.1")]
    [Rule("rr:target.4.1")]
    public void DefeatedChosenSchemeDoesNotRetargetAfterRealSearchAndShuffle(bool inspectFirst, int players, bool reenter)
    {
        // Independent effects "are not dependent on each other". A defeated card
        // leaves "after" its When Defeated ability. The receipt tail "does not
        // resolve against" the departed chosen scheme.
        var (world, runner, source, payment, patrol, other) = Board(players);
        var (control, controlRunner, controlSource, controlPayment, controlPatrol, _) = Board(players);
        string before = Observe(world);
        for (int repeat = 0; repeat < 3; repeat++)
        {
            var action = Assert.Single(runner.Actions(world, 0), item => item.Card == source.ObjectId);
            Assert.Contains(patrol.ObjectId, runner.Describe(world, action).Targets!.Legal);
        }
        Assert.Equal(before, Observe(world));
        var events = Resolve(world, runner, source, payment, patrol, inspectFirst, reenter);
        var controlEvents = Resolve(control, controlRunner, controlSource, controlPayment, controlPatrol, inspectFirst, reenter);
        Assert.Equal(controlEvents.Select(item => item.ToString()), events.Select(item => item.ToString()));
        Assert.Equal(Observe(control), Observe(world));
        Assert.Equal(reenter ? DeckType.SideSchemesArea : DeckType.EncounterDiscardPile, patrol.Area.Type);
        Assert.Equal(reenter ? 6 : 0, patrol.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(7, other.Tokens["k_threat"]);
        Assert.Equal(8, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens["k_threat"]);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    private static List<GameEvent> Resolve(World world, AbilityRunner runner, Card source,
        Card payment, Card patrol, bool inspectFirst, bool reenter)
    {
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == source.ObjectId);
        var events = runner.ExecuteAction(world,
            new PlayerAction(action, [payment.ObjectId], [patrol.ObjectId])).Events.ToList();
        var order = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(Question.Order, order.Asking);
        Sequence.Answer(world, world.Facts, runner, order,
            Decision.Take(Assert.Single(order.Affordances).Id, inspectFirst ? [1, 0] : [0, 1], []), events);
        int searches = 0, looks = 0;
        long rngBefore = world.Random.Generator.WordsConsumed;
        for (int step = 0; step < 12; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null) break;
            if (prompt.Cancellable)
            {
                Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, events);
                continue;
            }
            if (prompt.Asking == Question.Order && prompt.Affordances[0].Targets is { } targets)
            {
                Sequence.Answer(world, world.Facts, runner, prompt,
                    Decision.Take(prompt.Affordances[0].Id, targets.Legal, []), events);
                continue;
            }
            if (prompt.PublicKind == PublicDecisionKind.CardLook)
            {
                looks++;
                Assert.Equal(inspectFirst ? 0 : world.Players, searches);
                Assert.False(world.Cards[Assert.Single(prompt.ContextCardIds)].FaceUp);
                Sequence.Answer(world, world.Facts, runner, prompt,
                    Decision.Take(Assert.Single(prompt.Affordances).Id), events);
            }
            else
            {
                Assert.Equal(DeckType.SideSchemesArea, patrol.Area.Type);
                Assert.Equal(0, patrol.Tokens["k_threat"]);
                Assert.True(prompt.Affordances.Any(item => world.Cards[item.AnchorId].FaceId == "01182"),
                    JsonSerializer.Serialize(prompt));
                var choice = prompt.Affordances.First(item => world.Cards[item.AnchorId].FaceId == "01182");
                if (reenter)
                {
                    // Synthetic intervening movement probes the captured incarnation.
                    var area = patrol.Area;
                    int incarnation = patrol.Incarnation;
                    World.MoveToTop(patrol, world.AreaOf(DeckType.EncounterDiscardPile));
                    World.MoveToTop(patrol, area);
                    patrol.PlaceTokens("k_threat", 6);
                    Assert.NotEqual(incarnation, patrol.Incarnation);
                }
                searches++;
                Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(choice.Id), events);
            }
        }
        Assert.Equal(1, looks);
        Assert.Equal(world.Players, searches);
        Assert.True(world.Random.Generator.WordsConsumed > rngBefore);
        Assert.All(world.PlayerOrder, seat => Assert.Single(
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(seat)).Cards));
        Sequence.Finish(world, world.Facts, runner, events);
        return events;
    }

    private static (World, AbilityRunner, Card, Card, Card, Card) Board(int players)
    {
        var world = ThorActualFixture.Board("06001a", players);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 8);
        var patrol = world.CreateCard("04154", world.AreaOf(DeckType.SideSchemesArea));
        patrol.PlaceTokens("k_threat", 2);
        var other = world.CreateCard("04154", patrol.Area);
        other.PlaceTokens("k_threat", 7);
        for (int copy = 0; copy < 4; copy++) world.CreateCard("01182", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01107", world.AreaOf(DeckType.EncounterDeck));
        return (world, runner, world.CreateCard("09003", world.Seats[0].Hand),
            world.CreateCard("01088", world.Seats[0].Hand), patrol, other);
    }

    private static string Observe(World world) => JsonSerializer.Serialize(new
    {
        Digest = world.Digest().Canonical(), Random = world.Random.Generator.GetState(),
        world.Random.Generator.WordsConsumed, world.Agenda.Current,
        Outstanding = world.Agenda.Outstanding.ToArray(),
    });
}

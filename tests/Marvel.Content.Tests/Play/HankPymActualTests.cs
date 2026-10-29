using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual 12011 on prepared boards; this does not admit the Ant-Man pack.
public sealed class HankPymActualTests : CoreCardTestBase
{
    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 4)]
    [InlineData(3, 4)]
    [Rule("rr:cost.4.1")]
    public void OrdinaryPlayUsesExcessResourcesAndCapsCounters(int sources, long expected)
    {
        // Excess resources "were not paid for that cost"; printed cost is zero.
        var runner = AuthoredCards.Runner();
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var seat = world.Seats[0];
        var hank = world.CreateCard("12011", seat.Hand);
        string[] faces = ["01088", "01089", "01090"];
        var payments = faces.Take(sources).Select(face => world.CreateCard(face, seat.Hand)).ToArray();
        CardPlay.Play(world, world.Facts, runner, seat, hank, payments.Select(card => card.ObjectId).ToArray(), []);
        var interrupt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.True(interrupt.Cancellable);
        Assert.Equal(DeckType.RevealingArea, hank.Area.Type);
        var offer = Assert.Single(interrupt.Affordances, item => item.AnchorId == hank.ObjectId);
        Sequence.Answer(world, world.Facts, runner, interrupt, Decision.Take(offer.Id), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(expected, hank.Tokens.GetValueOrDefault("c_pym"));
        Assert.Equal(expected, DamagePlacement.Health(world, world.Facts, hank));
        Assert.Equal(DeckType.AlliesArea, hank.Area.Type);
        Assert.All(payments, card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:ability.11")]
    public void DecliningOverpaymentInterruptDefeatsHankWithoutRefund()
    {
        // "Unless prefaced by the word 'Forced', all interrupt and response abilities are optional."
        var runner = AuthoredCards.Runner();
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var seat = world.Seats[0];
        var hank = world.CreateCard("12011", seat.Hand);
        var payment = world.CreateCard("01088", seat.Hand);
        CardPlay.Play(world, world.Facts, runner, seat, hank, [payment.ObjectId], []);
        var interrupt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Contains(interrupt.Affordances, item => item.AnchorId == hank.ObjectId);
        Sequence.Answer(world, world.Facts, runner, interrupt, Decision.Decline, []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.DiscardPile, hank.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Equal(0, hank.Tokens.GetValueOrDefault("c_pym"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:ignore.1")]
    public void ZeroOrFreeEntryHasNoCounterOfferAndTeamTrainingKeepsHankAlive(bool free)
    {
        // Ignoring a cost means "no resources are considered paid".
        var runner = AuthoredCards.Runner();
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var seat = world.Seats[0];
        var training = world.CreateCard("04016", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        Reveal.EnterPlay(world, world.Facts, training, [], abilities: runner);
        var hank = world.CreateCard("12011", seat.Hand);
        if (free) CardPlay.PlayIgnoringResourceCost(world, world.Facts, runner, seat, hank, []);
        else CardPlay.Play(world, world.Facts, runner, seat, hank, [], []);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(DeckType.AlliesArea, hank.Area.Type);
        Assert.Equal(0, hank.Tokens.GetValueOrDefault("c_pym"));
        Assert.Equal(1, DamagePlacement.Health(world, world.Facts, hank));
    }

    [Fact]
    [Rule("rr:play-put-into-play.3")]
    [Rule("rr:cost.4.1")]
    public void MakeTheCallPaysForDifferentOwnersHankAndUsesThatPaymentsExcess()
    {
        // Putting into play is not playing; excess resources were not paid for the cost.
        Card? source = null, hank = null, eventResource = null, allyResource = null;
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard("01071", board.Seats[0].Hand);
            hank = board.CreateCard("12011", board.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1));
            eventResource = board.CreateCard("01003", board.Seats[0].Hand);
            allyResource = board.CreateCard("01088", board.Seats[0].Hand);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: AuthoredCards.Runner());
        var play = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == source!.ObjectId);
        int[] eventPayment = [eventResource!.ObjectId];
        game.Resolve(new Decision(play.Id, [], eventPayment,
            Allocations: ResourcePayment.Allocate(Assert.Single(play.Costs!), eventPayment)));
        var choice = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == hank!.ObjectId);
        int[] allyPayment = [allyResource!.ObjectId];
        game.Resolve(new Decision(choice.Id, [], allyPayment,
            Allocations: ResourcePayment.Allocate(Assert.Single(choice.CostOptions), allyPayment)));
        var offer = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == hank!.ObjectId);
        Assert.Equal(DeckType.DiscardPile, hank!.Area.Type);
        Assert.Equal(1, hank.Owner);
        game.Resolve(Decision.Take(offer.Id));
        Assert.Equal(DeckType.AlliesArea, hank.Area.Type);
        Assert.Equal(PlayArea.Of(0), hank.Area.PlayArea);
        Assert.Equal(2, hank.Tokens.GetValueOrDefault("c_pym"));
        Assert.Equal(2, DamagePlacement.Health(world, world.Facts, hank));
        Assert.Equal(DeckType.DiscardPile, allyResource.Area.Type);
    }

    [Fact]
    [Rule("rr:ally-limit")]
    [Rule("rr:initiating-abilities.1")]
    public void NonFirstPlayerChoosesFourthAllyBeforeEntryResponse()
    {
        // Interrupt may initiate before its condition; the excess ally is discarded before entry responses.
        var root = JsonNode.Parse(AuthoredCards.Text)!;
        root["cards"]!.AsArray().Add(JsonNode.Parse("""
            {"card":"01092","abilities":[{"trigger":{"event":"WhenCardEntersPlay","timing":"Response","subject":"game"},
              "effect":{"placeCounters":{"card":"you","counter":"response","count":1}}}]}
            """));
        // Replace the observer's existing row so the fixture keeps duplicate rejection intact.
        var cards = root["cards"]!.AsArray();
        var previous = cards.First(row => (string?)row!["card"] == "01092");
        cards.Remove(previous);
        var runner = new AbilityRunner(AbilityCatalog.Parse(root.ToJsonString()));
        var world = ThorActualFixture.Board("06001a", 2);
        world.Abilities = runner;
        var seat = world.Seats[1];
        var area = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1);
        var old = world.CreateCard("01067", area);
        world.CreateCard("01068", area);
        world.CreateCard("01041", area);
        var observer = world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        var hank = world.CreateCard("12011", seat.Hand);
        var payment = world.CreateCard("01088", seat.Hand);
        CardPlay.Play(world, world.Facts, runner, seat, hank, [payment.ObjectId], []);
        var interrupt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(1, interrupt.Player);
        var offer = Assert.Single(interrupt.Affordances, item => item.AnchorId == hank.ObjectId);
        Sequence.Answer(world, world.Facts, runner, interrupt, Decision.Take(offer.Id), []);
        var limit = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(Steps.ChooseAllyForLimit, limit.Trigger);
        Assert.Equal(1, limit.Player);
        Assert.Equal(2, DamagePlacement.Health(world, world.Facts, hank));
        Assert.Contains(limit.Affordances, item => item.AnchorId == hank.ObjectId);
        Assert.Contains(limit.Affordances, item => item.AnchorId == old.ObjectId);
        Sequence.Answer(world, world.Facts, runner, limit, Decision.Take(old.ObjectId), []);
        var response = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(DeckType.DiscardPile, old.Area.Type);
        Assert.Contains(response.Affordances, item => item.AnchorId == observer.ObjectId);
        Sequence.Answer(world, world.Facts, runner, response, Decision.Decline, []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.AlliesArea, hank.Area.Type);
    }

    [Fact]
    public void FourthAllyWithZeroHealthRemainsExplicitlyUnsupported()
    {
        // MARVEL-885 tracks the unresolved simultaneous framework obligations.
        var runner = AuthoredCards.Runner();
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var area = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
        world.CreateCard("01067", area);
        world.CreateCard("01068", area);
        world.CreateCard("01041", area);
        var hank = world.CreateCard("12011", world.Seats[0].Hand);
        var error = Assert.Throws<RulesNotImplementedException>(() =>
        {
            CardPlay.Play(world, world.Facts, runner, world.Seats[0], hank, [], []);
            Sequence.Work(world, world.Facts, runner, []);
        });
        Assert.Contains("zero-health defeat and ally-limit", error.Message);
    }
}

using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// The synthetic response pauses at entry without admitting Valkyrie's printed text.
public sealed class EntryPaymentProvenanceTests
{
    private const string Observer = """
        {"card":"06012","abilities":[{
          "trigger":{"event":"WhenCardEntersPlay","timing":"Response","subject":"this"},
          "effect":{"placeCounters":{"card":"this","counter":"test","count":1}}
        }]}
        """;

    [Fact]
    [Rule("rr:cost.4.1")]
    [Rule("rr:leaves-play.1")]
    public void PlayedAllyRetainsGeneratedExcessForItsExactEntryOnly()
    {
        // Excess resources "were not paid for that cost"; a returning card is "a new copy".
        var runner = new AbilityRunner(AbilityCatalog.Parse($$$"""{"cards":[{{{Observer}}}]}"""));
        var world = ThorActualFixture.Board("06001a", 2);
        world.Abilities = runner;
        var seat = world.Seats[0];
        var ally = world.CreateCard("06012", seat.Hand);
        var other = world.CreateCard("06012", world.Seats[1].Hand);
        var first = world.CreateCard("06022", seat.Hand);
        var second = world.CreateCard("01088", seat.Hand);
        CardPlay.Play(world, world.Facts, runner, seat, ally, [first.ObjectId, second.ObjectId], []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        var occurrence = Assert.IsType<Occurrence>(world.Agenda.Occurrence);
        var receipt = Assert.IsType<EntryPayment>(occurrence.EntryPayment);
        Assert.Equal(4, receipt.Generated.Length);
        Assert.Equal(3, receipt.Paid.Length);
        Assert.Equal(1, receipt.Excess);
        Assert.True(receipt.MatchesEntered(ally, 0));
        Assert.False(receipt.MatchesEntered(other, 0));
        Assert.False(receipt.MatchesEntered(ally, 1));
        Assert.Equal(receipt.Paid, Assert.IsType<PlayedCardPayment>(occurrence.CardPlayPayment).Paid);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, []);
        Sequence.Finish(world, world.Facts, runner, []);
        Discard.Card(world, ally, "test", []);
        CardPlay.PutAllyIntoPlay(world, world.Facts, runner, ally, 0, "test", []);
        prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Null(world.Agenda.Occurrence!.EntryPayment);
        Assert.Null(world.Agenda.Occurrence.CardPlayPayment);
        Assert.False(receipt.MatchesEntered(ally, 0));
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, []);
        Sequence.Finish(world, world.Facts, runner, []);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:cost.4.1")]
    public void NestedSpendResponseCannotReplaceTheEntryCost()
    {
        // Costs are paid before an ability resolves; excess was not paid for the cost.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"21019","abilities":[{
                "trigger":{"event":"WhenCardEntersPlay","timing":"Response","subject":"this"},
                "effect":{"placeCounters":{"card":"this","counter":"test","count":1}}}]},
              {"card":"12006","abilities":[{
                "trigger":{"event":"WhenResourceCardSpent","timing":"Response","subject":"this","player":"you"},
                "cost":{"spendAny":1},"effect":{"placeCounters":{"card":"you","counter":"nested","count":1}}}]}
            ]}
            """));
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var seat = world.Seats[0];
        var ally = world.CreateCard("21019", seat.Hand);
        var spent = world.CreateCard("12006", seat.Hand);
        var nested = world.CreateCard("01003", seat.Hand);
        CardPlay.Play(world, world.Facts, runner, seat, ally, [spent.ObjectId], []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        var response = Assert.Single(prompt.Affordances, item => item.AnchorId == spent.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, PaidDecision.Take(response, [nested.ObjectId]), []);
        prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Contains(prompt.Affordances, item => item.AnchorId == ally.ObjectId);
        var occurrence = Assert.IsType<Occurrence>(world.Agenda.Occurrence);
        var entry = Assert.IsType<EntryPayment>(occurrence.EntryPayment);
        Assert.Equal("G", entry.Generated);
        Assert.Equal("G", entry.Paid);
        Assert.Equal(0, entry.Excess);
        Assert.True(entry.MatchesEntered(ally, 0));
        Assert.Equal(1, seat.IdentityCard.Tokens.GetValueOrDefault("c_nested"));
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, []);
        Sequence.Finish(world, world.Facts, runner, []);
    }

    [Fact]
    public void PaymentCannotBeReboundToAnotherCardOrPayer()
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("{\"cards\":[]}"));
        var world = ThorActualFixture.Board("06001a", 2);
        world.Abilities = runner;
        var seat = world.Seats[0];
        var target = world.CreateCard("06012", seat.Hand);
        var other = world.CreateCard("06012", seat.Hand);
        var resource = world.CreateCard("01003", seat.Hand);
        var payment = CardPlay.Spend(world, world.Facts, runner, [seat.Hand], [resource.ObjectId],
            1, "", -1, 0, [], payingFor: target);
        Assert.Throws<InvalidOperationException>(() => EntryPayment.Capture(other, 0, payment));
        Assert.Throws<InvalidOperationException>(() => EntryPayment.Capture(target, 1, payment));
        var receipt = EntryPayment.Capture(target, 0, payment);
        Assert.True(receipt.MatchesPending(target, 0));
        Assert.False(receipt.MatchesPending(other, 0));
    }

    [Fact]
    [Rule("rr:play-put-into-play.3")]
    [Rule("rr:cost.4.1")]
    public void MakeTheCallCarriesChosenAllyPriceRatherThanEventPrice()
    {
        // Putting a card into play is not playing it; excess was not paid for the cost.
        var root = JsonNode.Parse(AuthoredCards.Text)!;
        var cards = root["cards"]!.AsArray();
        var previous = cards.SingleOrDefault(row => (string?)row!["card"] == "06012");
        if (previous is not null) cards.Remove(previous);
        cards.Add(JsonNode.Parse(Observer));
        var runner = new AbilityRunner(AbilityCatalog.Parse(root.ToJsonString()));
        Card? source = null;
        Card? ally = null;
        Card? eventResource = null;
        Card? first = null;
        Card? second = null;
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard("01071", board.Seats[0].Hand);
            ally = board.CreateCard("06012", board.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1));
            eventResource = board.CreateCard("01003", board.Seats[0].Hand);
            first = board.CreateCard("06022", board.Seats[0].Hand);
            second = board.CreateCard("01088", board.Seats[0].Hand);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var play = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == source!.ObjectId);
        int[] eventPayment = [eventResource!.ObjectId];
        game.Resolve(new Decision(play.Id, [], eventPayment,
            Allocations: ResourcePayment.Allocate(Assert.Single(play.Costs!), eventPayment)));
        var choice = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == ally!.ObjectId);
        int[] allyPayment = [first!.ObjectId, second!.ObjectId];
        game.Resolve(new Decision(choice.Id, [], allyPayment,
            Allocations: ResourcePayment.Allocate(Assert.Single(choice.CostOptions), allyPayment)));
        Assert.Contains(game.Pending!.Affordances, item => item.AnchorId == ally!.ObjectId);
        var occurrence = Assert.IsType<Occurrence>(world.Agenda.Occurrence);
        var receipt = Assert.IsType<EntryPayment>(occurrence.EntryPayment);
        Assert.Equal("YYYY", receipt.Generated);
        Assert.Equal("YYY", receipt.Paid);
        Assert.Equal(1, receipt.Excess);
        Assert.True(receipt.MatchesEntered(ally!, 0));
        Assert.Equal(1, ally!.Owner);
        Assert.Null(occurrence.CardPlayPayment);
        game.Resolve(Decision.Decline);
        Assert.Equal(DeckType.DiscardPile, eventResource!.Area.Type);
    }

    [Theory]
    [InlineData(true, "GGYY", "GY", 2)]
    [InlineData(false, "GYY", "YY", 1)]
    [Rule("rr:cost.4.1")]
    public void MakeTheCallPreservesChosenConditionalResourceAllocation(
        bool powerPays, string generated, string paid, int excess)
    {
        // Resources beyond the cost "were not paid for that cost".
        var runner = AuthoredCards.Runner();
        var world = ThorActualFixture.Board("06001a", 2);
        world.Abilities = runner;
        var seat = world.Seats[0];
        var source = world.CreateCard("01071", seat.Hand);
        var ally = world.CreateCard("01067", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1));
        var power = world.CreateCard("01072", seat.Hand);
        var physical = world.CreateCard("06022", seat.Hand);
        runner.ExecuteAction(world, new PlayerAction(new PendingAbility(source.ObjectId, AbilityType.Action, 0), [], []));
        var step = Assert.Single(world.Agenda.Outstanding);
        var prompt = Assert.IsType<Prompt>(runner.Choosing(world, step));
        var offer = Assert.Single(prompt.Affordances, item => item.AnchorId == ally.ObjectId);
        ResourceAllocation[] allocation = powerPays
            ? [new(power.ObjectId, 0, "G"), new(physical.ObjectId, 0, "Y")]
            : [new(physical.ObjectId, 0, "YY")];
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        ResourceAllocation[] invalid = [new(physical.ObjectId, 0, "GG")];
        Assert.Throws<ArgumentException>(() => runner.Chose(world, step,
            Decision.Take(ally.ObjectId, [], [power.ObjectId, physical.ObjectId],
                new Dictionary<string, long>(), invalid)));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.True(ResourcePayment.Allows(Assert.Single(offer.CostOptions), [power.ObjectId, physical.ObjectId], null, allocation));
        runner.Chose(world, step, Decision.Take(ally.ObjectId, [], [power.ObjectId, physical.ObjectId],
            new Dictionary<string, long>(), allocation));
        var entryStep = Assert.Single(world.Agenda.Outstanding, item => item.What == Steps.CardEntersPlay);
        var occurrence = Assert.IsType<Occurrence>(entryStep.ScheduledOccurrence);
        var entry = Assert.IsType<EntryPayment>(occurrence.EntryPayment);
        Assert.Equal(generated, entry.Generated);
        Assert.Equal(paid, entry.Paid);
        Assert.Equal(excess, entry.Excess);
        Assert.True(entry.MatchesEntered(ally, 0));
        Assert.Null(occurrence.CardPlayPayment);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void MakeTheCallRetainsAllyPriceAcrossGeneratorAndSpendWindows()
    {
        // Step 5 pays the costs before the effect begins.
        var root = JsonNode.Parse(AuthoredCards.Text)!;
        var cards = root["cards"]!.AsArray();
        foreach (string id in new[] { "06012", "01030", "01092", "12006" })
        {
            var previous = cards.SingleOrDefault(row => (string?)row!["card"] == id);
            if (previous is not null) cards.Remove(previous);
        }
        foreach (var row in JsonNode.Parse($$$$$"""
          [{{{{{Observer}}}}},
           {"card":"01030","abilities":[{
             "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},
             "cost":{"dealDamage":{"cards":"this","amount":2}},"effect":{"generate":"YY"}}]},
           {"card":"01092","abilities":[{
             "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
             "effect":{"draw":{"player":"you","count":1}}}]},
           {"card":"12006","abilities":[{
             "trigger":{"event":"WhenResourceCardSpent","timing":"Response","subject":"this","player":"you"},
             "effect":{"placeCounters":{"card":"you","counter":"spent_seen","count":1}}}]}]
          """)!.AsArray()) cards.Add(row!.DeepClone());
        var runner = new AbilityRunner(AbilityCatalog.Parse(root.ToJsonString()));
        Card? source = null;
        Card? ally = null;
        Card? generator = null;
        Card? spent = null;
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard("01071", board.Seats[0].Hand);
            ally = board.CreateCard("06012", board.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1));
            generator = board.CreateCard("01030", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            generator.TakeDamage(2);
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            spent = board.CreateCard("12006", board.Seats[0].Hand);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var play = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(play.Id));
        var choice = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == ally!.ObjectId);
        int[] payment = [generator!.ObjectId, spent!.ObjectId];
        game.Resolve(new Decision(choice.Id, [], payment,
            Allocations: ResourcePayment.Allocate(Assert.Single(choice.CostOptions), payment)));
        Assert.Equal(DeckType.DiscardPile, ally!.Area.Type);
        game.Resolve(Decision.Decline);
        var response = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == spent.ObjectId);
        game.Resolve(Decision.Take(response.Id));
        Assert.Contains(game.Pending!.Affordances, item => item.AnchorId == ally.ObjectId);
        var entry = Assert.IsType<EntryPayment>(world.Agenda.Occurrence!.EntryPayment);
        Assert.Equal(3, entry.Generated.Length);
        Assert.Equal(3, entry.Paid.Length);
        Assert.Equal(0, entry.Excess);
        Assert.True(entry.MatchesEntered(ally, 0));
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_spent_seen"));
        game.Resolve(Decision.Decline);
    }
}

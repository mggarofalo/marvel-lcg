using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic ability rows isolate original card-price receipts; no pack admission is implied.
public sealed class AllyEntryPaymentTests
{
    private const string Rows = """
        {"cards":[
          {"card":"06012","abilities":[{
            "trigger":{"event":"WhenCardEntersPlay","timing":"Response","subject":"this"},
            "effect":{"if":{"test":{"paidWithResource":"Y"},
              "then":{"placeCounters":{"card":"this","counter":"test","count":3}},
              "else":{"placeCounters":{"card":"this","counter":"test","count":2}}}}
          }]},
          {"card":"06016","abilities":[{"trigger":{"timing":"Constant","subject":"this"},"effect":{"doubleResourceFor":"Aggression"}}]},
          {"card":"01030","abilities":[{
            "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},
            "cost":{"dealDamage":{"cards":"this","amount":2}},"effect":{"generate":"YY"}
          }]},
          {"card":"01092","abilities":[{
            "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
            "effect":{"draw":{"player":"you","count":1}}
          }]}
        ]}
        """;

    [Theory]
    [InlineData("06022", null, 3)]
    [InlineData("06024", null, 2)]
    [InlineData("06016", "YY", 3)]
    [InlineData("06016", "RR", 2)]
    public void OriginalCardPriceSurvivesThePublicPlayAndResponse(string face, string? declared, int expected)
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse(Rows));
        Card? ally = null;
        Card? first = null;
        Card? second = null;
        var (game, world) = Playing(board =>
        {
            ally = board.CreateCard("06012", board.Seats[0].Hand);
            first = board.CreateCard(face, board.Seats[0].Hand);
            second = board.CreateCard("01003", board.Seats[0].Hand);
        }, hero: true, abilities: runner);
        var offer = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == ally!.ObjectId);
        ResourceAllocation[] allocation =
            [new(first!.ObjectId, 0, declared ?? (face == "06022" ? "YY" : "RR")),
             new(second!.ObjectId, 0, "R")];
        game.Resolve(new Decision(offer.Id, [world.Seats[0].IdentityCard.ObjectId],
            [first!.ObjectId, second!.ObjectId], Allocations: allocation));
        var response = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == ally!.ObjectId);
        game.Resolve(Decision.Take(response.Id));
        Assert.Equal(expected, ally!.Tokens["c_test"]);
        Assert.True(game.IsRootPrompt);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void InvalidDeclaredPaymentLeavesEntryStateUntouchedAndAllowsRetry()
    {
        // "abort this process without paying any costs".
        var runner = new AbilityRunner(AbilityCatalog.Parse(Rows));
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var seat = world.Seats[0];
        var ally = world.CreateCard("06012", seat.Hand);
        var energy = world.CreateCard("06022", seat.Hand);
        var physical = world.CreateCard("01003", seat.Hand);
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();
        var effects = world.Effects.Registered.ToArray();
        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(3, CardPayment.CostOf(world, world.Facts, seat, ally).Amount);
            Assert.Equal("3", CardPayment.Price(world, world.Facts, seat, ally)!.Cost);
        }
        var bad = new Decision(ally.ObjectId, [], [energy.ObjectId, physical.ObjectId],
            Allocations: [new(energy.ObjectId, 0, "RR"), new(physical.ObjectId, 0, "R")]);
        Assert.Throws<RulesNotImplementedException>(() =>
            CardPlay.PlayAllocated(world, world.Facts, runner, seat, ally, bad, []));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Equal(effects, world.Effects.Registered);
        CardPlay.Play(world, world.Facts, runner, seat, ally, [energy.ObjectId, physical.ObjectId], []);
        Respond(world, runner, ally);
        Assert.Equal(3, ally.Tokens["c_test"]);
    }

    [Fact]
    public void PutIntoPlayReentryCannotReuseEarlierPriceOrAnotherPhysicalCopy()
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse(Rows));
        var world = ThorActualFixture.Board("06001a", 2);
        world.Abilities = runner;
        var seat = world.Seats[1];
        var ally = world.CreateCard("06012", seat.Hand);
        var energy = world.CreateCard("06022", seat.Hand);
        var physical = world.CreateCard("01003", seat.Hand);
        CardPlay.Play(world, world.Facts, runner, seat, ally, [energy.ObjectId, physical.ObjectId], []);
        Respond(world, runner, ally);
        Assert.Equal(3, ally.Tokens["c_test"]);
        Discard.Card(world, ally, "fixture", []);
        CardPlay.PutAllyIntoPlay(world, world.Facts, runner, ally, 1, "fixture", []);
        Respond(world, runner, ally);
        Assert.Equal(2, ally.Tokens["c_test"]);
        Discard.Card(world, ally, "fixture", []);
        var other = world.CreateCard("06012", seat.Hand);
        CardPlay.PutAllyIntoPlay(world, world.Facts, runner, other, 1, "fixture", []);
        Respond(world, runner, other);
        Assert.Equal(2, other.Tokens["c_test"]);
    }

    [Fact]
    public void SuspendedGeneratorAndAllyLimitChoiceRetainTheOriginalPrice()
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse(Rows));
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var seat = world.Seats[0];
        var ally = world.CreateCard("06012", seat.Hand);
        var generator = world.CreateCard("01030", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        generator.TakeDamage(2);
        world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("01041", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var discard = world.CreateCard("01050", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("01051", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var physical = world.CreateCard("01003", seat.Hand);
        CardPlay.Play(world, world.Facts, runner, seat, ally, [generator.ObjectId, physical.ObjectId], []);
        var interrupt = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.Equal(DeckType.RevealingArea, ally.Area.Type);
        Sequence.Answer(world, world.Facts, runner, interrupt, Decision.Decline, []);
        var limit = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.Equal(Question.Element, limit.Asking);
        Sequence.Answer(world, world.Facts, runner, limit, Decision.Take(discard.ObjectId), []);
        var response = Sequence.Work(world, world.Facts, runner, [])!;
        var entry = Assert.IsType<EntryPayment>(world.Agenda.Occurrence!.Entry.EntryPayment);
        Assert.Equal(3, entry.Generated.Length);
        Assert.Equal(3, entry.Paid.Length);
        Assert.Equal(0, entry.Excess);
        Assert.True(entry.MatchesEntered(ally, 0));
        var offer = Assert.Single(response.Affordances, a => a.AnchorId == ally.ObjectId);
        Sequence.Answer(world, world.Facts, runner, response, Decision.Take(offer.Id), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(3, ally.Tokens["c_test"]);
        Assert.Equal(DeckType.DiscardPile, generator.Area.Type);
    }

    [Theory]
    [InlineData("06024", "01088", 2)]
    [InlineData("06022", "01003", 3)]
    public void ResponseResourceCostDoesNotReplaceTheOriginalCardPrice(
        string priceFace, string responseFace, int expected)
    {
        var rows = Rows.Replace(
            "\"effect\":{\"if\"", "\"cost\":{\"spendAny\":1},\"effect\":{\"if\"");
        var runner = new AbilityRunner(AbilityCatalog.Parse(rows));
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var seat = world.Seats[0];
        var ally = world.CreateCard("06012", seat.Hand);
        var first = world.CreateCard(priceFace, seat.Hand);
        var second = world.CreateCard("01003", seat.Hand);
        var responsePayment = world.CreateCard(responseFace, seat.Hand);
        CardPlay.Play(world, world.Facts, runner, seat, ally, [first.ObjectId, second.ObjectId], []);
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        var offer = Assert.Single(prompt.Affordances, a => a.AnchorId == ally.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt,
            Decision.Take(offer.Id, [], [responsePayment.ObjectId]), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(expected, ally.Tokens["c_test"]);
        Assert.Equal(DeckType.DiscardPile, responsePayment.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:wild-resource.1")]
    public void UndeclaredWildDoesNotBecomeEnergyAndForgedWildAllocationIsAtomic()
    {
        // A player "may specify which resource type" a generated wild is used as.
        var runner = new AbilityRunner(AbilityCatalog.Parse(Rows));
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var seat = world.Seats[0];
        var ally = world.CreateCard("06012", seat.Hand);
        var wild = world.CreateCard("06016", seat.Hand);
        var physical = world.CreateCard("01003", seat.Hand);
        string before = world.Digest().Canonical();
        var pending = world.Agenda.Outstanding.ToArray();
        var random = world.Random.Generator.GetState();
        var bad = new Decision(ally.ObjectId, [], [wild.ObjectId, physical.ObjectId],
            Allocations: [new(wild.ObjectId, 0, "YYY")]);
        Assert.Throws<RulesNotImplementedException>(() =>
            CardPlay.PlayAllocated(world, world.Facts, runner, seat, ally, bad, []));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        CardPlay.Play(world, world.Facts, runner, seat, ally, [wild.ObjectId, physical.ObjectId], []);
        Respond(world, runner, ally);
        Assert.Equal(2, ally.Tokens["c_test"]);
    }

    [Fact]
    public void ReceiptRejectsAnotherCopyPlayerAndLaterIncarnation()
    {
        var world = ThorActualFixture.Board("06001a", 2);
        var area = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1);
        var ally = world.CreateCard("06012", area);
        var other = world.CreateCard("01041", area);
        var receipt = new PlayedCardPayment(ally.ObjectId, ally.Incarnation, 1, "YYR");
        Assert.True(receipt.Includes(ally, 1, Resources.Energy));
        Assert.False(receipt.Includes(ally, 0, Resources.Energy));
        Assert.False(receipt.Includes(other, 1, Resources.Energy));
        Assert.False(receipt.Includes(ally, 1, Resources.Mental));
        Discard.Card(world, ally, "fixture", []);
        World.MoveToTop(ally, area);
        Assert.False(receipt.Includes(ally, 1, Resources.Energy));
    }

    private static void Respond(World world, AbilityRunner runner, Card ally)
    {
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        var offer = Assert.Single(prompt.Affordances, a => a.AnchorId == ally.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(offer.Id), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Empty(world.Agenda.Outstanding);
    }
}


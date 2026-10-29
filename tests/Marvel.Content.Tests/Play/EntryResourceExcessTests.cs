using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic entry response isolates the numeric receipt; no Hank row is admitted.
public sealed class EntryResourceExcessTests
{
    private static readonly string[] ResourceFaces = ["01088", "01089", "01090", "06002", "06003"];

    private const string Rows = """
        {"cards":[{"card":"06012","abilities":[{
          "trigger":{"event":"WhenCardEntersPlay","timing":"Response","subject":"this"},
          "effect":{"placeCounters":{"card":"this","counter":"excess",
            "count":{"min":[4,{"entryResourceExcess":"this"}]}}}
        }]}]}
        """;

    [Theory]
    [InlineData(2, 1)]
    [InlineData(3, 3)]
    [InlineData(5, 4)]
    [Rule("rr:cost.4.1")]
    public void PlayedEntryReadsGeneratedBeyondPaidAndComposesWithAnExistingCap(int sources, long expected)
    {
        // Excess resources "were not paid for that cost"; the cap here is a synthetic card choice.
        var runner = new AbilityRunner(AbilityCatalog.Parse(Rows));
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var seat = world.Seats[0];
        var ally = world.CreateCard("06012", seat.Hand);
        var paying = ResourceFaces.Take(sources).Select(face => world.CreateCard(face, seat.Hand).ObjectId).ToArray();
        CardPlay.Play(world, world.Facts, runner, seat, ally, paying, []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        var offer = Assert.Single(prompt.Affordances, item => item.AnchorId == ally.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(offer.Id), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(expected, ally.Tokens.GetValueOrDefault("c_excess"));
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:ignore.1")]
    public void UnpaidEntryDoesNotInventExcess(bool freePlay)
    {
        // Ignoring a resource cost means "no resources are considered paid".
        var runner = new AbilityRunner(AbilityCatalog.Parse(Rows));
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var ally = world.CreateCard("06012", world.Seats[0].Hand);
        if (freePlay) CardPlay.PlayIgnoringResourceCost(world, world.Facts, runner, world.Seats[0], ally, []);
        else CardPlay.PutAllyIntoPlay(world, world.Facts, runner, ally, 0, "fixture", []);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(0, ally.Tokens.GetValueOrDefault("c_excess"));
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:leaves-play.1")]
    public void QueryRequiresExactSourcePayerAndCurrentIncarnation()
    {
        // A returning card is "a new copy of the card"; its earlier cost does not belong to that copy.
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = new AbilityRunner(AbilityCatalog.Parse("{\"cards\":[]}"));
        world.Abilities = runner;
        var seat = world.Seats[1];
        var ally = world.CreateCard("06012", seat.Hand);
        var other = world.CreateCard("06012", seat.Hand);
        var resource = world.CreateCard("01090", seat.Hand);
        var paid = CardPlay.Spend(world, world.Facts, runner, [seat.Hand], [resource.ObjectId],
            1, "", -1, 1, [], payingFor: ally);
        var receipt = EntryPayment.Capture(ally, 1, paid);
        var occurrence = new Occurrence(1, []) { Entry = new OccurrenceEntry(EntryPayment: receipt) };
        var context = Context(world, ally, occurrence, 1);
        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long consumed = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();
        Assert.Equal(1, AbilityEntryResourceQuery.Excess(context));
        Assert.Equal(0, AbilityEntryResourceQuery.Excess(Context(world, ally, occurrence, 0)));
        Assert.Equal(0, AbilityEntryResourceQuery.Excess(Context(world, other, occurrence, 1)));
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(consumed, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Outstanding);
        var area = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1);
        World.MoveToTop(ally, area);
        Assert.Equal(0, AbilityEntryResourceQuery.Excess(context));
        Assert.Equal(1, AbilityEntryResourceQuery.Excess(Context(world, ally, occurrence, 1)));
        Discard.Card(world, ally, "fixture", []);
        World.MoveToTop(ally, area);
        Assert.Equal(0, AbilityEntryResourceQuery.Excess(Context(world, ally, occurrence, 1)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:target.3.4")]
    public void ZeroCounterSentenceDoesNotSuppressAnIndependentDraw(bool drawFirst)
    {
        // With multiple effects, "at least one of those effects must be able to affect the target".
        var rows = JsonNode.Parse(Rows)!;
        var zero = JsonNode.Parse("""{"placeCounters":{"card":"this","counter":"excess","count":0}}""")!;
        var draw = JsonNode.Parse("""{"draw":{"player":"you","count":1}}""")!;
        rows["cards"]![0]!["abilities"]![0]!["effect"] = new JsonObject
        {
            ["seq"] = drawFirst ? new JsonArray(draw, zero) : new JsonArray(zero, draw),
        };
        var runner = new AbilityRunner(AbilityCatalog.Parse(rows.ToJsonString()));
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var seat = world.Seats[0];
        var ally = world.CreateCard("06012", seat.Hand);
        int before = seat.Hand.Cards.Count;
        CardPlay.PlayIgnoringResourceCost(world, world.Facts, runner, seat, ally, []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        var offer = Assert.Single(prompt.Affordances, item => item.AnchorId == ally.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(offer.Id), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(before, seat.Hand.Cards.Count);
        Assert.Equal(0, ally.Tokens.GetValueOrDefault("c_excess"));
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData("\"you\"")]
    [InlineData("\"game\"")]
    [InlineData("1")]
    public void EntryExpressionRejectsArgumentsThatDoNotNameItsOwnSource(string argument)
    {
        // The fixed source is an engine language choice, not a printed rule.
        string invalid = Rows.Replace("\"entryResourceExcess\":\"this\"",
            "\"entryResourceExcess\":" + argument, StringComparison.Ordinal);
        Assert.Throws<AbilityException>(() => new AbilityRunner(AbilityCatalog.Parse(invalid)));
    }

    private static AbilityExpressionContext Context(World world, Card source, Occurrence occurrence, int player) =>
        new(new AbilityQueryContext(world, source, occurrence, player, source.Incarnation, null, null, null, []),
            ImmutableDictionary<string, long>.Empty, [], string.Empty, -1, false, null);
}

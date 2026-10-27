using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

// Synthetic prerequisite composition, not an authored or admitted Astral Projection row.
public sealed class EncounterInspectionBoostTests
{
    [Theory]
    [InlineData("01104", 0, false)]
    [InlineData("01106", 1, false)]
    [InlineData("01107", 2, false)]
    [InlineData("01121", 0, true)]
    [InlineData("01146", 1, true)]
    [Rule("rr:boost-boost-icon.1")]
    [Rule("rr:look-looked-at")]
    [Rule("rr:look-looked-at.1.1")]
    public void NumericReceiptUsesOnlyThePrivatelyInspectedTop(string face, int boost, bool star)
    {
        // A star "is not itself considered a boost icon". Only the resolving
        // player can look; cards return "in the same order" after looking.
        var (world, runner) = Board(face);
        Assert.Equal(star, world.Facts.HasBoostAbility(face));
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        var deck = world.AreaOf(DeckType.EncounterDeck);
        int[] order = deck.Cards.Select(card => card.ObjectId).ToArray();
        var (events, look) = Begin(world, runner, scheme);
        Assert.Equal(7, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.False(world.Seats[1].IdentityCard!.Ready);
        Assert.Equal(1, look.Player);
        Assert.Equal([order[^1]], look.ContextCardIds);
        string before = Snapshot(world, events);
        for (int repeat = 0; repeat < 3; repeat++)
        {
            var actor = Project(world, look, events, 1);
            Assert.Single(actor.World.Areas.Single(area => area.Id == deck.Id).Cards,
                card => card.Face is not null);
            Assert.Equal(look.ContextCardIds, actor.Prompt!.ContextCardIds);
            Assert.Null(Project(world, look, events, 0).Prompt);
            Assert.All(Project(world, look, events, 0).World.Areas.Single(area => area.Id == deck.Id).Cards,
                card => { Assert.Null(card.Face); Assert.Null(card.Id); });
        }
        Assert.Equal(before, Snapshot(world, events));
        foreach (var invalid in new[] { Decision.Decline, Decision.Take(-1), Decision.Take(order[0]) })
        {
            Assert.Throws<ArgumentException>(() => Sequence.Answer(world, world.Facts, runner, look, invalid, events));
            Assert.Equal(before, Snapshot(world, events));
        }
        Sequence.Answer(world, world.Facts, runner, look, Decision.Take(0), events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(7 - boost, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(order, deck.Cards.Select(card => card.ObjectId));
        Assert.All(deck.Cards, card => Assert.False(card.FaceUp));
        Assert.All(Project(world, null, events, 1).World.Areas.Single(area => area.Id == deck.Id).Cards,
            card => Assert.Null(card.Face));
        Assert.Equal(0L, world.Random.Generator.WordsConsumed);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [Rule("rr:and.1")]
    public void PartialInitialRemovalStillInspectsAndNeverRetargetsADefeatedScheme(int threat)
    {
        // Independent effects "resolve as much of each effect as possible".
        var (world, runner) = Board("01107");
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        var side = world.CreateCard("01107", world.AreaOf(DeckType.SideSchemesArea));
        side.PlaceTokens("k_threat", threat);
        var (events, look) = Begin(world, runner, side);
        Assert.Equal(PublicDecisionKind.CardLook, look.PublicKind);
        Assert.Equal(0, side.Tokens.GetValueOrDefault("k_threat"));
        if (threat > 0) Assert.Equal(DeckType.EncounterDiscardPile, side.Area.Type);
        Sequence.Answer(world, world.Facts, runner, look, Decision.Take(0), events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(10, main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void EmptyEncounterDeckDoesNotRefillAndStillRemovesInitialThreat()
    {
        var (world, runner) = Board(null);
        world.CreateCard("01107", world.AreaOf(DeckType.EncounterDiscardPile));
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        var (events, look) = Begin(world, runner, scheme);
        Assert.Empty(look.ContextCardIds);
        Sequence.Answer(world, world.Facts, runner, look, Decision.Take(0), events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(7, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Empty(world.AreaOf(DeckType.EncounterDeck).Cards);
        Assert.Single(world.AreaOf(DeckType.EncounterDiscardPile).Cards);
        Assert.Equal(0L, world.Random.Generator.WordsConsumed);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void SequentialInspectionCanFollowATargetedEffectWithoutReadingTheHiddenTopAtAdmission()
    {
        var (world, runner) = Board("01107", simultaneous: false);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        var (events, look) = Begin(world, runner, scheme);
        Assert.Equal(7, scheme.Tokens.GetValueOrDefault("k_threat"));
        Sequence.Answer(world, world.Facts, runner, look, Decision.Take(0), events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(5, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void UninspectedNumericBoostDoesNotChangePublicActionOffers()
    {
        var (zero, zeroRunner) = Board("01104");
        var (two, twoRunner) = Board("01107");
        string zeroBefore = Snapshot(zero, []);
        string twoBefore = Snapshot(two, []);
        Assert.Equal(JsonSerializer.Serialize(zeroRunner.Actions(zero, 1)),
            JsonSerializer.Serialize(twoRunner.Actions(two, 1)));
        Assert.Equal(zeroBefore, Snapshot(zero, []));
        Assert.Equal(twoBefore, Snapshot(two, []));
    }

    [Fact]
    public void QueriesAndRejectedAnswersPreserveTheSeededFutureAndCommittedCost()
    {
        var (world, runner) = Board("01107");
        var (control, controlRunner) = Board("01107");
        string before = Snapshot(world, []);
        for (int repeat = 0; repeat < 3; repeat++) Assert.Single(runner.Actions(world, 1));
        Assert.Equal(before, Snapshot(world, []));
        var (events, look) = Begin(world, runner, world.TheCardIn(DeckType.MainSchemesArea)!);
        var (controlEvents, controlLook) = Begin(control, controlRunner, control.TheCardIn(DeckType.MainSchemesArea)!);
        before = Snapshot(world, events);
        Assert.Throws<ArgumentException>(() => Sequence.Answer(world, world.Facts, runner, look, Decision.Take(99), events));
        Project(world, look, events, 1);
        Project(world, look, events, 0);
        Assert.Equal(before, Snapshot(world, events));
        Sequence.Answer(world, world.Facts, runner, look, Decision.Take(0), events);
        Sequence.Finish(world, world.Facts, runner, events);
        Sequence.Answer(control, control.Facts, controlRunner, controlLook, Decision.Take(0), controlEvents);
        Sequence.Finish(control, control.Facts, controlRunner, controlEvents);
        Assert.Equal(Snapshot(control, controlEvents), Snapshot(world, events));
        Assert.Throws<ArgumentException>(() => Sequence.Answer(world, world.Facts, runner, look, Decision.Take(0), events));
        Assert.Equal(Snapshot(control, controlEvents), Snapshot(world, events));
        for (int next = 0; next < 5; next++)
            Assert.Equal(control.Random.Generator.NextUInt32(), world.Random.Generator.NextUInt32());
    }

    private static (World World, AbilityRunner Runner) Board(string? face, bool simultaneous = true)
    {
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 818);
        for (int index = 0; index < 2; index++)
        {
            var seat = world.CreateSeat($"p{index}");
            seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        }
        world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea)).PlaceTokens("k_threat", 10);
        if (face is not null)
        {
            world.CreateCard("01106", world.AreaOf(DeckType.EncounterDeck));
            world.CreateCard(face, world.AreaOf(DeckType.EncounterDeck));
        }
        const string initial = """
            {"removeThreat":{"scheme":"chosen","amount":3}},
            {"inspectEncounterTop":{"count":1,"result":"boostIconsLookedAt"}}
            """;
        string predecessor = simultaneous ? "{\"and\":[" + initial + "]}" : initial;
        var runner = new AbilityRunner(AbilityCatalog.Parse($$$$"""
            {"cards":[{"card":"01001a","abilities":[{
              "trigger":{"timing":"Action","event":"WhenActionTriggered","subject":"this"},
              "cost":{"exhaust":"this"},
              "effect":{"chooseCard":{"from":{"query":"schemes"},"effect":{"seq":[
                {{{{predecessor}}}},
                {"removeThreat":{"scheme":"chosen","amount":{"result":"boostIconsLookedAt"}}}
              ]}}}
            }]}]}
            """));
        world.Abilities = runner;
        return (world, runner);
    }

    private static List<GameEvent> Start(World world, AbilityRunner runner) =>
        runner.ExecuteAction(world, new PlayerAction(Assert.Single(runner.Actions(world, 1)), [], [])).Events.ToList();

    private static (List<GameEvent> Events, Prompt Look) Begin(World world, AbilityRunner runner, Card scheme)
    {
        var events = Start(world, runner);
        var choose = Sequence.Work(world, world.Facts, runner, events)!;
        Sequence.Answer(world, world.Facts, runner, choose, Decision.Take(scheme.ObjectId), events);
        var order = Sequence.Work(world, world.Facts, runner, events)!;
        if (order.Asking != Question.Order) return (events, order);
        Sequence.Answer(world, world.Facts, runner, order, new Decision(order.Affordances[0].Id, [0, 1]), events);
        return (events, Sequence.Work(world, world.Facts, runner, events)!);
    }

    private static VisibleResult Project(World world, Prompt? prompt, List<GameEvent> events, int seat) =>
        WorldProjection.For(world, prompt, events, new RestrictedVisibilityPolicy(seat).Authorize(null, world.Players));

    private static string Snapshot(World world, List<GameEvent> events) => JsonSerializer.Serialize(new
    {
        Digest = world.Digest().Canonical(), Random = world.Random.Generator.GetState(), world.Random.Generator.WordsConsumed,
        world.Agenda.Stage, Pending = world.Agenda.Outstanding.ToArray(), Events = events.Select(EventJson.Write),
    });
}


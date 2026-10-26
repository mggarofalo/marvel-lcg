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

// Independent synthetic contracts; these do not author or admit Astral Projection.
public sealed class EncounterInspectionBoostReviewTests
{
    [Theory]
    [InlineData("", 0)]
    [InlineData("01107", 2)]
    [InlineData("01146,01107", 3)]
    [InlineData("01107,01121,01146,01107", 3)]
    [Rule("rr:boost-boost-icon.1")]
    [Rule("rr:look-looked-at.1.1")]
    public void NumericResultSurvivesAnIndependentInspectionAndUsesOnlyTheRequestedPrefix(string faces, int boost)
    {
        // A star "is not itself considered a boost icon". Looked-at cards return "in the same order".
        var (world, runner) = Board(faces, """
            {"seq":[
              {"removeThreat":{"scheme":{"query":"mainScheme"},"amount":1}},
              {"inspectEncounterTop":{"count":3,"result":"boostIconsLookedAt"}},
              {"inspectEncounterTop":{"count":1,"result":"treacheriesLookedAt"}},
              {"removeThreat":{"scheme":{"query":"mainScheme"},"amount":{"result":"boostIconsLookedAt"}}}
            ]}
            """);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        int[] order = deck.Cards.Select(card => card.ObjectId).ToArray();
        var (events, first) = Start(world, runner);
        Assert.Equal(order.AsEnumerable().Reverse().Take(3), first.ContextCardIds);
        Assert.Equal(19, Threat(world));
        Acknowledge(world, runner, first, events);
        var second = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(PublicDecisionKind.CardLook, second.PublicKind);
        Assert.Equal(order.AsEnumerable().Reverse().Take(1), second.ContextCardIds);
        Acknowledge(world, runner, second, events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(19 - boost, Threat(world));
        Assert.Equal(order, deck.Cards.Select(card => card.ObjectId));
        Assert.All(deck.Cards, card => Assert.False(card.FaceUp));
        Assert.Equal(0L, world.Random.Generator.WordsConsumed);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void ReusingTheNumericResultNameReplacesItsEarlierValue()
    {
        var (world, runner) = Board("01121,01146,01107", """
            {"seq":[
              {"inspectEncounterTop":{"count":3,"result":"boostIconsLookedAt"}},
              {"inspectEncounterTop":{"count":1,"result":"boostIconsLookedAt"}},
              {"removeThreat":{"scheme":{"query":"mainScheme"},"amount":{"result":"boostIconsLookedAt"}}}
            ]}
            """);
        var (events, first) = Start(world, runner);
        Acknowledge(world, runner, first, events);
        var second = Sequence.Work(world, world.Facts, runner, events)!;
        Acknowledge(world, runner, second, events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(18, Threat(world));
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:and.1")]
    public void InspectionCanResolveFirstWithoutApplyingTheOtherIndependentEffectEarly()
    {
        // Individual effects connected by "and" "are not dependent on each other".
        var (world, runner) = Board("01107", """
            {"chooseCard":{"from":{"query":"schemes"},"effect":{"seq":[
              {"and":[
                {"removeThreat":{"scheme":"chosen","amount":3}},
                {"inspectEncounterTop":{"count":1,"result":"boostIconsLookedAt"}}
              ]},
              {"removeThreat":{"scheme":"chosen","amount":{"result":"boostIconsLookedAt"}}}
            ]}}}
            """);
        var (events, choose) = Start(world, runner);
        Sequence.Answer(world, world.Facts, runner, choose,
            Decision.Take(world.TheCardIn(DeckType.MainSchemesArea)!.ObjectId), events);
        var order = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(Question.Order, order.Asking);
        Assert.Equal(0, order.Player);
        Sequence.Answer(world, world.Facts, runner, order, new Decision(order.Affordances[0].Id, [1, 0]), events);
        var look = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(PublicDecisionKind.CardLook, look.PublicKind);
        Assert.Equal(1, look.Player);
        Assert.Equal(20, Threat(world));
        Acknowledge(world, runner, look, events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(15, Threat(world));
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:look-looked-at")]
    public void ObserverQueriesCannotDistinguishHiddenNumericValuesAndDoNotChangeLaterResolution()
    {
        // "Only the player who is resolving the ability can look at those cards."
        const string effect = """
            {"seq":[
              {"inspectEncounterTop":{"count":1,"result":"boostIconsLookedAt"}},
              {"removeThreat":{"scheme":{"query":"mainScheme"},"amount":{"result":"boostIconsLookedAt"}}}
            ]}
            """;
        var (zero, zeroRunner) = Board("01104", effect);
        var (two, twoRunner) = Board("01107", effect);
        var (control, controlRunner) = Board("01107", effect);
        var (zeroEvents, zeroLook) = Start(zero, zeroRunner);
        var (twoEvents, twoLook) = Start(two, twoRunner);
        var (controlEvents, controlLook) = Start(control, controlRunner);
        string before = Snapshot(two, twoEvents);
        for (int repeat = 0; repeat < 3; repeat++)
        {
            Assert.Equal(JsonSerializer.Serialize(Project(zero, zeroLook, zeroEvents, 0)),
                JsonSerializer.Serialize(Project(two, twoLook, twoEvents, 0)));
            Assert.Equal(zeroLook.ContextCardIds, Project(zero, zeroLook, zeroEvents, 1).Prompt!.ContextCardIds);
            Assert.Equal(twoLook.ContextCardIds, Project(two, twoLook, twoEvents, 1).Prompt!.ContextCardIds);
        }
        Assert.Equal(before, Snapshot(two, twoEvents));
        Acknowledge(two, twoRunner, twoLook, twoEvents);
        Sequence.Finish(two, two.Facts, twoRunner, twoEvents);
        Acknowledge(control, controlRunner, controlLook, controlEvents);
        Sequence.Finish(control, control.Facts, controlRunner, controlEvents);
        Assert.Equal(18, Threat(two));
        Assert.Equal(Snapshot(control, controlEvents), Snapshot(two, twoEvents));
        Assert.All(Project(two, null, twoEvents, 1).World.Areas
            .Single(area => area.Id == two.AreaOf(DeckType.EncounterDeck).Id).Cards,
            card => { Assert.Null(card.Face); Assert.Null(card.Id); });
        for (int next = 0; next < 3; next++)
            Assert.Equal(control.Random.Generator.NextUInt32(), two.Random.Generator.NextUInt32());
    }

    private static (World World, AbilityRunner Runner) Board(string faces, string effect)
    {
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 818);
        for (int index = 0; index < 2; index++)
        {
            var seat = world.CreateSeat($"p{index}");
            seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        }
        world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea)).PlaceTokens("k_threat", 20);
        foreach (string face in faces.Split(',', StringSplitOptions.RemoveEmptyEntries))
            world.CreateCard(face, world.AreaOf(DeckType.EncounterDeck));
        var runner = new AbilityRunner(AbilityCatalog.Parse($$$"""
            {"cards":[{"card":"01001a","abilities":[{
              "trigger":{"timing":"Action","event":"WhenActionTriggered","subject":"this"},
              "effect":{{{effect}}}
            }]}]}
            """));
        world.Abilities = runner;
        return (world, runner);
    }

    private static (List<GameEvent> Events, Prompt Prompt) Start(World world, AbilityRunner runner)
    {
        var events = runner.ExecuteAction(world, new PlayerAction(Assert.Single(runner.Actions(world, 1)), [], []))
            .Events.ToList();
        return (events, Sequence.Work(world, world.Facts, runner, events)!);
    }

    private static void Acknowledge(World world, AbilityRunner runner, Prompt prompt, List<GameEvent> events) =>
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(0), events);

    private static long Threat(World world) => world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat");

    private static VisibleResult Project(World world, Prompt? prompt, List<GameEvent> events, int seat) =>
        WorldProjection.For(world, prompt, events, new RestrictedVisibilityPolicy(seat).Authorize(null, world.Players));

    private static string Snapshot(World world, List<GameEvent> events) => JsonSerializer.Serialize(new
    {
        Digest = world.Digest().Canonical(), Random = world.Random.Generator.GetState(), world.Random.Generator.WordsConsumed,
        world.Agenda.Stage, Pending = world.Agenda.Outstanding.ToArray(), Events = events.Select(EventJson.Write),
    });
}

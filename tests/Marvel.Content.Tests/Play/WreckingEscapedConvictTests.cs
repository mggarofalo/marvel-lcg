using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingEscapedConvictTests
{
    [Theory]
    [InlineData("07009", 0)]
    [InlineData("07024", 1)]
    [InlineData("07038", 2)]
    [InlineData("07053", 3)]
    [Rule("rr:activation.8")]
    public void AttackBoostQueuesSelectedVillainWithoutDealingAnotherBoost(string face, int owner)
    {
        // Printed: "that villain attacks you after this attack. That attack does not get a boost card."
        // rr:activation.8: "resolves after the current activation has finished resolving".
        var (world, groups, source) = WreckingEncounterCardFixture.Create(face, owner);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.EncounterTopology!.SelectActive(groups[owner].Villain, "setup", []);
        for (int index = 0; index < 4; index++)
            groups[index].Scheme.PlaceTokens("k_threat", index == 2 ? 0 : 5);
        World.MoveToTop(source, groups[owner].Deck);
        Start(world, groups[owner], attack: true);
        List<GameEvent> events = [];
        Finish(world, runner, events);

        Assert.Same(groups[2], world.EncounterTopology.Active);
        Assert.Same(groups[owner].Discard, source.Area);
        Assert.All(groups, group => Assert.Equal(2, group.Deck.Cards.Count));
        Assert.Equal(new[] { groups[owner].Villain.ObjectId, groups[2].Villain.ObjectId },
            events.OfType<AttackBoundary>().Where(item => item.Started).Select(item => item.Source));
        AssertSequential(events);
        Assert.Equal(owner == 0 ? 6 : owner == 3 ? 5 : 4, world.Seats[0].IdentityCard.Damage);
    }

    [Theory]
    [InlineData("07009", 0, true)]
    [InlineData("07024", 1, true)]
    [InlineData("07038", 2, true)]
    [InlineData("07053", 3, true)]
    [InlineData("07024", 1, false)]
    public void StandaloneSchemeMovesCounterButDoesNotStartAnAttack(string face, int owner, bool hero)
    {
        // ruling:8369361114d2929b: "when no attack is being made, that part of the ability is ignored."
        var (world, groups, source) = WreckingEncounterCardFixture.Create(face, owner,
            identity: hero ? "01001a" : "01001b");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        groups[0].Scheme.PlaceTokens("k_threat", 5);
        groups[1].Scheme.PlaceTokens("k_threat", 5);
        groups[3].Scheme.PlaceTokens("k_threat", 5);
        world.EncounterTopology!.SelectActive(groups[owner].Villain, "setup", []);
        World.MoveToTop(source, groups[owner].Deck);
        Start(world, groups[owner], attack: false);
        List<GameEvent> events = [];
        Finish(world, runner, events);
        Assert.Same(groups[2], world.EncounterTopology.Active);
        Assert.Empty(events.OfType<AttackBoundary>());
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Same(groups[owner].Discard, source.Area);
        Assert.All(groups, group => Assert.Equal(2, group.Deck.Cards.Count));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WaitingAttackThenSchemeRetainsAttackAncestryAndChecksHeroForm(bool hero)
    {
        // Canonical Waiting and Convict boosts with explicit group membership.
        // ruling:8369361114d2929b: "once you're finished with Villain 2's scheme".
        var (world, groups, waiting) = WreckingEncounterCardFixture.Create("07014", 0,
            identity: hero ? "01001a" : "01001b", boostFace: "07053");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.EncounterTopology!.SelectActive(groups[0].Villain, "setup", []);
        for (int index = 0; index < 3; index++) groups[index].Scheme.PlaceTokens("k_threat", 5);
        World.MoveToTop(waiting, groups[0].Deck);
        Start(world, groups[0], attack: true);
        List<GameEvent> events = [];
        Finish(world, runner, events);
        Assert.Equal(hero ? 2 : 1, events.OfType<AttackBoundary>().Count(item => item.Started));
        Assert.Equal(hero ? 6 : 4, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(1, groups[3].Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Single(groups[3].Deck.Cards);
        Assert.Equal(2, groups[0].Deck.Cards.Count);
        AssertSequential(events);
        int scheme = events.FindIndex(item => item is FieldSet field
            && field.Card == groups[3].Scheme.ObjectId && field.Field == "k_threat");
        int firstEnd = events.FindIndex(item => item is AttackBoundary { Started: false });
        Assert.True(scheme > firstEnd);
        if (hero) Assert.True(events.FindLastIndex(item => item is AttackBoundary { Started: true }) > scheme);
    }

    [Fact]
    [Rule("rr:first-player.1")]
    public void FirstPlayerResolvesTiedLeastThreatForTheAttackedPlayer()
    {
        // rr:first-player.1: "the first player selects among the eligible options."
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07009", 0, players: 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        groups[0].Scheme.PlaceTokens("k_threat", 5);
        groups[1].Scheme.PlaceTokens("k_threat", 5);
        World.MoveToTop(source, groups[1].Deck);
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 1,
            Subject: groups[1].Villain.ObjectId, Seat: 1));
        Prompt? choice;
        while (true)
        {
            choice = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
            if (!choice.Cancellable) break;
            Sequence.Answer(world, world.Facts, runner, choice, Decision.Decline, []);
        }
        Assert.Equal(world.FirstPlayer, choice.Player);
        Assert.Equal(new[] { groups[2].Villain.ObjectId, groups[3].Villain.ObjectId },
            choice.Affordances.Select(item => item.Id));
        Sequence.Answer(world, world.Facts, runner, choice, Decision.Take(groups[3].Villain.ObjectId), []);
        Finish(world, runner, []);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(5, world.Seats[1].IdentityCard.Damage);
        Assert.Same(groups[3], world.EncounterTopology!.Active);
        Assert.Same(groups[0].Discard, source.Area);
    }

    [Fact]
    public void CounterChangeDuringOriginalAttackDoesNotRetargetDelayedAttack()
    {
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07009", 0);
        // Synthetic optional attack-end response creates a routing checkpoint; Convict stays canonical.
        var document = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        var identity = document["cards"]!.AsArray().Single(row => row!["card"]!.GetValue<string>() == "01001a")!;
        identity["abilities"]!.AsArray().Add(JsonNode.Parse("""
        {"trigger":{"event":"WhenAttackEnds","timing":"Response","player":"you"},
         "effect":{"draw":{"player":"you","count":1}}}
        """));
        var runner = new AbilityRunner(AbilityCatalog.Parse(document.ToJsonString()));
        world.Abilities = runner;
        for (int index = 0; index < 4; index++)
            groups[index].Scheme.PlaceTokens("k_threat", index == 2 ? 0 : 5);
        World.MoveToTop(source, groups[1].Deck);
        Start(world, groups[1], attack: true);
        List<GameEvent> events = [];
        // Synthetic routing perturbation between canonical activations isolates the captured villain.
        Prompt checkpoint;
        for (int count = 0; ; count++)
        {
            checkpoint = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
            Assert.True(count < 15);
            if (events.OfType<AttackBoundary>().Any(item => !item.Started)) break;
            Assert.True(checkpoint.Cancellable, checkpoint.Description);
            Sequence.Answer(world, world.Facts, runner, checkpoint, Decision.Decline, events);
        }
        Assert.Single(events.OfType<AttackBoundary>(), item => item.Started);
        world.EncounterTopology!.SelectActive(groups[3].Villain, "synthetic counter change", []);
        Sequence.Answer(world, world.Facts, runner, checkpoint, Decision.Decline, events);
        Finish(world, runner, events);
        Assert.Equal(groups[2].Villain.ObjectId,
            events.OfType<AttackBoundary>().Last(item => item.Started).Source);
        Assert.Same(groups[3], world.EncounterTopology.Active);
    }

    [Theory]
    [InlineData("07009", 0)]
    [InlineData("07024", 1)]
    [InlineData("07038", 2)]
    [InlineData("07053", 3)]
    public void MinionRevealEngagesWithPrintedStatsAndSurges(string face, int owner)
    {
        var (world, groups, source) = WreckingEncounterCardFixture.Create(face, owner);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            1, 1, Subject: source.ObjectId, Seat: 0));
        Finish(world, runner, []);
        Assert.Same(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)), source.Area);
        Assert.Equal(PlayArea.Of(0), source.Area.PlayArea);
        Assert.Equal(1, StateFields.Modified(world, source, "attack", world.Facts, world.Players));
        Assert.Equal(1, StateFields.Modified(world, source, "scheme", world.Facts, world.Players));
        Assert.Equal(2, world.Facts.PrintedValue(source.FaceId, "HP", world.Players));
        Assert.Single(groups[1].Deck.Cards);
        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
    }

    private static void Start(World world, EncounterDeckGroup group, bool attack) =>
        world.Agenda.Add(new PhaseStep(attack ? new EnemyAttackOperation() : new EnemySchemeOperation(),
            1, 1, Subject: group.Villain.ObjectId, Seat: 0));

    private static void AssertSequential(List<GameEvent> events)
    {
        bool active = false;
        foreach (var item in events.OfType<AttackBoundary>())
        {
            Assert.NotEqual(active, item.Started);
            active = item.Started;
        }
        Assert.False(active);
    }

    private static void Finish(World world, AbilityRunner runner, List<GameEvent> events)
    {
        for (int count = 0; ; count++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null) return;
            Assert.True(count < 30);
            Assert.True(prompt.Cancellable, prompt.Description);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, events);
        }
    }
}




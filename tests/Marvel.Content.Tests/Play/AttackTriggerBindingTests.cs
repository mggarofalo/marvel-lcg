using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AttackTriggerBindingTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:attack-player-ability-type.2.1")]
    [Rule("rr:attack-player-ability-type.5")]
    [Rule("rr:triggering-condition.1")]
    [Fact]
    public void RecipientBindingSurvivesChoiceAndEachInstanceMayBeAnsweredOnce()
    {
        var probed = RunResponses(probe: true);
        var control = RunResponses(probe: false);
        Assert.Equal(control.World.Digest().Canonical(), probed.World.Digest().Canonical());
        Assert.Equal(control.Events.Select(item => System.Text.Json.JsonSerializer.Serialize(item, item.GetType())),
            probed.Events.Select(item => System.Text.Json.JsonSerializer.Serialize(item, item.GetType())));
        Assert.Equal(control.World.Random.Generator.GetState().Words, probed.World.Random.Generator.GetState().Words);
        Assert.Equal(control.World.Random.Generator.GetState().Index, probed.World.Random.Generator.GetState().Index);
        Assert.Equal(control.World.Agenda.Outstanding.Select(step => step.What), probed.World.Agenda.Outstanding.Select(step => step.What));
    }

    private static (World World, GameEvent[] Events) RunResponses(bool probe)
    {
        // "A single attack" considers the attacker to have "attacked each of
        // those enemies". This synthetic response tests the shared-window contract.
        var (world, first, second) = Board();
        var source = world.CreateCard("01007", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.Agenda.Add(new PhaseStep(new CompletedCardEffectOperation(), 1, 1));
        var occurrence = world.Agenda.Begin(world, Cards);
        world.Agenda.Advance();
        world.Agenda.Advance();
        occurrence.Also(Steps.AttackEnds);
        occurrence.Also(Attacked.Capture(first, Cards, 2));
        occurrence.Also(Attacked.Capture(second, Cards, 2));
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01007","abilities":[{
              "trigger":{"event":"WhenAttackEnds","timing":"Response","target":"enemy"},
              "effect":{"choose":{"options":[
                {"heal":{"card":"trigger.target","amount":1}},
                {"heal":{"card":"trigger.target","amount":2}}]}}}]}]}
            """));
        if (probe) AssertQueryPurity(world, occurrence, runner);
        var events = new List<GameEvent>();
        var offered = Sequence.Work(world, Cards, runner, events)!;
        Assert.Equal(2, offered.Affordances.Count);
        Assert.Equal(2, offered.Affordances.Select(option => option.Id).Distinct().Count());
        Assert.All(offered.Affordances, option => Assert.Equal(source.ObjectId, option.AnchorId));

        Resolve(offered.Affordances[1].Id);
        Assert.Equal(2, first.Damage);
        Assert.Equal(1, second.Damage);
        Assert.Same(occurrence.Attacks[1], Assert.Single(occurrence.Resolutions).TriggeringAttack);
        Assert.Single(offered!.Affordances);
        Resolve(offered.Affordances[0].Id);
        Assert.Equal(1, first.Damage);
        Assert.Null(offered);
        Assert.Equal(2, occurrence.Resolutions.Count);
        Assert.All(occurrence.Resolutions, entry => Assert.Equal(ResolutionStatus.Resolved, entry.Status));
        return (world, [.. events]);

        void Resolve(int handle)
        {
            Sequence.Answer(world, Cards, runner, offered!, Decision.Take(handle), events);
            var choice = Sequence.Work(world, Cards, runner, events)!;
            Assert.Equal(Marvel.Rules.Prompts.Question.Option, choice.Asking);
            Sequence.Answer(world, Cards, runner, choice, Decision.Take(choice.Affordances[0].Id), events);
            offered = Sequence.Work(world, Cards, runner, events);
        }
    }


    [Rule("rr:attack-player-ability-type.2.1")]
    [Rule("rr:attack-player-ability-type.5")]
    [Theory]
    [InlineData("enemy", 3)]
    [InlineData("minion", 2)]
    [InlineData("villain", 1)]
    [InlineData(null, 1)]
    public void TargetRolesCreateOnlyTheirOwnInstancesAndWholeAttackRemainsOnce(string? target, int expected)
    {
        // RR5: "attacked each of those enemies"; RR2.1: "a single attack".
        // Vendored ruling:f32c53356db718a6 distinguishes one whole-attack
        // Pitchback response from Jarnbjorn's one response per attacked enemy.
        var (world, first, second) = Board();
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var source = world.CreateCard("01007", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var occurrence = new Occurrence(1, Steps.AttackEnds);
        foreach (var card in new[] { first, villain, second }) occurrence.Also(Attacked.Capture(card, Cards));
        var runner = Response(target);
        var offered = runner.Waiting(world, occurrence, WindowKind.Response);
        Assert.Equal(expected, offered.Count);
        Assert.All(offered, offer => Assert.Equal(source.ObjectId, offer.Card));
        if (target is null) Assert.Null(Assert.Single(offered).TriggeringAttack);
        else Assert.Equal(expected, offered.Select(offer => offer.TriggeringAttack!.Card).Distinct().Count());
    }

    [Rule("rr:triggering-condition.2")]
    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    public void AttackAndDefeatClausesMustNameTheSameIncarnation(bool sameIncarnation, int expected)
    {
        // Several triggering conditions share a window, but their roles must
        // describe one fact; another target's defeat cannot satisfy this one.
        var (world, first, second) = Board();
        world.CreateCard("01007", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var occurrence = new Occurrence(1, Steps.AttackEnds);
        occurrence.Also(Attacked.Capture(first, Cards));
        occurrence.Also(Attacked.Capture(second, Cards));
        occurrence.Also(new Defeated(second.ObjectId, 0, "Attack")
        { Kind = CardKind.Minion, Incarnation = second.Incarnation + (sameIncarnation ? 0 : 1) });
        var offered = Response("enemy", Steps.CardDefeated).Waiting(world, occurrence, WindowKind.Response);
        Assert.Equal(expected, offered.Count);
        if (sameIncarnation) Assert.Equal(second.ObjectId, Assert.Single(offered).TriggeringAttack!.Card);
    }

    [Rule("rr:triggering-condition.2")]
    [Fact]
    public void AnotherRecipientsDamageDoesNotSatisfyAttackAndDamage()
    {
        var (world, first, second) = Board();
        world.CreateCard("01007", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var occurrence = new Occurrence(1, new[] { Steps.AttackEnds, Steps.DamageDealt });
        occurrence.Also(Attacked.Capture(first, Cards, 0));
        occurrence.Also(Attacked.Capture(second, Cards, 2));
        var offered = Response("enemy", Steps.DamageDealt).Waiting(world, occurrence, WindowKind.Response);
        Assert.Equal(second.ObjectId, Assert.Single(offered).TriggeringAttack!.Card);
    }


    [Fact]
    public void ReenteredPhysicalCardIsNotTheTargetOfTheEarlierAttack()
    {
        var (world, first, _) = Board();
        world.CreateCard("01007", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var occurrence = new Occurrence(1, Steps.AttackEnds);
        occurrence.Also(Attacked.Capture(first, Cards, 2));
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01007","abilities":[{
              "trigger":{"event":"WhenAttackEnds","timing":"Response","target":"enemy"},
              "effect":{"heal":{"card":"trigger.target","amount":1}}}]}]}
            """));
        Assert.Single(runner.Waiting(world, occurrence, WindowKind.Response));
        int incarnation = first.Incarnation;
        World.MoveToTop(first, world.AreaOf(DeckType.EncounterDiscardPile));
        World.MoveToTop(first, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        first.TakeDamage(2);
        Assert.NotEqual(incarnation, first.Incarnation);
        Assert.Empty(runner.Waiting(world, occurrence, WindowKind.Response));
        Assert.Equal(2, first.Damage);
    }

    private static void AssertQueryPurity(World world, Occurrence occurrence, AbilityRunner runner)
    {
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();
        var effects = world.Effects.Registered.ToArray();
        for (int repeat = 0; repeat < 3; repeat++)
            foreach (var offer in runner.Waiting(world, occurrence, WindowKind.Response))
                Assert.NotNull(runner.Describe(world, offer).DisplayLabel);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Equal(effects, world.Effects.Registered);
        Assert.Empty(occurrence.Resolutions);
    }

    private static AbilityRunner Response(string? target, string? also = null)
    {
        string role = target is null ? "" : $", \"target\":\"{target}\"";
        string extra = also is null ? "" : $", \"alsoHappened\":\"{also}\"";
        return new AbilityRunner(AbilityCatalog.Parse($$$$"""
            {"cards":[{"card":"01007","abilities":[{
              "trigger":{"event":"WhenAttackEnds","timing":"Response","subject":"game"{{{{role}}}}{{{{extra}}}}},
              "effect":{"placeCounters":{"card":"this","counter":"observed","count":1}}}]}]}
            """));
    }

    private static (World World, Card First, Card Second) Board()
    {
        var world = new World(Cards, players: 1);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        var first = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var second = world.CreateCard("01102", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        first.TakeDamage(2);
        second.TakeDamage(2);
        return (world, first, second);
    }
}

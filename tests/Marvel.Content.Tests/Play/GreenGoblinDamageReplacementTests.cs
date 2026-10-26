using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class GreenGoblinDamageReplacementTests
{
    [Fact]
    public void MissingGoblinRevealRejectsBeforeTheLastInfamyIsRemoved()
    {
        var runner = new AbilityRunner(AbilityBook.None);
        var world = new World(new Facts(), 1) { Abilities = runner };
        world.CreateSeat("p0");
        var villain = world.CreateCard("02001a,02001b", world.AreaOf(DeckType.VillainArea));
        var environment = world.CreateCard("02006a,02006b", world.AreaOf(DeckType.EnvironmentArea));
        environment.PlaceTokens("c_infamy", 1);
        var events = new List<GameEvent>();
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed);

        Assert.Throws<RulesNotImplementedException>(() =>
            GreenGoblinTransition.RemoveCounters(world, environment, "infamy", 1, "Test", events));

        Assert.Equal(before, (world.Digest().Canonical(), world.Random.Generator.WordsConsumed));
        Assert.Equal("02001a", villain.FaceId);
        Assert.Equal("02006a", environment.FaceId);
        Assert.Equal(1, environment.Tokens["c_infamy"]);
        Assert.Empty(events);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Rule("rr:indirect-damage.1")]
    [Fact]
    public void PrintedGoblinRevealSuspendsForAllocationAndCompletes()
    {
        // 02001b: “Deal 3 indirect damage to each player in hero form.”
        var runner = StageOneRevealRunner();
        var facts = new Facts();
        var world = new World(facts, 1) { Abilities = runner };
        world.CreateSeat("p0");
        var hero = world.CreateCard("hero", world.Seats[0].Hero);
        world.Seats[0].IdentityCard = hero;
        var ally = world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var villain = world.CreateCard("02001a,02001b", world.AreaOf(DeckType.VillainArea));
        var environment = world.CreateCard("02006a,02006b", world.AreaOf(DeckType.EnvironmentArea));
        environment.PlaceTokens("c_infamy", 1);
        var events = new List<GameEvent>();

        GreenGoblinTransition.RemoveCounters(world, environment, "infamy", 1, "Test", events);
        var prompt = Sequence.Work(world, facts, runner, events);
        Assert.NotNull(prompt);
        Assert.Equal(Question.Element, prompt.Asking);
        Assert.Equal(0, hero.Damage);
        Assert.Equal(0, ally.Damage);
        Assert.Contains(world.Agenda.Outstanding,
            step => step.What == Steps.ResolveEachPlayer);
        var allocation = Assert.Single(prompt.Affordances);
        Assert.Equal(3, allocation.Targets!.Min);

        Sequence.Answer(world, facts, runner, prompt!,
            Decision.Take(allocation.Id, [ally.ObjectId, hero.ObjectId, hero.ObjectId], []), events);
        Sequence.Finish(world, facts, runner, events);

        Assert.Equal("02001b", villain.FaceId);
        Assert.Equal("02006b", environment.FaceId);
        Assert.Equal(1, ally.Damage);
        Assert.Equal(2, hero.Damage);
        Assert.Empty(world.Agenda.Outstanding);
        Assert.False(world.Agenda.IsBusy);
    }

    private static AbilityRunner StageOneRevealRunner() => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"02001b","abilities":[{
          "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
          "effect":{"eachPlayer":{"effect":{"if":{
            "test":{"inForm":{"player":"you","form":"hero"}},
            "then":{"indirectDamage":{"among":{"query":"heroesAndAllies"},"amount":3}}
          }}}}
        }]}]}
        """));

    [Fact]
    public void AuthoredCounterRemovalUsesTheSameFormTransition()
    {
        var runner = new AbilityRunner(AbilityBook.None);
        var facts = new Facts();
        var world = new World(facts, 1) { Abilities = runner };
        world.CreateSeat("p0");
        var villain = world.CreateCard("02001a,02001b", world.AreaOf(DeckType.VillainArea));
        villain.TurnTo("02001b");
        var environment = world.CreateCard("02006a,02006b", world.AreaOf(DeckType.EnvironmentArea));
        environment.TurnTo("02006b");
        environment.PlaceTokens("c_madness", 1);
        var events = new List<GameEvent>();

        AbilityCardOperations.RemoveCounters(world, runner, environment,
            "madness", 1, "Test", events);

        Assert.Equal("02001a", villain.FaceId);
        Assert.Equal("02006a", environment.FaceId);
        Assert.Equal(2, environment.Tokens["c_infamy"]);
        Assert.Contains(events, item => item is CardsFlipped flip
            && flip.Cards.Contains(villain.ObjectId));
    }

    [Fact]
    public void AnotherForcedReplacementMakesThePreviewUncertain()
    {
        var book = AbilityCatalog.Parse("""
            {"cards":[{"card":"02001a","abilities":[{
              "trigger":{"event":"WhenDamageWouldBeDealt","timing":"ForcedInterrupt",
                "subject":"game"},
              "effect":{"preventDamage":"trigger.target"}
            }]}]}
            """);
        var runner = new AbilityRunner(book);
        var facts = new Facts();
        var world = new World(facts, 1) { Abilities = runner };
        world.CreateSeat("p0");
        var identity = world.CreateCard("hero", world.Seats[0].Hero);
        world.Seats[0].IdentityCard = identity;
        var villain = world.CreateCard("02001a,02001b", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("02006a,02006b", world.AreaOf(DeckType.EnvironmentArea));

        var preview = runner.PreviewDamageReplacement(world, villain, identity, 5);

        Assert.IsType<RuleProjection<long>.Unsupported>(preview.Result);
    }

    [Rule("rr:replacement-effect.1")]
    [Rule("rr:damage.step.3")]
    [Fact]
    public void OneInstanceConsumesLastInfamyAndLaterInstanceDamagesGoblin()
    {
        // 02001a: “When Norman Osborn would take any amount of damage, remove
        // that many infamy counters from Criminal Enterprise instead.”
        // ruling:e4ea1bf855e41aac confirms that a later instance sees Goblin.
        var runner = StageOneRevealRunner();
        var facts = new Facts();
        var world = new World(facts, 1) { Abilities = runner };
        world.CreateSeat("p0");
        var identity = world.CreateCard("alterego", world.Seats[0].Hero);
        world.Seats[0].IdentityCard = identity;
        var villain = world.CreateCard("02001a,02001b", world.AreaOf(DeckType.VillainArea));
        var environment = world.CreateCard("02006a,02006b", world.AreaOf(DeckType.EnvironmentArea));
        environment.PlaceTokens("c_infamy", 1);
        var attachment = world.CreateCard("attachment", world.AreaOf(
            DeckType.UpgradesArea, villain.Area.PlayArea, villain.ObjectId));
        var status = world.CreateCard("status", world.AreaOf(
            DeckType.StatusArea, villain.Area.PlayArea, villain.ObjectId));
        var boost = world.CreateCard("boost", world.AreaOf(
            DeckType.BoostCardsDeck, villain.Area.PlayArea, villain.ObjectId));
        var events = new List<GameEvent>();
        var preview = runner.PreviewDamageReplacement(world, villain, identity, 5);
        Assert.Equal(0, Assert.IsType<RuleProjection<long>.Known>(preview.Result).Value);

        var first = DamagePlacement.Resolve(world, facts,
            new DamageRequest(identity, villain, 5, "Test", "Damage"), events);
        Sequence.Finish(world, facts, runner, events);

        Assert.True(first.IsComplete);
        Assert.Equal(0, first.Taken);
        Assert.Equal(0, villain.Damage);
        Assert.Equal("02001b", villain.FaceId);
        Assert.Equal("02006b", environment.FaceId);
        Assert.Equal(2, environment.Tokens["c_madness"]);
        Assert.Equal(villain.ObjectId, attachment.Area.Host);
        Assert.Equal(villain.ObjectId, status.Area.Host);
        Assert.Equal(villain.ObjectId, boost.Area.Host);
        Assert.Contains(events, item => item is CardsFlipped flip
            && flip.Cards.Contains(villain.ObjectId));

        var second = DamagePlacement.Resolve(world, facts,
            new DamageRequest(identity, villain, 2, "Test", "Damage"), events);
        Sequence.Finish(world, facts, runner, events);
        Assert.True(second.IsComplete);
        Assert.Equal(2, second.Taken);
        Assert.Equal(2, villain.Damage);
    }

    private sealed class Facts : ICardFacts
    {
        public CardKind Kind(string face) => face switch
        {
            "hero" => CardKind.Hero,
            "alterego" => CardKind.AlterEgo,
            "ally" => CardKind.Ally,
            "02001a" or "02001b" => CardKind.EncounterVillain,
            "02006a" or "02006b" => CardKind.Environment,
            _ => CardKind.Treachery,
        };

        public IReadOnlyList<string> Traits(string face) => [];
        public string Title(string face) => face switch
        {
            "02001a" => "Norman Osborn",
            "02001b" => "Green Goblin",
            _ => face,
        };
        public IReadOnlyDictionary<string, string> Attributes(string face) =>
            face is "02001a" or "02001b"
                ? new Dictionary<string, string> { ["HP"] = "14" }
                : face == "hero" ? new Dictionary<string, string> { ["HP"] = "10" }
                : face == "ally" ? new Dictionary<string, string> { ["HP"] = "3" }
                : new Dictionary<string, string>();
        public long PrintedValue(string face, string attribute, int players, long fallback = 0) =>
            Attributes(face).TryGetValue(attribute, out string? value)
            && long.TryParse(value, out long number) ? number : fallback;
    }
}

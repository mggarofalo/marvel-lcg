using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class GreenGoblinActivationReplacementTests
{
    [Rule("rr:replacement-effect.1")]
    [Theory]
    [InlineData("02001a", 1)]
    [InlineData("02002a", 2)]
    [InlineData("02003a", 3)]
    public void NormanPlacesInfamyInsteadOfAttacking(string face, long amount)
    {
        // “When Norman Osborn would attack, place [stage] infamy ... instead.”
        // pack:mc02:new-rules#when-norman-osborn-attacks says no attack occurs.
        var (world, facts, villain, environment, boost) = Board(face);
        var abilities = new Recorder();
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Subject: villain.ObjectId, Seat: 0));

        Sequence.Finish(world, facts, abilities, events);

        Assert.Equal(2 + amount, environment.Tokens["c_infamy"]);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Same(boost, Assert.Single(world.AreaOf(DeckType.EncounterDeck).Cards));
        Assert.Null(world.Attack);
        Assert.Null(world.Activation);
        Assert.DoesNotContain(abilities.Windows, entry => entry == (WindowKind.Response, Steps.AttackInitiated));
        Assert.Contains(events, item => item is FieldSet field
            && field.Card == environment.ObjectId && field.Field == "c_infamy"
            && field.From == 2 && field.To == 2 + amount);
    }

    [Rule("rr:replacement-effect.1")]
    [Theory]
    [InlineData("02001b", 1)]
    [InlineData("02002b", 1)]
    [InlineData("02003b", 2)]
    public void GoblinRemovesMadnessInsteadOfScheming(string face, long amount)
    {
        // “When Green Goblin would scheme, remove [stage] madness ... instead.”
        // pack:mc02:new-rules#when-green-goblin-schemes excludes scheme triggers.
        var (world, facts, villain, environment, boost) = Board(face);
        var abilities = new Recorder();
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(new EnemySchemeOperation(), 1, 2,
            Subject: villain.ObjectId, Seat: 0));

        Sequence.Finish(world, facts, abilities, events);

        Assert.Equal(3 - amount, environment.Tokens["c_madness"]);
        Assert.Equal(0, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Same(boost, Assert.Single(world.AreaOf(DeckType.EncounterDeck).Cards));
        Assert.Null(world.Activation);
        Assert.DoesNotContain(abilities.Windows, entry => entry == (WindowKind.Response, Steps.EnemySchemes));
        Assert.Contains(events, item => item is FieldSet field
            && field.Card == environment.ObjectId && field.Field == "c_madness"
            && field.From == 3 && field.To == 3 - amount);
    }

    [Rule("rr:status-cards.2")]
    [Theory]
    [InlineData("02001a", true, "stunned", "c_infamy", 2)]
    [InlineData("02001b", false, "confused", "c_madness", 3)]
    public void StatusCancelsBeforePrintedReplacement(
        string face, bool attack, string status, string field, long expected)
    {
        // “Status card abilities have timing priority over all conflicting triggered abilities.”
        // ruling:e8098d547f8f4d99 and ruling:3f30cb0a4ea08236 clarify these activations.
        var (world, facts, villain, environment, boost) = Board(face);
        Statuses.Give(world, villain, status);
        world.Agenda.Add(new PhaseStep(
            attack ? new EnemyAttackOperation() : new EnemySchemeOperation(),
            1, 2, Subject: villain.ObjectId, Seat: 0));

        Sequence.Finish(world, facts, new NoCardAbilities(), []);

        Assert.Equal(expected, environment.Tokens[field]);
        Assert.False(Statuses.Has(world, villain, status));
        Assert.Same(boost, Assert.Single(world.AreaOf(DeckType.EncounterDeck).Cards));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WrongOrAbsentEnvironmentRejectsWithoutCounterOrEventChanges(bool wrongFace)
    {
        var (world, facts, villain, environment, _) = Board("02001b");
        if (wrongFace) environment.TurnTo("02006a");
        else World.MoveToTop(environment, world.AreaOf(DeckType.EncounterDiscardPile));
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(new EnemySchemeOperation(), 1, 2,
            Subject: villain.ObjectId, Seat: 0));
        string before = world.Digest().Canonical();

        Assert.Throws<RulesNotImplementedException>(() =>
            Sequence.Finish(world, facts, new NoCardAbilities(), events));

        Assert.Equal(before, world.Digest().Canonical());
        Assert.Empty(events);
    }

    [Rule("rr:replacement-effect.1")]
    [Fact]
    public void LastMadnessDuringSchemeFlipsBothFacesAndStartsInfamy()
    {
        // 02006b: “If there are no madness counters here, flip Green Goblin
        // and State of Madness.” The newly active 02006a starts with 2 per hero.
        var (world, facts, villain, environment, _) = Board("02001b");
        environment.PlaceTokens("c_madness", -2);
        world.Agenda.Add(new PhaseStep(new EnemySchemeOperation(), 1, 2,
            Subject: villain.ObjectId, Seat: 0));
        var events = new List<GameEvent>();

        Sequence.Finish(world, facts, new NoCardAbilities(), events);

        Assert.Equal("02001a", villain.FaceId);
        Assert.Equal("02006a", environment.FaceId);
        Assert.Equal(0, environment.Tokens["c_madness"]);
        Assert.Equal(2, environment.Tokens["c_infamy"]);
        Assert.Equal(0, world.TheCardIn(DeckType.MainSchemesArea)!
            .Tokens.GetValueOrDefault("k_threat"));
        Assert.Contains(events, item => item is CardsFlipped flip
            && flip.Cards.Contains(villain.ObjectId));
    }

    [Rule("rr:in-play-and-out-of-play.6")]
    [Fact]
    public void LastInfamyExposesGoblinAndInvokesHisReveal()
    {
        // 02006a flips both cards at zero; the pack insert says changing form
        // triggers Green Goblin's “When Revealed” ability.
        var (world, facts, villain, environment, _) = Board("02001a");
        environment.PlaceTokens("c_infamy", -1);
        var abilities = new RevealRecorder();
        world.Abilities = abilities;
        var events = new List<GameEvent>();

        GreenGoblinTransition.RemoveCounters(world, environment, "infamy", 5,
            "Test", events);

        Assert.Equal("02001b", villain.FaceId);
        Assert.Equal("02006b", environment.FaceId);
        Assert.Equal(0, environment.Tokens["c_infamy"]);
        Assert.Equal(2, environment.Tokens["c_madness"]);
        Assert.Equal([villain.ObjectId], abilities.Revealed);
    }

    [Rule("rr:villain-defeat.3")]
    [Theory]
    [InlineData("02001a", "02002a")]
    [InlineData("02001b", "02002b")]
    public void DefeatPromotesTheNextStageOnTheSameSide(string face, string nextFace)
    {
        // pack:mc02:new-rules#when-a-villain-stage-is-defeated requires the
        // same side; rr:villain-defeat.3 carries hosted cards to that stage.
        var (world, facts, villain, environment, _) = Board(face);
        var next = world.CreateCard("02002a,02002b", world.AreaOf(DeckType.VillainDeck));
        var status = world.CreateCard("status", world.AreaOf(
            DeckType.StatusArea, villain.Area.PlayArea, villain.ObjectId));

        var events = new List<GameEvent>();
        var damage = DamagePlacement.Resolve(world, facts,
            new DamageRequest(world.Seats[0].IdentityCard, villain, 14, "Test", "Damage"), events);
        Sequence.Finish(world, facts, new NoCardAbilities(), events);

        Assert.True(damage.IsComplete);
        Assert.Equal(nextFace, next.FaceId);
        Assert.Same(next, world.TheCardIn(DeckType.VillainArea));
        Assert.Equal(next.ObjectId, status.Area.Host);
        Assert.Equal(face.EndsWith('a') ? "02006a" : "02006b", environment.FaceId);
        Assert.Equal(0, next.Damage);
    }

    private static (World World, Facts Facts, Card Villain, Card Environment, Card Boost)
        Board(string face)
    {
        var facts = new Facts();
        var world = new World(facts, 1);
        world.CreateSeat("p0");
        bool attacking = face.EndsWith('a');
        var identity = world.CreateCard(attacking ? "hero" : "alterego", world.Seats[0].Hero);
        world.Seats[0].IdentityCard = identity;
        identity.Exhaust();
        string stage = face[..5];
        var villain = world.CreateCard(stage + "a," + stage + "b", world.AreaOf(DeckType.VillainArea));
        villain.TurnTo(face);
        var environment = world.CreateCard("02006a,02006b", world.AreaOf(DeckType.EnvironmentArea));
        environment.TurnTo(attacking ? "02006a" : "02006b");
        environment.PlaceTokens(attacking ? "c_infamy" : "c_madness", attacking ? 2 : 3);
        world.CreateCard("scheme", world.AreaOf(DeckType.MainSchemesArea));
        var boost = world.CreateCard("boost", world.AreaOf(DeckType.EncounterDeck));
        return (world, facts, villain, environment, boost);
    }

    private sealed class Recorder : NoCardAbilities
    {
        public List<(WindowKind, string)> Windows { get; } = [];

        public override IReadOnlyList<PendingAbility> Waiting(
            World world, Occurrence occurrence, WindowKind window)
        {
            if (occurrence.Is(Steps.AttackInitiated)) Windows.Add((window, Steps.AttackInitiated));
            if (occurrence.Is(Steps.EnemySchemes)) Windows.Add((window, Steps.EnemySchemes));
            return [];
        }
    }

    private sealed class RevealRecorder : NoCardAbilities
    {
        public List<int> Revealed { get; } = [];

        public override AbilityExecutionResult WhenRevealed(
            World world, AbilityExecution entry, Card card, int player)
        {
            Revealed.Add(card.ObjectId);
            return base.WhenRevealed(world, entry, card, player);
        }
    }

    private sealed class Facts : ICardFacts
    {
        public CardKind Kind(string face) => face switch
        {
            "hero" => CardKind.Hero,
            "alterego" => CardKind.AlterEgo,
            "02001a" or "02002a" or "02003a" or "02001b" or "02002b" or "02003b"
                => CardKind.EncounterVillain,
            "02006a" or "02006b" => CardKind.Environment,
            "scheme" => CardKind.MainScheme,
            _ => CardKind.Treachery,
        };

        public IReadOnlyList<string> Traits(string face) => [];
        public string Title(string face) => face switch
        {
            "02001a" or "02002a" or "02003a" => "Norman Osborn",
            "02001b" or "02002b" or "02003b" => "Green Goblin",
            _ => face,
        };
        public IReadOnlyDictionary<string, string> Attributes(string face) =>
            face == "scheme" ? new Dictionary<string, string> { ["TargetThreat"] = "99" }
                : face.StartsWith("0200") ? new Dictionary<string, string> { ["HP"] = "14" }
                : new Dictionary<string, string>();
        public long PrintedValue(string face, string attribute, int players, long fallback = 0) =>
            Attributes(face).TryGetValue(attribute, out string? value)
            && long.TryParse(value, out long number) ? number : fallback;
    }
}

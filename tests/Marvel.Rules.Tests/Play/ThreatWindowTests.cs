using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>Interrupt windows around imminent threat assignments.</summary>
public sealed class ThreatWindowTests
{
    [Rule("rr:prevent.2")]
    [Fact]
    public void PreventionChangesTheAssignedAmountBeforeItIsPlaced()
    {
        // "When threat is prevented, reduce the amount of threat being
        // assigned before it is placed on the scheme." The prompt therefore
        // sees the assignment and the scheme still has no tokens.
        var (world, facts, scheme, source) = Board(escalation: 3);
        world.Agenda.Add(new PhaseStep(Steps.PlaceThreat, 1, 1));
        var cards = new Interrupter(source.ObjectId, occurrence =>
            occurrence.Threat!.Prevent(1));
        var events = new List<GameEvent>();

        var asked = Sequence.Work(world, facts, cards, events);

        Assert.NotNull(asked);
        Assert.False(scheme.Tokens.ContainsKey("k_threat"));
        var occurrence = world.Agenda.Occurrence!;
        Assert.Equal(Steps.ThreatWouldBePlaced, occurrence.Conditions.Single());
        Assert.Equal(scheme.ObjectId, occurrence.Threat!.Scheme);
        Assert.Equal(scheme.ObjectId, occurrence.Threat.Source);
        Assert.Equal(3, occurrence.Threat.Assigned);
        Assert.Equal(ThreatCause.VillainPhase, occurrence.Threat.Cause);

        Sequence.Answer(
            world, facts, cards, asked!, Decision.Take(Interrupter.Handle), events);
        Assert.Null(Sequence.Work(world, facts, cards, events));

        Assert.Equal(2, scheme.Tokens["k_threat"]);
        Assert.Equal(2, occurrence.Threat.Remaining);
        Assert.Contains(Steps.ThreatPlaced, occurrence.Conditions);
    }

    [Rule("rr:replacement-effect.1")]
    [Rule("rr:interrupt.4")]
    [Fact]
    public void ReplacingTheAssignmentClosesItsWindowsAndSkipsPlacement()
    {
        // A replaced effect is no longer imminent and has neither further
        // interrupts nor responses. Leaving the step on Interrupts would ask
        // the same question again; moving it to Responses would be just as
        // wrong.
        var (world, facts, scheme, source) = Board(escalation: 3);
        world.Agenda.Add(new PhaseStep(Steps.PlaceThreat, 1, 1));
        var cards = new Interrupter(source.ObjectId, occurrence =>
            occurrence.Threat!.Replace());

        var asked = Sequence.Work(world, facts, cards, []);
        Sequence.Answer(
            world, facts, cards, asked!, Decision.Take(Interrupter.Handle), []);

        Assert.False(world.Agenda.IsBusy);
        Assert.False(world.Windows.IsResolving);
        Assert.False(scheme.Tokens.ContainsKey("k_threat"));
    }

    [Rule("rr:prevent.2")]
    [Fact]
    public void PreventingAllThreatCreatesNoPlacedThreatCondition()
    {
        var (world, facts, scheme, source) = Board(escalation: 1);
        world.Agenda.Add(new PhaseStep(Steps.PlaceThreat, 1, 1));
        var cards = new Interrupter(source.ObjectId, occurrence =>
            occurrence.Threat!.Prevent(99));

        var asked = Sequence.Work(world, facts, cards, []);
        var occurrence = world.Agenda.Occurrence!;
        Sequence.Answer(
            world, facts, cards, asked!, Decision.Take(Interrupter.Handle), []);
        Assert.Null(Sequence.Work(world, facts, cards, []));

        Assert.False(scheme.Tokens.ContainsKey("k_threat"));
        Assert.DoesNotContain(Steps.ThreatPlaced, occurrence.Conditions);
        Assert.Contains(Steps.VillainPhaseStepOneEnds, occurrence.Conditions);
    }

    [Rule("rr:scheme-enemy-activation.step.3")]
    [Fact]
    public void SchemeThreatFreezesItsSourceTargetAndAmountAtTheWindow()
    {
        var (world, facts, scheme, enemy) = Board(escalation: 0, schemeValue: 4);
        world.Agenda.Add(new PhaseStep(
            Steps.SchemeThreat, 1, 3, Subject: enemy.ObjectId, Seat: 0));

        var occurrence = world.Agenda.Begin(world, facts);

        Assert.Equal(enemy.ObjectId, occurrence.Subject);
        Assert.Equal(scheme.ObjectId, occurrence.Target);
        Assert.Equal(enemy.ObjectId, occurrence.Threat!.Source);
        Assert.Equal(4, occurrence.Threat.Assigned);
        Assert.Equal(ThreatCause.EnemyScheme, occurrence.Threat.Cause);
        Assert.Equal(0, occurrence.Threat.Player);
    }

    [Rule("rr:interrupt.3")]
    [Fact]
    public void APlacementInsertedByAnInterruptRunsBeforeTheOuterOccurrenceResumes()
    {
        // Advancing by the occurrence that applied is load-bearing here. The
        // nested item is now at the front; advancing "current" would skip its
        // interrupt window and leave the outer item on Apply.
        var (world, facts, scheme, source) = Board(escalation: 3);
        var side = world.CreateCard("side", world.AreaOf(DeckType.SideSchemesArea));
        world.Agenda.Add(new PhaseStep(Steps.PlaceThreat, 1, 1));
        var cards = new Interrupter(source.ObjectId, _ =>
            Threat.Schedule(
                world, side, source, 1, ThreatCause.CardAbility, "test", player: 0));

        var asked = Sequence.Work(world, facts, cards, []);
        Sequence.Answer(
            world, facts, cards, asked!, Decision.Take(Interrupter.Handle), []);
        Assert.Null(Sequence.Work(world, facts, cards, []));

        Assert.Equal(1, side.Tokens["k_threat"]);
        Assert.Equal(3, scheme.Tokens["k_threat"]);
    }

    [Rule("rr:interrupt.3.1")]
    [Fact]
    public void AnIdlePlacementWaitsOnTheAgendaForItsWindows()
    {
        // An occurrence-tier ability can resolve outside a phase during
        // scenario setup. "Would" interrupts still happen while the threat is
        // imminent, so scheduling must not apply it synchronously.
        var (world, facts, scheme, source) = Board(escalation: 0);

        Threat.Schedule(
            world, scheme, source, 2, ThreatCause.CardAbility, "setup", player: 0);

        Assert.False(scheme.Tokens.ContainsKey("k_threat"));
        Assert.Equal(Steps.PlaceThreatEffect, world.Agenda.Current?.What);

        Sequence.Finish(world, facts, new NoCardAbilities(), []);

        Assert.Equal(2, scheme.Tokens["k_threat"]);
        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:triggering-condition.1")]
    [Fact]
    public void IdlePlacementsKeepBoardOrderAndSeparateOccurrences()
    {
        var (world, facts, scheme, source) = Board(escalation: 0);
        var first = world.CreateCard("side", world.AreaOf(DeckType.SideSchemesArea));
        var second = world.CreateCard("side", world.AreaOf(DeckType.SideSchemesArea));
        var events = new List<GameEvent>();

        Threat.Schedule(
            world, [first, second], source, 1, ThreatCause.CardAbility, "setup", player: 0);
        Sequence.Finish(world, facts, new NoCardAbilities(), events);

        Assert.Equal(
            [first.ObjectId, second.ObjectId],
            events.OfType<FieldSet>().Select(happened => happened.Card));
        Assert.Equal(1, first.Tokens["k_threat"]);
        Assert.Equal(1, second.Tokens["k_threat"]);
        Assert.False(scheme.Tokens.ContainsKey("k_threat"));
    }

    [Rule("rr:ability.6")]
    [Theory]
    [InlineData(AbilityType.Interrupt)]
    [InlineData(AbilityType.ForcedInterrupt)]
    public void SetupWindowsExcludePlayerCardAbilities(AbilityType type)
    {
        // "Player card abilities cannot resolve during game setup, unless
        // prefaced by a Setup timing trigger." The identity's interrupt is not
        // such a trigger, so the placement finishes without offering it.
        var (world, facts, scheme, source) = Board(escalation: 0);
        bool resolved = false;
        var cards = new Interrupter(
            world.Seats[0].IdentityCard.ObjectId, _ => resolved = true, type);

        Threat.Schedule(
            world, scheme, source, 2, ThreatCause.VillainPhase, "setup", player: 0);
        Sequence.Finish(
            world, facts, cards, [], WindowAbilityScope.EncounterCardsOnly);

        Assert.False(resolved);
        Assert.Equal(2, scheme.Tokens["k_threat"]);
    }

    [Rule("rr:ability.6")]
    [Fact]
    public void SetupWindowsStillResolveEncounterCardAbilities()
    {
        // The setup restriction names player cards. Encounter-card text still
        // resolves, including a forced interrupt that nobody chooses to use.
        var (world, facts, scheme, source) = Board(escalation: 0);
        bool resolved = false;
        var cards = new Interrupter(
            source.ObjectId,
            occurrence =>
            {
                resolved = true;
                occurrence.Threat!.Prevent(1);
            },
            AbilityType.ForcedInterrupt);

        Threat.Schedule(
            world, scheme, source, 2, ThreatCause.VillainPhase, "setup", player: 0);
        Sequence.Finish(
            world, facts, cards, [], WindowAbilityScope.EncounterCardsOnly);

        Assert.True(resolved);
        Assert.Equal(1, scheme.Tokens["k_threat"]);
    }

    private static (World World, Facts Facts, Card Scheme, Card Source) Board(
        int escalation, int schemeValue = 0)
    {
        var facts = new Facts(escalation, schemeValue);
        var world = new World(facts, 1);
        world.CreateSeat("p0");
        var identity = world.CreateCard("identity", world.Seats[0].Hero);
        world.Seats[0].IdentityCard = identity;
        var enemy = world.CreateCard("villain", world.AreaOf(DeckType.VillainArea));
        var scheme = world.CreateCard("scheme", world.AreaOf(DeckType.MainSchemesArea));
        return (world, facts, scheme, enemy);
    }

    private sealed class Interrupter(
        int card, Action<Occurrence> resolve, AbilityType type = AbilityType.Interrupt)
        : NoCardAbilities
    {
        public const int Handle = 91;

        public override IReadOnlyList<PendingAbility> Waiting(
            World world, Occurrence occurrence, WindowKind window) =>
            window == WindowKind.Interrupt
            && occurrence.Is(Steps.ThreatWouldBePlaced)
            && occurrence.Threat?.Cause == ThreatCause.VillainPhase
                ? [new PendingAbility(card, type, 0)]
                : [];

        public override Affordance Describe(World world, PendingAbility ability) =>
            new(Handle, "Interrupt", ability.Card, 0, "interrupt threat");

        public override IReadOnlyList<GameEvent> Resolve(
            World world, Occurrence occurrence, PendingAbility ability,
            IReadOnlyList<int> paying, IReadOnlyList<int> chosen)
        {
            resolve(occurrence);
            return [];
        }
    }

    private sealed class Facts(int escalation, int schemeValue) : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId switch
        {
            "identity" => CardKind.AlterEgo,
            "villain" => CardKind.EncounterVillain,
            "scheme" => CardKind.MainScheme,
            "side" => CardKind.EncounterSideScheme,
            _ => CardKind.Unknown,
        };

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) => faceId switch
        {
            "villain" => new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["SCH"] = schemeValue.ToString(),
            },
            "scheme" => new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["EscalationThreat"] = escalation.ToString(),
                ["TargetThreat"] = "99",
            },
            "side" => new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["TargetThreat"] = "99",
            },
            _ => new Dictionary<string, string>(StringComparer.Ordinal),
        };

        public long PrintedValue(
            string faceId, string attribute, int players, long fallback = 0) =>
            Attributes(faceId).TryGetValue(attribute, out string? value)
            && long.TryParse(value, out long parsed) ? parsed : fallback;
    }
}

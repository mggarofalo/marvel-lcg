using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class ActivationCompletionTests
{
    [Fact]
    public void AbilityContinuationAggregatesEveryActivationInAgendaData()
    {
        // The field names and structural path are engine save-format choices.
        // Nothing lives only in an AbilityRunner instance while the attacks wait.
        var agenda = new Agenda();
        int first = agenda.ThenActivation(
            new PhaseStep(Steps.Attack, 1, 2, Subject: 1, Seat: 0));
        int second = agenda.ThenActivation(
            new PhaseStep(Steps.Attack, 1, 2, Subject: 2, Seat: 0));
        agenda.AfterActivations([first, second], new PhaseStep(
            Steps.ResumeAbility, 1, 2, Subject: 9, Seat: 0,
            AbilityOrdinal: 3,
            AbilityPath: ["seq:1", "then:then"],
            AbilityActivationIds: [first, second],
            AbilityResults: new Dictionary<string, long> { ["earlier"] = 4 }));

        Assert.Equal(
            [Steps.Attack, Steps.Attack],
            agenda.Outstanding.Select(step => step.What));
        var waiting = agenda.ActivationWait(first);
        Assert.NotNull(waiting);
        Assert.Equal(3, waiting.Value.AbilityOrdinal);
        Assert.Equal(["seq:1", "then:then"], waiting.Value.AbilityPath);
        Assert.Equal(4, waiting.Value.AbilityResults!["earlier"]);

        // Agenda stores and replaces a continuation opaquely. Cards owns the
        // activation-result keys and decides whether an updated wait is done.
        agenda.ReplaceActivationWait(first, waiting.Value with
        { AbilityActivationIds = [second] });
        Assert.Null(agenda.ActivationWait(first));
        var resumed = agenda.TakeActivationWait(second);
        Assert.Equal(3, resumed.AbilityOrdinal);
        Assert.Equal(["seq:1", "then:then"], resumed.AbilityPath);
    }

    [Rule("rr:activation.8")]
    [Fact]
    public void AnActivationInitiatedDuringAnotherWaitsForItsCompletion()
    {
        // "The newly initiated activation resolves after the current
        // activation has finished resolving." An activation is several agenda
        // steps, so "after the current step" is observably too early.
        var agenda = new Agenda();
        agenda.Add(new PhaseStep(Steps.Attack, 1, 2, Subject: 1, Seat: 0));
        int first = agenda.Outstanding[0].ActivationId;
        agenda.Then(new PhaseStep(
            Steps.GiveBoostCard, 1, 1, Subject: 1, ActivationId: first));

        agenda.Advance();
        agenda.Advance();
        agenda.Advance();
        Assert.Equal(Steps.GiveBoostCard, agenda.Current!.Value.What);

        int second = agenda.ThenActivation(
            new PhaseStep(Steps.Attack, 1, 2, Subject: 2, Seat: 0));

        Assert.Equal(
            [Steps.GiveBoostCard, Steps.Attack],
            agenda.Outstanding.Select(step => step.What));

        agenda.Advance();
        agenda.Advance();
        agenda.Advance();
        Assert.Equal(Steps.CompleteAttackActivation, agenda.Current!.Value.What);
        Assert.Equal(first, agenda.Current.Value.ActivationId);

        agenda.Advance();
        agenda.Advance();
        agenda.Advance();
        Assert.Equal(Steps.Attack, agenda.Current!.Value.What);
        Assert.Equal(second, agenda.Current.Value.ActivationId);
    }

    [Rule("rr:activation.7")]
    [Rule("rr:attack-enemy-activation.step.5")]
    [Fact]
    public void AttackCompletionReportsItsIdentityAndActualDamage()
    {
        var (world, facts, enemy) = Board(attacking: true, value: 3);
        var abilities = new CompletionRecorder();
        world.Agenda.Add(new PhaseStep(
            Steps.Attack, 1, 2, Subject: enemy.ObjectId, Seat: 0));

        Sequence.Finish(world, facts, abilities, []);

        var result = Assert.Single(abilities.Results);
        Assert.True(result.Made);
        Assert.True(result.Attacking);
        Assert.Equal(3, result.DamageDealt);
        Assert.Equal(0, result.ThreatPlaced);
        Assert.Equal(enemy.ObjectId, result.Enemy);
        Assert.Equal(0, result.Player);
        Assert.True(result.Id >= 0);
        Assert.Null(world.FinishedActivation);
        Assert.Null(world.Activation);
    }

    [Rule("rr:activation.7")]
    [Rule("rr:activation.8.2")]
    [Fact]
    public void TheInitiatingEffectResumesAfterTheAttacksResponses()
    {
        // All abilities triggered by the activation resolve before a later
        // activation starts, and the initiating effect itself is not resolved
        // until the activation is complete. The completion callback therefore
        // sits beyond the attack's response window, not inside it.
        var (world, facts, enemy) = Board(attacking: true, value: 1);
        var abilities = new OrderedCompletionRecorder(enemy.ObjectId);
        world.Agenda.Add(new PhaseStep(
            Steps.Attack, 1, 2, Subject: enemy.ObjectId, Seat: 0));

        Sequence.Finish(world, facts, abilities, []);

        Assert.Equal(["response", "completion"], abilities.Order);
    }

    [Rule("rr:activation.7")]
    [Rule("rr:scheme-enemy-activation.step.3")]
    [Fact]
    public void SchemeCompletionReportsThreatAndTheOccurrenceRoles()
    {
        var (world, facts, enemy) = Board(attacking: false, value: 2);
        var abilities = new CompletionRecorder();
        world.Agenda.Add(new PhaseStep(
            Steps.Scheme, 1, 2, Subject: enemy.ObjectId, Seat: 0));

        // Reach the scheme's final step and inspect the occurrence before it
        // applies. Its actor is the scheming enemy and its target is the main
        // scheme, the same explicit relation attack and thwart occurrences use.
        while (world.Agenda.Current is { } step && step.What != Steps.SchemeThreat)
        {
            if (world.Agenda.Stage == Stage.Apply)
            {
                VillainPhase.Take(world, facts, abilities, step, []);
            }
            world.Agenda.Advance();
        }

        var occurrence = world.Agenda.Begin(world, facts);
        Assert.Equal(enemy.ObjectId, occurrence.Actor);
        Assert.Equal(
            world.TheCardIn(DeckType.MainSchemesArea)!.ObjectId,
            occurrence.Target);
        Assert.Equal(0, occurrence.Player);

        Sequence.Finish(world, facts, abilities, []);

        var result = Assert.Single(abilities.Results);
        Assert.True(result.Made);
        Assert.False(result.Attacking);
        Assert.Equal(0, result.DamageDealt);
        Assert.Equal(2, result.ThreatPlaced);
    }

    [Rule("rr:confuse-confused.1")]
    [Rule("rr:confuse-confused.7")]
    [Rule("rr:activation.7")]
    [Fact]
    public void ACancelledSchemeStillCompletesAndReportsThatNoneWasMade()
    {
        // Removal replaces the scheme activation, so "that character is not
        // considered to have [...] schemed." Completion still occurs but says
        // no scheme was made and no threat was placed.
        var (world, facts, enemy) = Board(attacking: false, value: 2);
        Statuses.Give(world, enemy, Statuses.Confused);
        var abilities = new CompletionRecorder();
        world.Agenda.Add(new PhaseStep(
            Steps.Scheme, 1, 2, Subject: enemy.ObjectId, Seat: 0));

        Sequence.Finish(world, facts, abilities, []);

        var result = Assert.Single(abilities.Results);
        Assert.False(result.Made);
        Assert.Equal(0, result.ThreatPlaced);
        Assert.Empty(Statuses.On(world, enemy, Statuses.Confused));
    }

    [Rule("rr:boost-boost-icon.4")]
    [Rule("rr:boost-boost-icon.6")]
    [Fact]
    public void AnAdditionalBoostCardJoinsTheNamedEnemysQueueImmediately()
    {
        var (world, _, enemy) = Board(attacking: true, value: 1);
        var top = world.CreateCard("boost", world.AreaOf(DeckType.EncounterDeck));

        Attack.GiveAdditionalBoostCard(world, enemy, "additional boost", []);

        var waiting = world.AreaOf(
            DeckType.BoostCardsDeck, enemy.Area.PlayArea, host: enemy.ObjectId);
        Assert.Equal(top.ObjectId, Assert.Single(waiting.Cards).ObjectId);
        Assert.False(top.FaceUp);
    }

    private static (World World, Facts Facts, Card Enemy) Board(bool attacking, int value)
    {
        var facts = new Facts()
            .With("identity", ("HP", "10"), ("DEF", "0"))
            .With("enemy", attacking ? ("ATK", value.ToString()) : ("SCH", value.ToString()))
            .With("scheme", ("TargetThreat", "99"));
        facts.Kinds["identity"] = attacking ? CardKind.Hero : CardKind.AlterEgo;
        facts.Kinds["enemy"] = CardKind.Minion;
        facts.Kinds["scheme"] = CardKind.MainScheme;

        var world = new World(facts, players: 1);
        world.CreateSeat("p0");
        var identity = world.CreateCard("identity", world.Seats[0].Hero);
        world.Seats[0].IdentityCard = identity;
        if (attacking)
        {
            // No defense question: an exhausted hero cannot exhaust to defend.
            identity.Exhaust();
        }
        world.CreateCard("scheme", world.AreaOf(DeckType.MainSchemesArea));
        var enemy = world.CreateCard(
            "enemy", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        return (world, facts, enemy);
    }

    private sealed class CompletionRecorder : NoCardAbilities
    {
        public List<EnemyActivation> Results { get; } = [];

        public override IReadOnlyList<GameEvent> ActivationCompleted(
            World world, EnemyActivation result)
        {
            Results.Add(result);
            return [];
        }
    }

    private sealed class OrderedCompletionRecorder(int card) : NoCardAbilities
    {
        public List<string> Order { get; } = [];

        public override IReadOnlyList<PendingAbility> Waiting(
            World world, Occurrence occurrence, WindowKind window) =>
            window == WindowKind.Response
            && occurrence.Is(Steps.AttackEnds)
            && !Order.Contains("response", StringComparer.Ordinal)
                ? [new PendingAbility(card, AbilityType.ForcedResponse, 0)]
                : [];

        public override IReadOnlyList<GameEvent> Resolve(
            World world, Occurrence occurrence, PendingAbility ability,
            IReadOnlyList<int> paying, IReadOnlyList<int> chosen)
        {
            Order.Add("response");
            return [];
        }

        public override IReadOnlyList<GameEvent> ActivationCompleted(
            World world, EnemyActivation result)
        {
            Order.Add("completion");
            return [];
        }
    }

    private sealed class Facts : ICardFacts
    {
        private readonly Dictionary<string, Dictionary<string, string>> attributes =
            new(StringComparer.Ordinal);

        public Dictionary<string, CardKind> Kinds { get; } = new(StringComparer.Ordinal);

        public Facts With(string faceId, params (string Key, string Value)[] values)
        {
            var table = attributes.TryGetValue(faceId, out var found)
                ? found
                : attributes[faceId] = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (key, value) in values)
            {
                table[key] = value;
            }
            return this;
        }

        public CardKind Kind(string faceId) =>
            Kinds.TryGetValue(faceId, out var kind) ? kind : CardKind.Unknown;

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            attributes.TryGetValue(faceId, out var found)
                ? found
                : new Dictionary<string, string>(StringComparer.Ordinal);

        public long PrintedValue(
            string faceId, string attribute, int players, long fallback = 0) =>
            Attributes(faceId).TryGetValue(attribute, out string? printed)
            && long.TryParse(printed, out long value)
                ? value
                : fallback;
    }
}

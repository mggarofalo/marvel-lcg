using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class CardEntryExecutionContractTests
{
    [Theory]
    [InlineData("setup")]
    [InlineData("entry")]
    [InlineData("reveal")]
    [InlineData("boost")]
    [InlineData("activation")]
    [InlineData("special")]
    [InlineData("wouldDefeat")]
    [InlineData("defeat")]
    [InlineData("damageReplacement")]
    public void EmptyEventsCannotCompleteAnyEntryBeforeItsChildChoice(string entry)
    {
        // Engine execution contract: each public entry admits ownership before
        // the implementation schedules its first nested procedure or cost.
        var world = new World(new Printed(), 1);
        var card = world.CreateCard("synthetic", world.AreaOf(
            DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        world.Agenda.Add(new PhaseStep(new LifecycleOperation(LifecycleKind.CardPlayed), 1, 1));
        var owner = world.Agenda.Current!.Value;
        var occurrence = world.Agenda.Occurrence!;
        var abilities = new Entries();

        var result = Invoke(entry, abilities, world, card);

        Assert.Empty(result.Events);
        Assert.False(result.IsComplete);
        Assert.Throws<InvalidOperationException>(() => world.Agenda.Advance(owner, occurrence));
        Assert.True(world.Agenda.IsOutstanding(owner, occurrence));
        var child = Assert.IsType<AbilityExecution>(abilities.Child);
        world.Agenda.Executions.Resume(child);
        child.Finish([]);
        Assert.True(child.IsComplete);
        Assert.True(result.IsComplete);
        Assert.True(world.Agenda.Advance(owner, occurrence));
        Assert.Throws<InvalidOperationException>(() => world.Agenda.Executions.Resume(child));
    }

    private static AbilityExecutionResult Invoke(string entry, Entries abilities, World world, Card card) => entry switch
    {
        "setup" => abilities.ExecuteSetup(world, card),
        "entry" => abilities.ExecuteEntry(world, card),
        "reveal" => abilities.ExecuteReveal(world, card, 0),
        "boost" => abilities.ExecuteBoost(world, card, 0),
        "activation" => abilities.CompleteActivation(world, new EnemyActivation(card.ObjectId, 0, true)),
        "special" => abilities.ExecuteSpecial(world, card, 0, true),
        "wouldDefeat" => abilities.ExecuteWouldDefeat(world, card, new DefeatCause()),
        "defeat" => abilities.ExecuteDefeat(world, card, new Defeated(card.ObjectId, -1, "synthetic") { Kind = CardKind.Ally }, "synthetic"),
        "damageReplacement" => Replace(abilities, world, card),
        _ => throw new ArgumentOutOfRangeException(nameof(entry)),
    };

    private static AbilityExecutionResult Replace(Entries abilities, World world, Card card)
    {
        world.Abilities = abilities;
        return DamagePlacement.Replace(world, new(card, card, 1, "synthetic", "Damage"), []).Execution.Report([]);
    }

    private sealed class Entries : NoCardAbilities
    {
        internal AbilityExecution? Child { get; private set; }

        private AbilityExecutionResult Run(World world, AbilityExecution entry)
        {
            world.Agenda.Executions.RequireBatch(entry);
            Child = world.Agenda.Executions.Begin(entry.Occurrence, new(7, AbilityType.ForcedInterrupt, 0));
            world.Agenda.ThenContinuation(new PhaseStep(
                new AbilityContinuationAgendaOperation(AbilityContinuationStage.Choose)
                { Continuation = new Continuation { Execution = Child } }, 1, 2), entry.Occurrence);
            Child.Yield([]);
            return entry.Finish([]);
        }

        public override AbilityExecutionResult WouldBeDealt(World world, AbilityExecution entry, DamageReplacement replacement) => Run(world, entry);

        public override AbilityExecutionResult Setup(World world, AbilityExecution entry, Card card) => Run(world, entry);
        public override AbilityExecutionResult EntersPlay(World world, AbilityExecution entry, Card card) => Run(world, entry);
        public override AbilityExecutionResult WhenRevealed(World world, AbilityExecution entry, Card card, int player) => Run(world, entry);
        public override AbilityExecutionResult Boost(World world, AbilityExecution entry, Card card, int player) => Run(world, entry);
        public override AbilityExecutionResult ActivationCompleted(World world, AbilityExecution entry, EnemyActivation result) => Run(world, entry);
        public override AbilityExecutionResult ResolveSpecial(World world, AbilityExecution entry, Card card, int player, bool finalStep) => Run(world, entry);
        public override AbilityExecutionResult WouldBeDefeated(World world, AbilityExecution entry, Card target, DefeatCause cause) => Run(world, entry);
        public override AbilityExecutionResult WhenCardDefeated(World world, AbilityExecution entry, Card card, Defeated defeated, string trigger) => Run(world, entry);
    }

    private sealed record Continuation : CardAbilityContinuation;

    private sealed class Printed : ICardFacts
    {
        public CardKind Kind(string faceId) => CardKind.Ally;
        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) => new Dictionary<string, string>();
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) => fallback;
    }
}

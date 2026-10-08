using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class NextMinionActivationTests : VillainPhaseTestBase
{
    [Rule("rr:minion.3")]
    [Rule("rr:minion.4")]
    [Rule("rr:activation.7")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EachChoiceSeesTheBoardAfterThePreviousMinionFinished(bool samePhysicalCard)
    {
        // rr:minion.3 resolves "one minion at a time" in the player's chosen
        // order; .4 includes newly engaged minions. rr:activation.7 resolves
        // triggered abilities before continuing to the next activation.
        var printed = new Printed().With("villain", ("SCH", "0"))
            .With("minion", ("SCH", "1")).With("scheme", ("EscalationThreat", "0"))
            .With("boost", ("Boost", "0"));
        World world = Board(printed, 1);
        var area = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
        Card first = world.CreateCard("minion", area);
        Card removed = world.CreateCard("minion", area);
        Card remaining = world.CreateCard("minion", area);
        var abilities = new ChangeAfterActivation(first.ObjectId, removed.ObjectId, samePhysicalCard);
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(Steps.EnemiesActivate, 1, 2, Plan: true));
        Prompt prompt = Sequence.Work(world, printed, abilities, events)!;
        Assert.Equal(3, prompt.Affordances.Count);
        Sequence.Answer(world, printed, abilities, prompt, Decision.Take(first.ObjectId), events);
        Prompt next = Sequence.Work(world, printed, abilities, events)!;
        Assert.Equal([world.TheCardIn(DeckType.VillainArea)!.ObjectId, first.ObjectId], abilities.Completed);
        Assert.Equal(1, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens["k_threat"]);
        Assert.Equal(new[] { remaining.ObjectId, abilities.Arriving }.Order(), next.Affordances.Select(offer => offer.AnchorId));
        Assert.All(next.Affordances, offer => Assert.Null(offer.Targets));
        Sequence.Answer(world, printed, abilities, next, Decision.Take(abilities.Arriving), events);
        Assert.Null(Sequence.Work(world, printed, abilities, events));
        Assert.Equal([world.TheCardIn(DeckType.VillainArea)!.ObjectId,
            first.ObjectId, abilities.Arriving, remaining.ObjectId], abilities.Completed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InvalidNextMinionAnswersDoNotActivateAnything(bool invented)
    {
        var printed = new Printed().With("villain", ("SCH", "0"))
            .With("minion", ("SCH", "1")).With("scheme", ("EscalationThreat", "0"))
            .With("boost", ("Boost", "0"));
        World world = Board(printed, 1);
        var area = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
        Card first = world.CreateCard("minion", area);
        Card second = world.CreateCard("minion", area);
        var observer = new EnemyOrderObserver();
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(Steps.EnemiesActivate, 1, 2, Plan: true));
        Prompt prompt = Sequence.Work(world, printed, observer, events)!;
        Decision answer = invented ? Decision.Take(999)
            : Decision.Take(first.ObjectId, [second.ObjectId], []);
        Assert.Throws<RulesNotImplementedException>(() =>
            Sequence.Answer(world, printed, observer, prompt, answer, events));
        Assert.Equal(["villain"], observer.Enemies);
    }

    private sealed class ChangeAfterActivation(int selected, int departing, bool samePhysicalCard) : NoCardAbilities
    {
        public List<int> Completed { get; } = [];
        public int Arriving { get; private set; }

        public override IReadOnlyList<GameEvent> ActivationCompleted(World world, EnemyActivation result)
        {
            Completed.Add(result.Enemy);
            if (result.Enemy == selected && Completed.Count(id => id == selected) == 1)
            {
                World.MoveToTop(world.Cards[departing], world.AreaOf(DeckType.EncounterDiscardPile));
                var engaged = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
                if (samePhysicalCard)
                {
                    World.MoveToTop(world.Cards[selected], world.AreaOf(DeckType.EncounterDiscardPile));
                    World.MoveToTop(world.Cards[selected], engaged);
                    Arriving = selected;
                }
                else Arriving = world.CreateCard("minion", engaged).ObjectId;
            }
            return [];
        }
    }
}

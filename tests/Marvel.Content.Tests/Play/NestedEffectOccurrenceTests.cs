using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class NestedEffectOccurrenceTests
{
    [Rule("rr:triggering-condition.2")]
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DirectEffectsRetainTheirCauseWhenAnotherProcedureIsCurrent(bool damage)
    {
        // Component contract: damage and defeat from a "single game occurrence"
        // share its response window, even if a nested procedure is current.
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(cards, 1);
        var seat = world.CreateSeat("She-Hulk");
        seat.IdentityCard = world.CreateCard("01019a", seat.Hero);
        var source = world.CreateCard("01022", seat.Hand);
        var target = damage
            ? world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, new PlayArea(0)))
            : world.CreateCard("01109", world.AreaOf(DeckType.SideSchemesArea));
        if (damage) target.TakeDamage(2);
        else target.PlaceTokens("k_threat", 1);
        world.Agenda.Add(new PhaseStep(new LifecycleOperation(LifecycleKind.CardPlayed), 1, 1));
        var cause = world.Agenda.Occurrence!;
        CardEffectWindows.Begin(world.Agenda, source.ObjectId, 0);
        var nested = world.Agenda.Occurrence!;

        if (damage)
        {
            var result = DamagePlacement.Resolve(world, cards,
                new DamageRequest(source, target, 1, "component", "Deal_Damage", 0)
                { RecordDefeatOn = cause }, []);
            Assert.Equal(Damage.Outcome.Defeated, result.Outcome);
        }
        else
        {
            Assert.Equal(1, Threat.Remove(world, cards, world.ThreatAbilities,
                new ThreatRemovalRequest(target, 1, "component", "Remove_Threat", 0)
                { RecordDefeatOn = cause }, []));
        }
        Assert.Equal(target.ObjectId, Assert.Single(cause.Defeats).Card);
        Assert.Empty(nested.Defeats);
        Assert.Equal(DeckType.EncounterDiscardPile, target.Area.Type);
    }

    [Rule("rr:triggering-condition.2")]
    [Rule("rr:response.3")]
    [Rule("rr:you-your.18")]
    [Fact]
    public void InterrogationRoomDefeatingASchemeDoesNotChangeTheAttackThatTriggeredIt()
    {
        // Component timing contract with printed Core cards. Conditions from
        // "a single game occurrence" share a window; a response effect creates
        // its own occurrence. Responses to conditions from a "single effect"
        // may be resolved "in any order" (rr:response.3).
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var runner = AuthoredCards.Runner();
        var world = new World(cards, 1) { Abilities = runner };
        var seat = world.CreateSeat("Spider-Man");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var minion = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, new PlayArea(0)));
        minion.TakeDamage(2);
        var room = world.CreateCard("01063", world.AreaOf(DeckType.SupportsArea, new PlayArea(0), cardOwner: 0));
        var scheme = world.CreateCard("01109", world.AreaOf(DeckType.SideSchemesArea));
        scheme.PlaceTokens("k_threat", 1);
        var events = new List<GameEvent>();

        BasicPowerInitiation.BasicAttack(world, cards, 0, minion, events);
        var response = Assert.IsType<Prompt>(Sequence.Work(world, cards, runner, events));
        var attack = world.Agenda.Occurrence!;
        Assert.Equal(minion.ObjectId, Assert.Single(attack.Defeats).Card);
        var offered = Assert.Single(response.Affordances, option => option.AnchorId == room.ObjectId);
        Sequence.Answer(world, cards, runner, response, Decision.Take(offered.Id), events);
        var choice = Assert.IsType<Prompt>(Sequence.Work(world, cards, runner, events));
        var target = Assert.Single(choice.Affordances, option => option.Id == scheme.ObjectId);
        Sequence.Answer(world, cards, runner, choice, Decision.Take(target.Id), events);

        var child = world.Agenda.Occurrence!;
        Assert.NotSame(attack, child);
        Assert.Equal(scheme.ObjectId, Assert.Single(child.Defeats).Card);
        // A support's abilities are "not considered to be performed by that
        // player's identity" (rr:you-your.18).
        Assert.Equal(-1, Assert.Single(child.Defeats).By);
        Assert.Equal(minion.ObjectId, Assert.Single(attack.Defeats).Card);
        Assert.Null(Sequence.Work(world, cards, runner, events));
        Assert.Equal(DeckType.EncounterDiscardPile, scheme.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.False(room.Ready);
        Assert.False(world.Agenda.IsBusy);
    }
}

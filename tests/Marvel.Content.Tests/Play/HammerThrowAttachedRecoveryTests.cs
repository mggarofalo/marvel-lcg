using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HammerThrowAttachedRecoveryTests
{
    [Theory]
    [InlineData("01186", true)]
    [InlineData("01101", true)]
    [InlineData("01186", false)]
    [InlineData("01101", false)]
    [Rule("rr:forced.5")]
    [Rule("rr:forced.6")]
    [Rule("rr:overkill.1")]
    public void FirstPlayerOrdersActualLokiAndBiomechanicalRecovery(string top, bool lokiFirst)
    {
        var control = ResolveRecovery(top, lokiFirst, probe: false);
        var probed = ResolveRecovery(top, lokiFirst, probe: true);
        Assert.Equal(control.World.Digest().Canonical(), probed.World.Digest().Canonical());
        Assert.Equal(control.Events, probed.Events);
        Assert.Equal(control.World.Random.Generator.GetState().Words, probed.World.Random.Generator.GetState().Words);
        Assert.Equal(control.World.Random.Generator.GetState().Index, probed.World.Random.Generator.GetState().Index);
        Assert.Equal(control.World.Random.Generator.WordsConsumed, probed.World.Random.Generator.WordsConsumed);
    }

    private static (World World, string[] Events) ResolveRecovery(string top, bool lokiFirst, bool probe)
    {
        // Printed 01185: "heal all damage from it instead, then discard this card."
        // rr:forced.5 gives simultaneous forced ordering to the first player;
        // rr:forced.6 resolves each completely before the next initiates.
        // rr:overkill.1: "If this attack defeats a minion" requires actual defeat.
        // Actual cards in a research board; Thor public admission remains closed.
        var world = ThorActualFixture.Board("06001a", players: 2);
        world.FirstPlayer = 1;
        var runner = HammerThrowRecoveryPreflightTests.Runner();
        world.Abilities = runner;
        var loki = world.CreateCard("06028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        int incarnation = loki.Incarnation;
        var attachment = world.CreateCard("01185", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), loki.ObjectId));
        var hammer = world.CreateCard("06009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var hall = world.CreateCard("06017", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var discarded = world.CreateCard(top, world.AreaOf(DeckType.EncounterDeck));
        var events = new List<GameEvent>();
        var target = Start(world, runner, probe, events);
        Sequence.Answer(world, world.Facts, runner, target, Decision.Take(loki.ObjectId), events);
        var order = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(Question.Order, order.Asking);
        Assert.Equal(1, order.Player);
        Assert.False(order.Cancellable);
        Assert.Equal(2, order.Affordances.Count);
        Assert.Equal(8, loki.Damage);
        Assert.Equal(DeckType.UpgradesArea, hammer.Area.Type);
        Assert.Equal(DeckType.EncounterDeck, discarded.Area.Type);
        var selected = Assert.Single(order.Affordances,
            option => option.AnchorId == (lokiFirst ? loki.ObjectId : attachment.ObjectId));
        Sequence.Answer(world, world.Facts, runner, order, Decision.Take(selected.Id), events);
        Assert.Null(Sequence.Work(world, world.Facts, runner, events));
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(DeckType.EngagedEnemiesArea, loki.Area.Type);
        Assert.Equal(incarnation, loki.Incarnation);
        Assert.Equal(0, loki.Damage);
        Assert.Equal(lokiFirst && top == "01186" ? DeckType.UpgradesArea : DeckType.EncounterDiscardPile,
            attachment.Area.Type);
        if (lokiFirst && top == "01186") Assert.Equal(loki.ObjectId, attachment.Area.Host);
        Assert.Equal(lokiFirst ? DeckType.EncounterDiscardPile : DeckType.EncounterDeck, discarded.Area.Type);
        Assert.Same(world.Seats[0].Hand, hammer.Area);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(0, hall.Tokens.GetValueOrDefault("c_glory"));
        Assert.Empty(world.Agenda.Outstanding);
        return (world, events.Select(item => System.Text.Json.JsonSerializer.Serialize(item, item.GetType())).ToArray());
    }

    [Theory]
    [InlineData("04117", "01186")]
    [InlineData("04117", "01101")]
    [InlineData("04118", "01186")]
    [InlineData("04118", "01101")]
    [InlineData("04119", "01186")]
    [InlineData("04119", "01101")]
    [Rule("rr:overkill.1")]
    public void ActualZolaHealthAttachmentsSurviveRecoveryAndLeaveOnlyOnDefeat(string face, string top)
    {
        // Printed 04117/04118/04119 each grant the attached minion +2 hit points.
        // rr:overkill.1: "If this attack defeats a minion" then spill excess damage.
        var world = ThorActualFixture.Board("06001a");
        var runner = HammerThrowRecoveryPreflightTests.Runner();
        world.Abilities = runner;
        var loki = world.CreateCard("06028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var attachment = world.CreateCard(face, world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), loki.ObjectId));
        var hammer = world.CreateCard("06009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard(top, world.AreaOf(DeckType.EncounterDeck));
        Assert.Equal(6, DamagePlacement.Health(world, world.Facts, loki));
        var target = Start(world, runner);
        Sequence.Answer(world, world.Facts, runner, target, Decision.Take(loki.ObjectId), []);
        Sequence.Finish(world, world.Facts, runner, []);
        bool recovered = top == "01186";
        Assert.Equal(recovered ? DeckType.EngagedEnemiesArea : DeckType.EncounterDiscardPile, loki.Area.Type);
        Assert.Equal(recovered ? DeckType.UpgradesArea : DeckType.EncounterDiscardPile, attachment.Area.Type);
        Assert.Equal(recovered ? 0 : 2, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(face == "04118" && recovered ? 1 : 0, world.Seats[0].IdentityCard.Damage);
        Assert.Same(world.Seats[0].Hand, hammer.Area);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData("{\"seq\":[{\"heal\":{\"card\":\"attachedTo\",\"amount\":1}},{\"discard\":\"this\"}]}")]
    [InlineData("{\"seq\":[{\"heal\":{\"card\":\"attachedTo\",\"amount\":{\"damageOn\":\"attachedTo\"}}},{\"discard\":\"attachedTo\"}]}")]
    public void UnboundedAttachedRecoveryStillRefusesAtomically(string recovery)
    {
        // Synthetic bodies must not inherit the actual full-heal/self-discard permission.
        var world = ThorActualFixture.Board("06001a");
        var runner = HammerThrowRecoveryPreflightTests.Runner(recovery: recovery);
        world.Abilities = runner;
        var loki = world.CreateCard("06028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard("01185", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), loki.ObjectId));
        world.CreateCard("06009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var target = Start(world, runner);
        var before = world.Digest().Canonical();
        var rng = world.Random.Generator.GetState();
        var agenda = world.Agenda.Outstanding.ToArray();
        var error = Assert.Throws<RulesNotImplementedException>(() =>
            Sequence.Answer(world, world.Facts, runner, target, Decision.Take(loki.ObjectId), []));
        Assert.Contains("step-6 interrupt", error.Message);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(rng.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(rng.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(agenda, world.Agenda.Outstanding.ToArray());
    }

    private static Prompt Start(World world, AbilityRunner runner, bool probe = true, List<GameEvent>? events = null)
    {
        var card = world.CreateCard("06005", world.Seats[0].Hand);
        var first = world.CreateCard("01090", world.Seats[0].Hand);
        var second = world.CreateCard("01003", world.Seats[0].Hand);
        var before = world.Digest().Canonical();
        var rng = world.Random.Generator.GetState();
        var agenda = world.Agenda.Outstanding.ToArray();
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == card.ObjectId);
        if (probe)
            Assert.Equal(action, Assert.Single(runner.Actions(world, 0), a => a.Card == card.ObjectId));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(rng.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(rng.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(agenda, world.Agenda.Outstanding.ToArray());
        runner.ExecuteAction(world, new PlayerAction(action, [first.ObjectId, second.ObjectId], []));
        return Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events ?? []));
    }
}

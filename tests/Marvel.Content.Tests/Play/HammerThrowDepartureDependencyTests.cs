using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HammerThrowDepartureDependencyTests
{
    [Theory]
    [InlineData(12, false)]
    [InlineData(16, true)]
    [Rule("rr:hit-points.2.3")]
    public void ReturnedTraitGrantSettlesConditionalHealthLoss(int damage, bool eliminated)
    {
        // Synthetic health condition challenges departure dependency analysis;
        // printed Thor's Helmet itself grants an unconditional five hit points.
        // rr:hit-points.2.3: "If that ability later ceases to be in effect,
        // reduce that character's hit point dial by X."
        var runner = HammerThrowRecoveryPreflightTests.Runner(helmet: """
            {"if":{"test":{"hasTrait":{"card":"you","trait":"AERIAL"}},
            "then":{"grant":{"card":"you","keyword":"health","amount":5}}}}
            """);
        var world = ThorActualFixture.Board("06001a", players: 2);
        world.Abilities = runner;
        var hero = world.Seats[0].IdentityCard;
        var loki = world.CreateCard("06028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var hammer = world.CreateCard("06009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("06010", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        hero.TakeDamage(damage);
        world.CreateCard("01186", world.AreaOf(DeckType.EncounterDeck));
        Assert.Equal(19, DamagePlacement.Health(world, world.Facts, hero));
        var card = world.CreateCard("06005", world.Seats[0].Hand);
        var first = world.CreateCard("01090", world.Seats[0].Hand);
        var second = world.CreateCard("01003", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == card.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [first.ObjectId, second.ObjectId], []));
        var target = Sequence.Work(world, world.Facts, runner, [])!;
        Sequence.Answer(world, world.Facts, runner, target, Decision.Take(loki.ObjectId), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(eliminated, world.Seats[0].Eliminated);
        Assert.Equal(eliminated ? DeckType.RemovedArea : DeckType.HandsArea, hammer.Area.Type);
        Assert.Equal(0, loki.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, loki.Area.Type);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Empty(world.Agenda.Outstanding);
    }
}

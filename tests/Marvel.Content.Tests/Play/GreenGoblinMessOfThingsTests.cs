using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class GreenGoblinMessOfThingsTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:activation.7")]
    [Fact]
    public void TailSweepStunsThePlayerWhenScorpionIsAbsent()
    {
        // "If no attack was made this way, you are stunned." The missing minion
        // cannot be replaced by the villain or another engaged minion.
        var world = Board("01001a");
        world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var card = world.CreateCard("02040", world.AreaOf(DeckType.RevealingArea));
        var runner = AuthoredCards.Runner();

        runner.ExecuteReveal(world, card, 0);
        Finish(world, runner);

        Assert.True(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:activation.7")]
    [Fact]
    public void TailSweepDoesNotStunWhenScorpionActuallyAttacks()
    {
        var world = Board("01001a");
        world.CreateCard("02038", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Tough);
        var card = world.CreateCard("02040", world.AreaOf(DeckType.RevealingArea));
        var runner = AuthoredCards.Runner();

        runner.ExecuteReveal(world, card, 0);
        Finish(world, runner);

        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Tough));
    }

    [Rule("rr:attack-enemy-activation.step.6")]
    [Fact]
    public void ScorpionStunsOnlyTheCharacterItDamages()
    {
        // Scorpion's forced response requires attack damage to the character.
        // An unprevented attack therefore stuns its damaged target.
        var world = Board("01001a");
        world.CreateCard("02038", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        var card = world.CreateCard("02040", world.AreaOf(DeckType.RevealingArea));
        var runner = AuthoredCards.Runner();

        runner.ExecuteReveal(world, card, 0);
        Finish(world, runner);

        Assert.True(world.Seats[0].IdentityCard.Damage > 0);
        Assert.True(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
    }

    [Rule("rr:boost-boost-icon.2")]
    [Fact]
    public void TailSweepBoostStunsWithoutDependingOnDamage()
    {
        var world = Board("01001a");
        var card = world.CreateCard("02040", world.AreaOf(DeckType.BoostingArea));

        AuthoredCards.Runner().ExecuteBoost(world, card, 0);

        Assert.True(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:surge.1")]
    [Fact]
    public void GangUpSurgesInAlterEgoInsteadOfSchedulingAttacks()
    {
        var world = Board("01001b");
        world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        var card = world.CreateCard("02039", world.AreaOf(DeckType.RevealingArea));
        var runner = AuthoredCards.Runner();

        runner.ExecuteReveal(world, card, 0);
        Finish(world, runner);

        Assert.Single(world.AreaOf(
            DeckType.DealtEncounterCardsDeck, PlayArea.Of(0), cardOwner: 0).Cards);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:attack-enemy-activation.1")]
    [Rule("rr:attack-enemy-activation.step.1")]
    [Fact]
    public void GangUpMakesVillainAndEngagedMinionAttackSeparately()
    {
        // "If a minion without the villainous keyword is attacking, skip this step."
        // Rhino's 2 ATK plus one boost icon and Hydra Mercenary's 1 ATK deal 4.
        var world = Board("01001a");
        world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        var card = world.CreateCard("02039", world.AreaOf(DeckType.RevealingArea));
        var runner = AuthoredCards.Runner();

        runner.ExecuteReveal(world, card, 0);
        Finish(world, runner);

        Assert.Equal(4, world.Seats[0].IdentityCard.Damage);
        Assert.Single(world.AreaOf(DeckType.EncounterDeck).Cards);
    }

    private static World Board(string identity)
    {
        var world = new World(Cards, players: 1);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard(identity, seat.Hero);
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        return world;
    }

    private static void Finish(World world, ICardAbilities runner)
    {
        var events = new List<GameEvent>();
        int answers = 0;
        while (Sequence.Work(world, Cards, runner, events) is { } prompt)
        {
            Assert.True(++answers <= 8, "A single reveal must finish after bounded optional windows.");
            Assert.True(prompt.Cancellable, prompt.Label);
            Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, events);
        }
    }
}

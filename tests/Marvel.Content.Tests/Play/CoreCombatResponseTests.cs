using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CoreCombatResponseTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:attack-player-ability-type.step.7")]
    [Fact]
    public void SuperhumanStrengthUsesTheCompletedAttacksTarget()
    {
        var world = Board("01019a");
        var hero = world.Seats[0].IdentityCard;
        var strength = world.CreateCard(
            "01028", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var minion = Minion(world);
        var runner = AuthoredCards.Runner();
        var occurrence = Occurrence.ForAttack(
            1, [Steps.AttackEnds], world, Cards, hero.ObjectId, minion.ObjectId, 0);

        var pending = Assert.Single(
            runner.Waiting(world, occurrence, WindowKind.Response),
            ability => ability.Card == strength.ObjectId);
        runner.Resolve(world, occurrence, pending, [], []);

        Assert.False(DeckTypes.IsInPlay(strength.Area.Type));
        Assert.True(Statuses.Has(world, minion, Statuses.Stunned));
    }

    [Rule("rr:triggering-condition.2")]
    [Rule("rr:consequential-damage.1")]
    [Fact]
    public void TigraRespondsOnlyWhenHerAttackDefeatedItsMinion()
    {
        var world = Board("01019a");
        var tigra = world.CreateCard(
            "01051", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        tigra.TakeDamage(1);
        var minion = Minion(world);
        var runner = AuthoredCards.Runner();
        var occurrence = Occurrence.ForAttack(
            1, [Steps.AttackEnds], world, Cards, tigra.ObjectId, minion.ObjectId, 0);

        Assert.Empty(runner.Waiting(world, occurrence, WindowKind.Response));

        occurrence.Also(new Defeated(minion.ObjectId, tigra.ObjectId, "Attack"));
        var pending = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Response));
        runner.Resolve(world, occurrence, pending, [], []);

        Assert.Equal(0, tigra.Damage);
    }

    [Rule("rr:thwart")]
    [Rule("rr:consequential-damage.1")]
    [Fact]
    public void DaredevilUsesTheThwarterActorRole()
    {
        var world = Board("01019a");
        var daredevil = world.CreateCard(
            "01058", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var scheme = world.CreateCard("01125", world.AreaOf(DeckType.SideSchemesArea));
        var minion = Minion(world);
        var runner = AuthoredCards.Runner();
        var occurrence = Occurrence.ForThwart(
            1, [Steps.CharacterThwartsScheme], world, Cards,
            daredevil.ObjectId, scheme.ObjectId, 0);

        var pending = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Response));
        runner.Resolve(world, occurrence, pending, [], []);
        var waiting = Assert.Single(world.Agenda.Outstanding);
        runner.Chose(
            world, daredevil, 0, waiting.Index, Decision.Take(minion.ObjectId), waiting.Tier);

        Assert.Equal(1, minion.Damage);
    }

    [Rule("rr:defend-defense.2")]
    [Fact]
    public void IndomitableRequiresABasicHeroDefense()
    {
        var world = Board("01001a");
        var hero = world.Seats[0].IdentityCard;
        hero.Exhaust();
        var indomitable = world.CreateCard(
            "01082", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var enemy = world.TheCardIn(DeckType.VillainArea)!;
        var runner = AuthoredCards.Runner();
        var occurrence = Occurrence.ForAttack(
            1, [Steps.AttackEnds], world, Cards, enemy.ObjectId, hero.ObjectId, 0);

        world.FinishedAttack = new EnemyAttack(
            enemy.ObjectId, 0, hero.ObjectId, Defender: hero.ObjectId, BasicDefense: false);
        Assert.Empty(runner.Waiting(world, occurrence, WindowKind.Response));

        world.FinishedAttack = world.FinishedAttack with { BasicDefense = true };
        var pending = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Response));
        runner.Resolve(world, occurrence, pending, [], []);

        Assert.True(hero.Ready);
        Assert.False(DeckTypes.IsInPlay(indomitable.Area.Type));
    }

    private static World Board(string heroFace)
    {
        var world = new World(Cards, players: 1);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard(heroFace, seat.Hero);
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        return world;
    }

    private static Card Minion(World world) => world.CreateCard(
        "01120", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
}

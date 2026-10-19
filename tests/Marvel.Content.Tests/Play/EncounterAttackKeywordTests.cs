using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;
using Marvel.Tests;

namespace Marvel.Content.Tests.Play;

public sealed class EncounterAttackKeywordTests
{
    private const string Book = """
        {"cards":[
          {"card":"04027","abilities":[
            {"trigger":{"timing":"Constant"},"effect":{"grant":{"card":"this","keyword":"piercing"}}},
            {"trigger":{"timing":"Boost","event":"WhenCardRevealed","subject":"this"},"effect":{"if":{"test":{"activationIs":"attack"},"then":{"grantUntil":{"card":"activatingEnemy","keyword":"piercing","amount":1,"until":"EndOfAttack"}}}}}]},
          {"card":"04029","abilities":[{"trigger":{"timing":"ForcedInterrupt","event":"WhenAttackInitiated","actor":"attachedTo"},"effect":{"grantUntil":{"card":"attachedTo","keyword":"ranged","amount":1,"until":"EndOfAttack"}}}]},
          {"card":"04072","abilities":[{"trigger":{"timing":"ForcedInterrupt","event":"WhenAttackInitiated","actor":"attachedTo"},"effect":{"grantUntil":{"card":"attachedTo","keyword":"ranged","amount":1,"until":"EndOfAttack"}}}]}
        ]}
        """;

    [Fact]
    public void CrossfireBoostPiercesOnlyTheCurrentAttack()
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse(Book));
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var hero = world.Seats[0].IdentityCard;
        world.CreateCard("04027", world.AreaOf(DeckType.EncounterDeck));
        Statuses.Give(world, hero, Statuses.Tough);
        Attack(world, runner, villain);
        long damage = hero.Damage;
        Assert.True(damage > 0);
        Assert.False(Keywords.Has(world, villain, Keywords.Piercing, world.Facts));
        world.CreateCard("04076", world.AreaOf(DeckType.EncounterDeck));
        Statuses.Give(world, hero, Statuses.Tough);
        Attack(world, runner, villain);
        Assert.Equal(damage, hero.Damage);
    }

    [Fact]
    public void CrossfireOwnAttacksRetainPiercing()
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse(Book));
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var crossfire = world.CreateCard("04027", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Tough);
        Attack(world, runner, crossfire);
        Assert.True(world.Seats[0].IdentityCard.Damage > 0);
        Assert.True(Keywords.Has(world, crossfire, Keywords.Piercing, world.Facts));
    }

    [Theory]
    [InlineData("04029")]
    [InlineData("04072")]
    public void AttachedRifleSuppressesRetaliateForItsAttackAndDoesNotLeak(string face)
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse(Book));
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var attachment = world.CreateCard(face, world.AreaOf(DeckType.UpgradesArea, villain.Area.PlayArea, villain.ObjectId));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "retaliate", 1,
            Affects: world.Seats[0].IdentityCard.ObjectId));
        world.CreateCard("04076", world.AreaOf(DeckType.EncounterDeck));
        Attack(world, runner, villain);
        Assert.Equal(0, villain.Damage);
        Assert.False(Keywords.Has(world, villain, Keywords.Ranged, world.Facts));
        Discard.Card(world, attachment, "test", []);
        world.CreateCard("04076", world.AreaOf(DeckType.EncounterDeck));
        Attack(world, runner, villain);
        Assert.Equal(1, villain.Damage);
    }

    private static void Attack(World world, AbilityRunner runner, Card enemy)
    {
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2, Subject: enemy.ObjectId, Seat: 0));
        Agendas.Finish(world, world.Facts, runner);
    }
}

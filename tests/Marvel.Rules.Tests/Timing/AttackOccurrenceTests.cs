using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Timing;

/// <summary>Actor and target roles on source-neutral attack occurrences.</summary>
public sealed class AttackOccurrenceTests
{
    [Rule("rr:attack-enemy-activation.1")]
    [Rule("rr:attack-player-ability-type.4")]
    [Fact]
    public void EveryCharacterClassificationCanBeAnActorOrTarget()
    {
        var (world, facts, hero, ally, villain, minion) = Board();

        var attacks = new[]
        {
            Occurrence.ForAttack(1, [Steps.AttackInitiated], world, facts,
                villain.ObjectId, hero.ObjectId, player: 0),
            Occurrence.ForAttack(2, [Steps.AttackInitiated], world, facts,
                minion.ObjectId, ally.ObjectId, player: 0),
            Occurrence.ForAttack(3, [Steps.AttackInitiated], world, facts,
                hero.ObjectId, minion.ObjectId),
            Occurrence.ForAttack(4, [Steps.AttackInitiated], world, facts,
                ally.ObjectId, villain.ObjectId),
        };

        Assert.True(attacks[0].ActorFacts!.IsVillain);
        Assert.True(attacks[1].ActorFacts!.IsMinion);
        Assert.True(attacks[2].ActorFacts!.IsHero);
        Assert.True(attacks[3].ActorFacts!.IsAlly);
        Assert.All(attacks[..2], attack => Assert.True(attack.ActorFacts!.IsEnemy));
        Assert.All(attacks[2..], attack => Assert.True(attack.ActorFacts!.IsFriendly));
        Assert.Equal(0, attacks[0].Player);
        Assert.Equal(-1, attacks[2].Player);

        world.CharacterAttack = new CharacterAttack(hero.ObjectId, minion.ObjectId, 0);
        var characterAttack = new PhaseStep(Steps.CharacterAttacks, 1, 2)
            .OccurrenceOf(world, facts);
        Assert.Equal(-1, characterAttack.Player);
        Assert.Equal(0, characterAttack.ActorFacts!.Controller);
    }

    [Rule("rr:friendly")]
    [Fact]
    public void FriendlyAndEnemyRelationshipsDoNotAssumeOpposingSides()
    {
        var (world, facts, hero, ally, villain, minion) = Board();

        var friendly = Occurrence.ForAttack(
            1, [Steps.AttackInitiated], world, facts, hero.ObjectId, ally.ObjectId);
        var enemy = Occurrence.ForAttack(
            2, [Steps.AttackInitiated], world, facts, villain.ObjectId, minion.ObjectId);

        Assert.True(friendly.ActorFacts!.IsFriendly);
        Assert.True(friendly.TargetFacts!.IsFriendly);
        Assert.True(enemy.ActorFacts!.IsEnemy);
        Assert.True(enemy.TargetFacts!.IsEnemy);
    }

    [Rule("rr:ownership-and-control.5")]
    [Fact]
    public void MovingAndChangingControlCannotRewriteAnOpenOccurrence()
    {
        var (world, facts, _, ally, villain, _) = Board();
        var occurrence = Occurrence.ForAttack(
            1, [Steps.AttackInitiated], world, facts, ally.ObjectId, villain.ObjectId);

        World.MoveToTop(
            ally,
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 0));
        World.MoveToTop(villain, world.AreaOf(DeckType.EncounterDiscardPile));

        Assert.Equal(1, ally.Area.PlayArea.Player);
        Assert.Equal(0, occurrence.ActorFacts!.Controller);
        Assert.Equal(0, occurrence.ActorFacts.Owner);
        Assert.True(occurrence.ActorFacts.IsAlly);
        Assert.True(occurrence.TargetFacts!.IsVillain);
        Assert.True(occurrence.TargetFacts.IsEnemy);
    }

    [Rule("rr:defend-defense.3.1")]
    [Fact]
    public void DamageCapturesTheTargetChosenByTheDefenderWindow()
    {
        var (world, facts, hero, ally, villain, _) = Board();
        world.Attack = new EnemyAttack(villain.ObjectId, 0, hero.ObjectId);
        world.Agenda.Add(new PhaseStep(
            Steps.DealAttackDamage, 1, 4, Subject: villain.ObjectId, Seat: 0));

        // The defender window runs after the damage step is scheduled and
        // before its interrupt window begins. An ally defending becomes the
        // attack's target character.
        world.Attack = world.Attack with { Target = ally.ObjectId };

        var occurrence = world.Agenda.Begin(world, facts);

        Assert.Equal(villain.ObjectId, occurrence.Actor);
        Assert.Equal(ally.ObjectId, occurrence.Target);
        Assert.True(occurrence.TargetFacts!.IsAlly);
    }

    [Rule("rr:attack-enemy-activation.step.6")]
    [Fact]
    public void AttackEndRetainsTheAttackerAndFinalTarget()
    {
        var (world, facts, _, ally, villain, _) = Board();
        world.Attack = new EnemyAttack(villain.ObjectId, 0, ally.ObjectId);
        world.Agenda.Add(new PhaseStep(
            Steps.EndAttack, 1, 6, Subject: villain.ObjectId, Seat: 0));

        var occurrence = world.Agenda.Begin(world, facts);

        Assert.Equal(villain.ObjectId, occurrence.Actor);
        Assert.Equal(ally.ObjectId, occurrence.Target);
        Assert.Equal(0, occurrence.Player);
        Assert.Equal(-1, occurrence.Subject);
    }

    private static (World World, Facts Facts, Card Hero, Card Ally, Card Villain, Card Minion)
        Board()
    {
        var facts = new Facts();
        var world = new World(facts, players: 2);
        world.CreateSeat("p0");
        world.CreateSeat("p1");
        var hero = world.CreateCard("hero", world.Seats[0].Hero);
        var ally = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var villain = world.CreateCard("villain", world.AreaOf(DeckType.VillainArea));
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        return (world, facts, hero, ally, villain, minion);
    }

    private sealed class Facts : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId switch
        {
            "hero" => CardKind.Hero,
            "ally" => CardKind.Ally,
            "villain" => CardKind.EncounterVillain,
            "minion" => CardKind.Minion,
            _ => CardKind.Unknown,
        };

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            new Dictionary<string, string>(StringComparer.Ordinal);

        public long PrintedValue(
            string faceId, string attribute, int players, long fallback = 0) => fallback;
    }
}

using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
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

    [Rule("rr:attack-player-ability-type")]
    [Rule("rr:draw-drawing-cards")]
    [Fact]
    public void PhotonicBlastsDamageAndEnergyDrawKeepTheTurnActionTrigger()
    {
        var world = Board("01010a");
        var blast = world.CreateCard("01013", world.Seats[0].Hand);
        var payment = world.CreateCard("01014", world.Seats[0].Hand);
        var drawn = world.CreateCard("01087", world.Seats[0].Deck);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;

        var action = runner.Actions(world, 0).Single(ability => ability.Card == blast.ObjectId);
        var events = runner.Act(world, action, [payment.ObjectId], []).ToList();
        var prompt = Sequence.Work(world, Cards, runner, events)!;
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(villain.ObjectId), events);
        Sequence.Finish(world, Cards, runner, events);

        var damage = Assert.Single(events.OfType<FieldSet>(), change =>
            change.Card == villain.ObjectId && change.Verb == BasicPowers.AttackVerb);
        var draw = Assert.Single(events.OfType<CardsMoved>(), moved =>
            moved.Cards.Any(card => card.Card == drawn.ObjectId));
        Assert.Equal(Steps.TurnAction, damage.Trigger);
        Assert.Equal(Steps.TurnAction, draw.Trigger);
    }

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

    [Rule("rr:attack-player-ability-type.step.7")]
    [Fact]
    public void AttackEventsExposeTheirHeroActorAndChosenTargetToResponses()
    {
        // "After She-Hulk attacks, ... stun the attacked enemy." The response
        // reads both roles from the attack occurrence made by Uppercut.
        var world = Board("01019a");
        var hero = world.Seats[0].IdentityCard;
        var strength = world.CreateCard(
            "01028", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var uppercut = world.CreateCard("01054", world.Seats[0].Hand);
        var genius = world.CreateCard("01089", world.Seats[0].Hand);
        var energy = world.CreateCard("01088", world.Seats[0].Hand);
        world.CreateCard("01087", world.Seats[0].Deck);
        var enemy = world.TheCardIn(DeckType.VillainArea)!;
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;

        var action = runner.Actions(world, 0).Single(ability => ability.Card == uppercut.ObjectId);
        runner.Act(world, action, [genius.ObjectId, energy.ObjectId], []);
        var target = Sequence.Work(world, Cards, runner, [])!;
        Sequence.Answer(world, Cards, runner, target, Decision.Take(enemy.ObjectId), []);

        Sequence.Finish(world, Cards, runner, []);

        Assert.Equal(5, enemy.Damage);
        Assert.Equal(0, hero.Damage);
        Assert.False(DeckTypes.IsInPlay(strength.Area.Type));
        Assert.True(Statuses.Has(world, enemy, Statuses.Stunned));
        Assert.Equal(DeckType.DiscardPile, uppercut.Area.Type);
    }

    [Rule("rr:ranged.1")]
    [Fact]
    public void ACardAttackWithRangedIgnoresRetaliate()
    {
        // "An attack with the ranged keyword ignores the retaliate keyword."
        // The keyword belongs to the attacking identity, not the event source.
        var world = Board("01019a,01019b");
        var hero = world.Seats[0].IdentityCard;
        hero.TurnTo("01019a");
        var uppercut = world.CreateCard("01054", world.Seats[0].Hand);
        var genius = world.CreateCard("01089", world.Seats[0].Hand);
        var energy = world.CreateCard("01088", world.Seats[0].Hand);
        world.CreateCard("01087", world.Seats[0].Deck);
        var modok = world.CreateCard(
            AuthoredCards.Modok,
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, Keywords.Ranged, Amount: 1,
            Card: hero.ObjectId, Affects: hero.ObjectId));

        var action = runner.Actions(world, 0).Single(ability => ability.Card == uppercut.ObjectId);
        runner.Act(world, action, [genius.ObjectId, energy.ObjectId], []);
        var target = Sequence.Work(world, Cards, runner, [])!;
        Sequence.Answer(world, Cards, runner, target, Decision.Take(modok.ObjectId), []);
        Sequence.Finish(world, Cards, runner, []);

        Assert.Equal(5, modok.Damage);
        Assert.Equal(0, hero.Damage);
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

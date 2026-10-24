using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HeroDefenseOccurrenceTests
{
    [Theory]
    [InlineData("hero")]
    [InlineData("enemy")]
    [InlineData("activation")]
    [Rule("rr:leaves-play.1")]
    public void CapturedDefenseCannotTransferToANewIncarnationOrActivation(string changed)
    {
        // A card that leaves play has "no memory of its previous state".
        // Binding the pending defense to its physical incarnations/activation is engine policy.
        var (world, hero, enemy) = Board();
        Assert.True(HeroDefenseTiming.Schedule(world, hero));
        var step = world.Agenda.Current!.Value;
        Assert.Null(step.ScheduledOccurrence);
        var occurrence = step.OccurrenceOf(world, world.Facts);
        Assert.True(occurrence.Is(HeroDefenseTiming.Defends));
        Assert.Equal(hero.ObjectId, occurrence.Actor);
        Assert.Equal(enemy.ObjectId, occurrence.Target);
        Assert.True(occurrence.ActorFacts!.IsHero);
        Assert.Equal(0, occurrence.Player);
        if (changed == "activation") world.Activation = world.Activation! with { Id = 18 };
        else
        {
            var card = changed == "hero" ? hero : enemy;
            var area = card.Area;
            World.MoveToTop(card, world.AreaOf(DeckType.AsideDeck));
            World.MoveToTop(card, area);
        }
        Assert.False(step.OccurrenceOf(world, world.Facts).Is(HeroDefenseTiming.Defends));
    }

    [Fact]
    [Rule("rr:triggering-condition.1")]
    public void RepeatedDefensesHaveSeparateIdsAndReadingTheirContextIsPure()
    {
        // Each interrupt may trigger "only once per occurrence of its triggering condition".
        var (world, hero, _) = Board();
        Assert.True(HeroDefenseTiming.Schedule(world, hero));
        var first = world.Agenda.Current!.Value;
        string digest = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();
        for (int query = 0; query < 4; query++)
        {
            var observed = first.OccurrenceOf(world, world.Facts);
            Assert.True(observed.Is(HeroDefenseTiming.Defends));
            Assert.Equal(first.OccurrenceId, observed.Id);
        }
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.True(HeroDefenseTiming.Schedule(world, hero));
        Assert.Equal(2, world.Agenda.Outstanding.Select(step => step.OccurrenceId).Distinct().Count());
    }

    [Fact]
    public void OutsideAnAttackDoesNotCreateADefenseOccurrence()
    {
        var (world, hero, _) = Board();
        world.Attack = null;
        Assert.False(Attack.BeginDefenseAbility(world, 0, hero));
        Assert.Empty(world.Agenda.Outstanding);
        Assert.True(hero.Ready);
    }

    private static (World World, Card Hero, Card Enemy) Board()
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(cards, players: 1);
        var seat = world.CreateSeat("p0");
        var hero = world.CreateCard("01001a", seat.Hero);
        seat.IdentityCard = hero;
        world.CreateCard("03033", seat.Hand);
        var enemy = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.Abilities = AuthoredCards.Runner();
        world.Attack = new EnemyAttack(enemy.ObjectId, 0, hero.ObjectId, Defender: hero.ObjectId);
        world.Activation = new EnemyActivation(enemy.ObjectId, 0, Attacking: true, Id: 17);
        return (world, hero, enemy);
    }
}

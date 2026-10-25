using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ExpertDefenseTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [Rule("rr:triggering-condition.1.1")]
    public void CopiesAddThreeDefenseOnlyForTheCurrentAttack(int copies)
    {
        // 03033: "it gets +3 DEF for that attack."
        // rr:triggering-condition.1.1: "each copy may be triggered once".
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(cards, players: 1);
        var seat = world.CreateSeat("p0");
        var hero = world.CreateCard("01001a", seat.Hero);
        seat.IdentityCard = hero;
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            "attack", Amount: 4, Affects: villain.ObjectId));
        world.CreateCard("01088", seat.Deck);
        for (int index = 0; index < 4; index++)
            world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        var events = Enumerable.Range(0, copies).Select(_ => world.CreateCard("03033", seat.Hand)).ToArray();
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 1, Subject: villain.ObjectId, Seat: 0));
        int played = 0;
        for (int decisions = 0; ; decisions++)
        {
            var prompt = Sequence.Work(world, cards, runner, []);
            if (prompt is null) break;
            Assert.True(decisions < 20, prompt.Label);
            var offered = prompt.Affordances.FirstOrDefault(offer => events.Any(card => card.ObjectId == offer.AnchorId));
            var answer = prompt.Asking == Question.Defender ? Decision.Take(hero.ObjectId)
                : offered is not null ? Decision.Take(offered.Id) : Decision.Decline;
            if (offered is not null) played++;
            Sequence.Answer(world, cards, runner, prompt, answer, []);
        }
        Assert.Equal(copies, played);
        Assert.Equal(Math.Max(0, 4 - copies * 3), hero.Damage);
        Assert.False(hero.Ready);
        Assert.All(events, card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
        Assert.Equal(3, StateFields.Modified(world, hero, "defense", cards, world.Players));
        Assert.Null(world.Attack);

        // A subsequent attack must not inherit the first attack's DEF grant.
        long damageBefore = hero.Damage;
        hero.Refresh();
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Subject: villain.ObjectId, Seat: 0));
        for (int decisions = 0; ; decisions++)
        {
            var prompt = Sequence.Work(world, cards, runner, []);
            if (prompt is null) break;
            Assert.True(decisions < 20, prompt.Label);
            Sequence.Answer(world, cards, runner, prompt,
                prompt.Asking == Question.Defender ? Decision.Take(hero.ObjectId) : Decision.Decline, []);
        }
        Assert.Equal(damageBefore + 4, hero.Damage);
        Assert.Null(world.Attack);
    }
}

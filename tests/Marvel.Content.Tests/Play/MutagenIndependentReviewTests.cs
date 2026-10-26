using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class MutagenIndependentReviewTests : CoreCardTestBase
{
    [Theory]
    [InlineData("02022")]
    [InlineData("02025")]
    [Rule("rr:delayed-effect.1")]
    public void ShuffleWaitsWhileAttackDamageInterruptIsUnanswered(string face)
    {
        // "Delayed effects resolve ... after their specified timing point ... occurs."
        var world = Hero("01001a", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var backflip = world.CreateCard("01003", world.Seats[1].Hand);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        for (int i = 0; i < 5; i++) world.CreateCard("01186", deck);
        var source = world.CreateCard(face, deck);
        var other = world.CreateCard(face, world.AreaOf(DeckType.EncounterDiscardPile));
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Subject: villain.ObjectId, Seat: 1));
        var events = new List<GameEvent>();
        bool observed = false;
        for (int i = 0; i < 20; i++)
        {
            var prompt = Sequence.Work(world, Cards, runner, events);
            if (prompt is null) break;
            if (prompt.Affordances.Any(option => option.AnchorId == backflip.ObjectId))
            {
                observed = true;
                Assert.Equal(1, prompt.Player);
                Assert.Equal(DeckType.EncounterDiscardPile, source.Area.Type);
                Assert.Empty(events.OfType<AttackCompleted>());
                Assert.NotNull(world.Activation);
            }
            Assert.True(prompt.Cancellable, prompt.Description);
            Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, events);
        }
        Assert.True(observed);
        Assert.Same(deck, source.Area);
        Assert.Equal(DeckType.EncounterDiscardPile, other.Area.Type);
        Assert.Single(events.OfType<AttackCompleted>());
        Assert.Null(world.Activation);
    }

    [Fact]
    [Rule("rr:defend-defense.5.1")]
    public void KnightRecruitsForTheCrossPlayerDefender()
    {
        // "the player whose character defended the attack becomes the attacked player."
        var world = Hero("01001a", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var knight = world.CreateCard("02022", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var deck = world.AreaOf(DeckType.EncounterDeck);
        for (int i = 0; i < 4; i++) world.CreateCard("01186", deck);
        var recruited = world.CreateCard("02024", deck);
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Subject: knight.ObjectId, Seat: 1));
        var events = new List<GameEvent>();
        for (int i = 0; i < 20; i++)
        {
            var prompt = Sequence.Work(world, Cards, runner, events);
            if (prompt is null) break;
            Assert.True(prompt.Cancellable, prompt.Description);
            var decision = prompt.Asking == Question.Defender && prompt.Player == 0
                ? Decision.Take(world.Seats[0].IdentityCard.ObjectId) : Decision.Decline;
            Sequence.Answer(world, Cards, runner, prompt, decision, events);
        }
        Assert.Equal(DeckType.EngagedEnemiesArea, recruited.Area.Type);
        Assert.Equal(PlayArea.Of(0), recruited.Area.PlayArea);
        Assert.Equal(PlayArea.Of(1), knight.Area.PlayArea);
        Assert.Single(events.OfType<AttackCompleted>());
        Assert.Null(world.Activation);
    }
}

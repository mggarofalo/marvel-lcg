using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class MsMarvelDefenseIndependentReviewTests : CoreCardTestBase
{
    [Theory]
    [InlineData("01088")]
    [InlineData("05012")]
    [Rule("rr:attack-enemy-activation.5")]
    public void SecondSeatNovaInterruptsEachNewAttackWithoutExhausting(string resource)
    {
        // "When ... initiates an attack" has attack-initiation timing. Printed
        // 05012 spends energy, deals two damage, and has no exhaust or limit text.
        var world = Board();
        var nova = world.CreateCard("05012", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var enemy = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var unrelated = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var events = new List<GameEvent>();
        for (int attack = 0; attack < 2; attack++)
        {
            var payment = world.CreateCard(resource, world.Seats[1].Hand);
            world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2, Subject: enemy.ObjectId, Seat: 1));
            var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
            var offer = Assert.Single(prompt.Affordances, a => a.AnchorId == nova.ObjectId);
            Assert.Equal(1, prompt.Player);
            Sequence.Answer(world, Cards, runner, prompt, new Decision(offer.Id, [], [payment.ObjectId]), events);
            Finish(world, runner, events);
            Assert.Equal(2 * (attack + 1), enemy.Damage);
            Assert.Equal(2 * (attack + 1), world.Seats[1].IdentityCard.Damage);
            Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
            Assert.True(nova.Ready);
            Assert.Null(world.Attack);
        }
        Assert.Equal(0, unrelated.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, nova.Damage);
    }

    [Theory]
    [InlineData("01104", "05013", 1)]
    [InlineData("01104", "05013", 0)]
    [InlineData("01191", "05013", 1)]
    [InlineData("01191", "05013", 0)]
    [InlineData("01104", "01078", 1)]
    [InlineData("01104", "01078", 0)]
    [InlineData("01191", "01078", 1)]
    [InlineData("01191", "01078", 0)]
    [Rule("rr:cancel.4")]
    [Rule("rr:surge.1")]
    public void FullCrossSeatRevealCancelsOnlyWhenRevealedAndDiscardsAfterNestedAttack(string face, string sourceFace, int payer)
    {
        // "The card is still regarded as revealed" and goes to encounter discard.
        // Printed 01104 heals Rhino; canceling that text leaves his damage intact.
        // rr:surge.1: "The surge keyword is equivalent" to a When Revealed ability.
        int revealer = 1 - payer;
        var world = Board();
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        villain.TakeDamage(5);
        var source = world.CreateCard(sourceFace, world.Seats[payer].Hand);
        var payment = world.CreateCard("01089", world.Seats[payer].Hand);
        var treachery = world.CreateCard(face, world.AreaOf(DeckType.RevealingArea, PlayArea.Of(revealer)));
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            1, 4, Subject: treachery.ObjectId, Seat: revealer));
        var events = new List<GameEvent>();
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        var offer = Assert.Single(prompt.Affordances, a => a.AnchorId == source.ObjectId);
        Assert.Equal(payer, prompt.Player);
        Sequence.Answer(world, Cards, runner, prompt, new Decision(offer.Id, [], [payment.ObjectId]), events);
        Assert.Equal(DeckType.RevealingArea, source.Area.Type);
        var defense = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(DeckType.RevealingArea, source.Area.Type);
        Assert.Equal(payer, world.Attack!.Player);
        Sequence.Answer(world, Cards, runner, defense, Decision.Decline, events);
        Finish(world, runner, events);
        Assert.Equal(5, villain.Damage);
        Assert.Equal(2, world.Seats[payer].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[revealer].IdentityCard.Damage);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, treachery.Area.Type);
        Assert.Single(events.OfType<WhenRevealedCanceled>());
        Assert.True(world.Seats[revealer].IdentityCard.Ready);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(revealer)).Cards);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Equal(Outcome.Unfinished, world.Result);
        Assert.Null(world.Attack);
    }

    [Fact]
    public void NovaCannotSpendAnotherSeatsEnergyAndItsOwnEnergyIsAnEligibilityControl()
    {
        var world = Board();
        var nova = world.CreateCard("05012", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var enemy = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01088", world.Seats[0].Hand);
        var runner = AuthoredCards.Runner();
        var occurrence = Occurrence.ForAttack(1, [Steps.AttackInitiated], world, Cards,
            enemy.ObjectId, world.Seats[1].IdentityCard.ObjectId, player: 1);
        Assert.DoesNotContain(runner.Waiting(world, occurrence, WindowKind.Interrupt), a => a.Card == nova.ObjectId);
        world.CreateCard("01088", world.Seats[1].Hand);
        Assert.Single(runner.Waiting(world, occurrence, WindowKind.Interrupt), a => a.Card == nova.ObjectId);
    }

    private static World Board()
    {
        var world = Hero("03001a", 2);
        foreach (var seat in world.Seats)
            for (int i = 0; i < 4; i++) world.CreateCard("01087", seat.Deck);
        for (int i = 0; i < 8; i++) world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        return world;
    }

    private static void Finish(World world, Marvel.Cards.Run.AbilityRunner runner, List<GameEvent> events)
    {
        for (int i = 0; i < 20; i++)
        {
            var prompt = Sequence.Work(world, Cards, runner, events);
            if (prompt is null)
            {
                Assert.False(world.Agenda.IsBusy);
                return;
            }
            Assert.True(prompt.Cancellable, prompt.Description);
            Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, events);
        }
        Assert.Fail("The prepared attack/reveal did not finish within twenty choices.");
    }
}

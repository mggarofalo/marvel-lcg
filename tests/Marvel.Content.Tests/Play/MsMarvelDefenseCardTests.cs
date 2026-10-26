using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class MsMarvelDefenseCardTests : CoreCardTestBase
{
    [Theory]
    [InlineData("01094", true)]
    [InlineData("01101", true)]
    [InlineData("01101", false)]
    [Rule("rr:you-your.7")]
    public void NovaSpendsEnergyAgainstTheEnemyAttackingItsController(string attacker, bool hero)
    {
        // "You refers to the attacked player"; the printed Interrupt has no form or exhaust cost.
        var world = Board(hero ? "03001a" : "03001b", 2);
        var nova = world.CreateCard("05012", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        nova.Exhaust();
        var enemy = world.CreateCard(attacker, world.AreaOf(attacker == "01094" ? DeckType.VillainArea : DeckType.EngagedEnemiesArea));
        var energy = world.CreateCard("01088", world.Seats[0].Hand);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var occurrence = Occurrence.ForAttack(1, [Steps.AttackInitiated], world, Cards,
            enemy.ObjectId, world.Seats[0].IdentityCard.ObjectId, player: 0);
        var ability = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Interrupt), a => a.Card == nova.ObjectId);
        runner.ResolveWindow(world, occurrence, ability, new Decision(ability.Card, [], [energy.ObjectId]));
        Assert.Equal(2, enemy.Damage);
        Assert.Equal(DeckType.DiscardPile, energy.Area.Type);
        Assert.False(nova.Ready);
        Assert.Equal(0, nova.Damage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NovaNeedsEnergyAndTheAttackMustBeAgainstItsController(bool foreign)
    {
        var world = Board("03001a", 2);
        var nova = world.CreateCard("05012", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var enemy = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard(foreign ? "01088" : "01089", world.Seats[0].Hand);
        var runner = AuthoredCards.Runner();
        var occurrence = Occurrence.ForAttack(1, [Steps.AttackInitiated], world, Cards,
            enemy.ObjectId, world.Seats[foreign ? 1 : 0].IdentityCard.ObjectId, player: foreign ? 1 : 0);
        var digest = world.Digest().Canonical();
        long random = world.Random.Generator.WordsConsumed;
        Assert.DoesNotContain(runner.Waiting(world, occurrence, WindowKind.Interrupt), a => a.Card == nova.ObjectId);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random, world.Random.Generator.WordsConsumed);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:cancel.4")]
    public void GetBehindMeCancelsAnotherPlayersTreacheryAndAttacksThePayingHero(int revealing)
    {
        // "The canceled effects do not resolve"; revealing and eventual discard are not canceled.
        var world = Board("03001a", 2);
        var source = world.CreateCard("05013", world.Seats[1].Hand);
        var payment = world.CreateCard("01089", world.Seats[1].Hand);
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var treachery = world.CreateCard("01104", world.AreaOf(DeckType.RevealingArea));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var occurrence = new Occurrence(1, [Steps.CardRevealed], Subject: treachery.ObjectId, Player: revealing);
        var ability = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Interrupt), a => a.Card == source.ObjectId);
        runner.ResolveWindow(world, occurrence, ability, new Decision(ability.Card, [], [payment.ObjectId]));
        Assert.Contains(world.Effects.Active(), e => e.Kind == "cancelWhenRevealed" && e.Affects == treachery.ObjectId);
        Assert.Equal(DeckType.RevealingArea, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.True(world.Agenda.IsBusy);
        Assert.Contains(world.Agenda.Outstanding, step => step.Seat == 1);
        Assert.Equal(DeckType.RevealingArea, treachery.Area.Type);
        Assert.Equal(0, villain.Damage);
        var events = new List<GameEvent>();
        for (int decisions = 0; decisions < 12; decisions++)
        {
            var prompt = Sequence.Work(world, Cards, runner, events);
            if (prompt is null) break;
            Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, events);
        }
        Assert.False(world.Agenda.IsBusy);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(2, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Single(runner.ExecuteReveal(world, treachery, revealing).Events.OfType<WhenRevealedCanceled>());
    }

    [Theory]
    [InlineData("03001b", "01104")]
    [InlineData("03001a", "01101")]
    public void GetBehindMeRejectsWrongFormAndNonTreachery(string identity, string revealed)
    {
        var world = Board(identity);
        var source = world.CreateCard("05013", world.Seats[0].Hand);
        world.CreateCard("01089", world.Seats[0].Hand);
        var card = world.CreateCard(revealed, world.AreaOf(DeckType.RevealingArea));
        var occurrence = new Occurrence(1, [Steps.CardRevealed], Subject: card.ObjectId, Player: 0);
        Assert.DoesNotContain(AuthoredCards.Runner().Waiting(world, occurrence, WindowKind.Interrupt), a => a.Card == source.ObjectId);
        Assert.Equal(DeckType.HandsArea, source.Area.Type);
    }

    [Fact]
    public void NovaDefeatingTheAttackerStopsTheRestOfItsAttack()
    {
        // faq:05012: "the rest of the attack sequence fails to resolve."
        var world = Board("03001a");
        var nova = world.CreateCard("05012", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        var minion = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        minion.TakeDamage(1);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2, Subject: minion.ObjectId, Seat: 0));
        var events = new List<GameEvent>();
        var prompt = Sequence.Work(world, Cards, runner, events)!;
        var offer = Assert.Single(prompt.Affordances, a => a.AnchorId == nova.ObjectId);
        Sequence.Answer(world, Cards, runner, prompt, new Decision(offer.Id, [], [payment.ObjectId]), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.False(world.Agenda.IsBusy);
        Assert.Null(world.Attack);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, nova.Damage);
    }

    private static World Board(string identity, int players = 1)
    {
        var world = Hero(identity, players);
        foreach (var seat in world.Seats)
        {
            world.CreateCard("01087", seat.Deck);
            world.CreateCard("01087", seat.Deck);
        }
        for (int i = 0; i < 8; i++) world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        return world;
    }

}


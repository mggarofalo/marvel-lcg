using Marvel.Content.Tests.Cards;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingBulldozerTests : CoreCardTestBase
{
    [Rule("rr:overkill.1")]
    [Rule("rr:lasting-effects.5")]
    [Rule("rr:stun-stunned.6")]
    [Theory]
    [InlineData("07046", true, false, true, 2)]
    [InlineData("07047", true, false, true, 2)]
    [InlineData("07046", true, true, true, 0)]
    [InlineData("07047", true, true, true, 0)]
    [InlineData("07046", false, false, true, 3)]
    [InlineData("07046", true, false, false, 0)]
    public void OnlyBulldozersUnreplacedAttackGainsOverkill(
        string face, bool defend, bool stunned, bool bulldozerAttacks, int expectedDamage)
    {
        // 07046/07047: "Forced Interrupt: When Bulldozer attacks, the attack gains overkill."
        // Overkill: "If this attack defeats an ally, deal the damage beyond its hit points
        // to the identity of that ally's controller." A lasting effect "expires as soon
        // as its specified duration ends."
        // Stun: "If a stunned villain or minion would attack, discard the stunned status card instead."
        var world = Hero("01001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.CreateCard("01088", world.Seats[0].Deck);
        world.CreateCard("01089", world.Seats[0].Deck);
        var own = Group(world, face, "07048");
        var other = Group(world, "07032", "07034");
        world.BindEncounterTopology(new EncounterTopology(world, [own, other], other.Villain));
        var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        ally.TakeDamage(1);
        var attacker = bulldozerAttacks ? own.Villain : other.Villain;
        if (stunned) Statuses.Give(world, attacker, Statuses.Stunned);
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2, Subject: attacker.ObjectId, Seat: 0));
        FinishAttack(world, runner, ally, defend);

        Assert.Equal(expectedDamage, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(defend && !stunned ? DeckType.DiscardPile : DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(0, StateFields.Modified(world, own.Villain, "overkill", Cards, 1));
        Assert.Equal(0, StateFields.Modified(world, other.Villain, "overkill", Cards, 1));
        Assert.False(Statuses.Has(world, attacker, Statuses.Stunned));
        Assert.Same(other, world.EncounterTopology!.Active);
        Assert.Equal(stunned ? 2 : 1, other.Deck.Cards.Count);
    }

    [Theory]
    [InlineData("07046", 1)]
    [InlineData("07047", 2)]
    public void SchemingPlacesThreatOnlyOnBulldozersOwnScheme(string face, int threat)
    {
        // 07046/07047: "When Bulldozer schemes, place the threat on his side scheme
        // instead of the main scheme."
        var world = Hero("01001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var main = world.CreateCard("07001b", world.AreaOf(DeckType.MainSchemesArea));
        var own = Group(world, face, "07048");
        var other = Group(world, "07032", "07034");
        world.BindEncounterTopology(new EncounterTopology(world, [own, other], other.Villain));
        world.Agenda.Add(new PhaseStep(new EnemySchemeOperation(), 1, 2, Subject: own.Villain.ObjectId, Seat: 0));

        Sequence.Finish(world, Cards, runner, []);

        Assert.Equal(threat, own.Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, other.Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Same(other, world.EncounterTopology!.Active);
        Assert.Single(other.Deck.Cards);
        Assert.Equal(2, own.Deck.Cards.Count);
    }

    private static void FinishAttack(World world, AbilityRunner runner, Card ally, bool defend)
    {
        var events = new List<GameEvent>();
        var prompt = Sequence.Work(world, Cards, runner, events);
        for (int steps = 0; prompt is not null; steps++)
        {
            Assert.True(steps < 10);
            var answer = prompt.Asking == Question.Defender && defend
                ? Decision.Take(ally.ObjectId) : Decision.Decline;
            if (answer == Decision.Decline) Assert.True(prompt.Cancellable, prompt.Description);
            Sequence.Answer(world, Cards, runner, prompt, answer, events);
            prompt = Sequence.Work(world, Cards, runner, events);
        }
    }

    private static EncounterDeckGroup Group(World world, string villain, string scheme)
    {
        var deck = world.CreateArea(DeckType.EncounterDeck);
        Card[] cards = [world.CreateCard("07037", deck), world.CreateCard("07037", deck)];
        return new(world.CreateCard(villain, world.AreaOf(DeckType.VillainArea)),
            world.CreateCard(scheme, world.AreaOf(DeckType.SideSchemesArea)), deck,
            world.CreateArea(DeckType.EncounterDiscardPile), cards);
    }
}

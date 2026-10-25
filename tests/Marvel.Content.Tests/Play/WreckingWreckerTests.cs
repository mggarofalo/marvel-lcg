using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingWreckerTests : CoreCardTestBase
{
    [Rule("rr:defend-defense.6")]
    [Rule("rr:stun-stunned.6")]
    [Theory]
    [InlineData("07002", 2, "none", false, 4)]
    [InlineData("07003", 3, "none", false, 5)]
    [InlineData("07002", 2, "hero", false, 0)]
    [InlineData("07003", 3, "hero", false, 0)]
    [InlineData("07002", 2, "ally", false, 0)]
    [InlineData("07003", 3, "ally", false, 0)]
    [InlineData("07002", 2, "none", true, 0)]
    [InlineData("07003", 3, "none", true, 0)]
    public void BonusAppliesOnlyWhileHisAttackIsUndefended(
        string face, int printedAttack, string defender, bool stunned, int heroDamage)
    {
        // 07002/07003: "While Wrecker is attacking, he gets +2 ATK if the attack is undefended."
        // rr:defend-defense.6: "If no character is used to defend against an enemy attack,
        // that attack is considered undefended."
        // rr:stun-stunned.6: "If a stunned villain or minion would attack,
        // discard the stunned status card instead."
        var world = Hero("01001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.CreateCard("01088", world.Seats[0].Deck);
        world.CreateCard("01089", world.Seats[0].Deck);
        var own = Group(world, face, "07004");
        var other = Group(world, "07032", "07034");
        world.BindEncounterTopology(new EncounterTopology(world, [own, other], other.Villain));
        var ally = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        if (stunned) Statuses.Give(world, own.Villain, Statuses.Stunned);
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2, Subject: own.Villain.ObjectId, Seat: 0));
        var chosen = defender switch
        {
            "hero" => world.Seats[0].IdentityCard,
            "ally" => ally,
            _ => null,
        };

        Finish(world, runner, chosen);

        Assert.Equal(heroDamage, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(defender == "ally" && printedAttack == 3 ? DeckType.DiscardPile : DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(defender == "ally" && printedAttack == 2 ? 2 : 0, ally.Damage);
        Assert.Equal(printedAttack, StateFields.Modified(world, own.Villain, "attack", Cards, 1));
        Assert.Null(world.Attack);
        Assert.False(Statuses.Has(world, own.Villain, Statuses.Stunned));
        Assert.Same(other, world.EncounterTopology!.Active);
        Assert.Equal(stunned ? 2 : 1, other.Deck.Cards.Count);
    }

    [Rule("rr:defend-defense.6")]
    [Theory]
    [InlineData("07002", 4)]
    [InlineData("07003", 5)]
    public void BoostDefeatingAllyMakesWreckersAttackUndefended(string face, int damage)
    {
        // rr:defend-defense.6: "if a defending ally is defeated before damage from the
        // attack is dealt (such as through a 'Boost' ability), the attack is considered undefended."
        // 07027 boost: "Deal 1 damage to the defending character."
        var world = Hero("01001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.CreateCard("01088", world.Seats[0].Deck);
        var own = Group(world, face, "07004");
        var other = Group(world, "07017", "07019", "07027");
        world.BindEncounterTopology(new EncounterTopology(world, [own, other], other.Villain));
        var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        ally.TakeDamage(1);
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2, Subject: own.Villain.ObjectId, Seat: 0));

        Finish(world, runner, ally);

        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(damage, world.Seats[0].IdentityCard.Damage);
        Assert.Contains(other.Discard.Cards, card => card.FaceId == "07027");
        Assert.Same(other, world.EncounterTopology!.Active);
    }

    [Rule("rr:defend-defense.4")]
    [Rule("rr:attack-enemy-activation.step.4")]
    [Fact]
    public void BackflipDefendsAfterDamageCalculationAndPreventsTheCalculatedAttack()
    {
        // rr:defend-defense.4: "that player's identity becomes the defender".
        // rr:attack-enemy-activation.step.4: "Determine how much damage will be dealt by the attack."
        // 01003: "When you would take any amount of damage from an attack, prevent all of that damage."
        var world = Hero("01001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var backflip = world.CreateCard("01003", world.Seats[0].Hand);
        world.CreateCard("01088", world.Seats[0].Deck);
        var own = Group(world, "07002", "07004");
        var other = Group(world, "07032", "07034");
        world.BindEncounterTopology(new EncounterTopology(world, [own, other], other.Villain));
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2, Subject: own.Villain.ObjectId, Seat: 0));
        var events = new List<GameEvent>();
        var defense = ReachDefense(world, runner, events);
        Sequence.Answer(world, Cards, runner, defense, Decision.Decline, events);
        var prevent = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        var offer = Assert.Single(prevent.Affordances, option => option.AnchorId == backflip.ObjectId);
        Assert.Equal(4, world.Attack!.CalculatedDamage);

        Sequence.Answer(world, Cards, runner, prevent, Decision.Take(offer.Id), events);
        Sequence.Finish(world, Cards, runner, events);

        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.DiscardPile, backflip.Area.Type);
        Assert.Equal(world.Seats[0].IdentityCard.ObjectId, world.FinishedAttack!.Defender);
        Assert.Equal(4, world.FinishedAttack.CalculatedDamage);
        Assert.Equal(2, StateFields.Modified(world, own.Villain, "attack", Cards, 1));
    }

    private static Prompt ReachDefense(World world, AbilityRunner runner, List<GameEvent> events)
    {
        for (int steps = 0; steps < 5; steps++)
        {
            var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
            if (prompt.Asking == Question.Defender) return prompt;
            Assert.True(prompt.Cancellable, prompt.Description);
            Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, events);
        }
        throw new InvalidOperationException("The attack did not reach its defense opportunity.");
    }

    [Theory]
    [InlineData("07002", 2)]
    [InlineData("07003", 3)]
    public void SchemingUsesHisOwnSideScheme(string face, int threat)
    {
        // 07002/07003: "When Wrecker schemes, place the threat on his side scheme
        // instead of the main scheme."
        var world = Hero("01001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var main = world.CreateCard("07001b", world.AreaOf(DeckType.MainSchemesArea));
        var own = Group(world, face, "07004");
        var other = Group(world, "07032", "07034");
        world.BindEncounterTopology(new EncounterTopology(world, [own, other], other.Villain));
        world.Agenda.Add(new PhaseStep(new EnemySchemeOperation(), 1, 2, Subject: own.Villain.ObjectId, Seat: 0));

        Sequence.Finish(world, Cards, runner, []);

        Assert.Equal(threat, own.Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, other.Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Same(other, world.EncounterTopology!.Active);
        Assert.Single(other.Deck.Cards);
    }

    private static void Finish(World world, AbilityRunner runner, Card? defender)
    {
        var events = new List<GameEvent>();
        var prompt = Sequence.Work(world, Cards, runner, events);
        for (int steps = 0; prompt is not null; steps++)
        {
            Assert.True(steps < 10);
            var answer = prompt.Asking == Question.Defender && defender is not null
                ? Decision.Take(defender.ObjectId) : Decision.Decline;
            if (answer == Decision.Decline) Assert.True(prompt.Cancellable, prompt.Description);
            Sequence.Answer(world, Cards, runner, prompt, answer, events);
            prompt = Sequence.Work(world, Cards, runner, events);
        }
    }

    private static EncounterDeckGroup Group(World world, string villain, string scheme, string boost = "07037")
    {
        var deck = world.CreateArea(DeckType.EncounterDeck);
        Card[] cards = [world.CreateCard(boost, deck), world.CreateCard(boost, deck)];
        return new(world.CreateCard(villain, world.AreaOf(DeckType.VillainArea)),
            world.CreateCard(scheme, world.AreaOf(DeckType.SideSchemesArea)), deck,
            world.CreateArea(DeckType.EncounterDiscardPile), cards);
    }
}

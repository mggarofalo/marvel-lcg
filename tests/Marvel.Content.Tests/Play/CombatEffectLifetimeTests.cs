using Marvel.Rules.Setup;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CombatEffectLifetimeTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    private static readonly SetupCatalog Setup = SetupCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:lasting-effects.5")]
    [Rule("rr:replacement-effect.1")]
    public void ReplacedStampedeCannotStunTheVictimOfALaterAttack(bool webbed)
    {
        // A replaced attack never happens; a lasting effect expires when its
        // specified duration ends, even when no attack steps were performed.
        var world = Hero();
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        if (webbed) world.CreateCard("01009", world.AreaOf(
            DeckType.UpgradesArea, host: villain.ObjectId, cardOwner: 0));
        else Statuses.Give(world, villain, Statuses.Stunned);
        Reveal(world, "01106");
        Run(world);
        Assert.Empty(world.Effects.Registered);
        foreach (var status in Statuses.On(world, villain, Statuses.Stunned).ToArray())
            Discard.Card(world, status, "test", []);
        DamageAttacks.Attack(world, Cards, world.Seats[0].IdentityCard, villain,
            2, "test", "Attack", []);
        Assert.False(Statuses.Has(world, villain, Statuses.Stunned));
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), Round: 2, Number: 2, Subject: villain.ObjectId, Seat: 0));
        Run(world);
        Assert.True(world.Seats[0].IdentityCard.Damage > 0);
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:scheme-enemy-activation.step.2.c")]
    [Rule("rr:lasting-effects.5")]
    public void ReplacedSchemeRetiresItsBoostAndDelayedDamageEffects(bool swoop)
    {
        // Boost icons add SCH for this activation; replacement of its threat
        // does not extend that duration into a later attack or scheme.
        var world = Hero();
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        var responsibility = world.CreateCard("01061", world.Seats[0].Hand);
        world.CreateCard(swoop ? "01168" : "01106", world.AreaOf(DeckType.EncounterDeck));
        Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Tough);
        Reveal(world, "01186");
        Run(world, response: responsibility);
        Assert.Empty(world.Effects.Registered);
        Assert.Equal(1, StateFields.Modified(world, villain, "scheme", Cards, 1));
        Assert.Equal(0, scheme.Tokens.GetValueOrDefault("k_threat"));
        DamageAttacks.Attack(world, Cards, world.Seats[0].IdentityCard, villain,
            1, "test", "Attack", []);
        Assert.False(Statuses.Has(world, villain, Statuses.Stunned));
        world.CreateCard("01106", world.AreaOf(DeckType.EncounterDeck));
        world.Agenda.Add(new PhaseStep(new EnemySchemeOperation(), Round: 2, Number: 2, Subject: villain.ObjectId, Seat: 0));
        Run(world);
        Assert.Equal(2, scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [Rule("rr:overkill.2")]
    [Rule("rr:tough.3")]
    [Rule("rr:delayed-effect.1")]
    public void EveryDamagedRecipientOfTheAttackIsStunned(bool swoop, bool toughHero)
    {
        // Overkill is damage from the attack. Tough prevents all damage, so
        // that recipient is not considered to have taken damage and is not stunned.
        var world = Hero();
        var hero = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.CreateCard("01099", world.AreaOf(DeckType.UpgradesArea, host: villain.ObjectId));
        var ally = world.CreateCard("01058", world.AreaOf(DeckType.AlliesArea,
            PlayArea.Of(0), cardOwner: 0));
        ally.TakeDamage(2);
        if (toughHero) Statuses.Give(world, hero, Statuses.Tough);
        if (swoop)
        {
            world.CreateCard("01168", world.AreaOf(DeckType.EncounterDeck));
            world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), Round: 1, Number: 2, Subject: villain.ObjectId, Seat: 0));
        }
        else Reveal(world, "01106");
        var events = Run(world, defender: ally);
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(!toughHero, Statuses.Has(world, hero, Statuses.Stunned));
        Assert.Equal(!toughHero, hero.Damage > 0);
        Assert.Contains(events.OfType<CardAttached>(), attached => attached.Host == ally.ObjectId);
        Assert.Empty(world.Areas.Where(area => area.Host == ally.ObjectId).SelectMany(area => area.Cards));
        Assert.Empty(world.Effects.Registered);
    }

    [Fact]
    [Rule("rr:delayed-effect.1")]
    [Rule("rr:lasting-effects.5")]
    public void UnrelatedDamageAndCompletionDoNotConsumeAnotherActivationsEffect()
    {
        // Synthetic owner-isolation contract: a pending inner attack cannot
        // borrow an outer attack's delayed condition or expire its duration.
        var world = Hero();
        var hero = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Activation = new EnemyActivation(villain.ObjectId, 0, true, Id: 20);
        world.Effects.Register(new ContinuousEffect(EffectSource.DelayedEffect,
            DelayedEffects.StunTheSubject, Card: villain.ObjectId,
            Lasts: new Duration(Until: TimingPoints.EndOfAttack, OnCondition: Steps.DamageDealt))
        { RepeatsWithinDuration = true });
        world.Effects.CompleteActivation(21, []);
        DamageAttacks.Attack(world, Cards, hero, villain, 1, "test", "Attack", []);
        Assert.False(Statuses.Has(world, villain, Statuses.Stunned));
        DamageAttacks.Attack(world, Cards, villain, hero, 1, "test", "Attack", []);
        Assert.False(Statuses.Has(world, hero, Statuses.Stunned));
        DamageAttacks.Resolve(world, Cards,
            new AttackDamageRequest(villain, villain, hero, 1, "test", "Attack", ActivationId: 20), []);
        Assert.True(Statuses.Has(world, hero, Statuses.Stunned));
        world.Effects.CompleteActivation(20, []);
        Assert.Empty(world.Effects.Registered);
    }

    [Fact]
    [Rule("rr:delayed-effect.1")]
    [Rule("rr:retaliate-x.1")]
    public void ChargeAtEndDiscardPrecedesBlackPanthersAfterAttackRetaliate()
    {
        // rr:delayed-effect.1: "before responses to that point or condition may be used".
        // Charge 01099 says "At the end of this attack, discard Charge."
        // Black Panther's printed retaliate is a Forced Response after the attack.
        var world = Hero("black_panther", "01040a");
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var charge = world.CreateCard("01099",
            world.AreaOf(DeckType.UpgradesArea, host: villain.ObjectId));
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Subject: villain.ObjectId, Seat: 0));

        var events = Run(world);

        int discarded = events.FindIndex(item => item is CardsMoved move
            && move.Cards.Any(card => card.Card == charge.ObjectId));
        int retaliated = events.FindIndex(item => item is FieldSet field
            && field.Card == villain.ObjectId && field.Verb == "Retaliate");
        Assert.True(discarded >= 0 && retaliated > discarded);
        Assert.Equal(DeckType.EncounterDiscardPile, charge.Area.Type);
        Assert.Equal(1, villain.Damage);
    }

    private static World Hero(string hero = "spider_man", string face = "01001a")
    {
        var world = WorldSetup.DealWithoutCardAbilities(Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", [hero]), Cards),
            ["Spider-Man"], 12345);
        world.Seats[0].IdentityCard.TurnTo(face);
        return world;
    }

    private static void Reveal(World world, string face)
    {
        var card = world.CreateCard(face, world.AreaOf(DeckType.RevealingArea));
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard), Round: 1, Number: 4, Subject: card.ObjectId, Seat: 0));
    }

    private static List<GameEvent> Run(World world, Card? defender = null, Card? response = null)
    {
        var runner = AuthoredCards.Runner();
        var events = new List<GameEvent>();
        for (int count = 0; ; count++)
        {
            var prompt = Sequence.Work(world, Cards, runner, events);
            if (prompt is null) return events;
            Assert.True(count < 30, prompt.Label);
            var chosen = prompt.Asking == Question.Defender ? defender : response;
            var option = chosen is null ? null
                : prompt.Affordances.FirstOrDefault(offer => offer.AnchorId == chosen.ObjectId);
            Sequence.Answer(world, Cards, runner, prompt,
                option is null ? Decision.Decline : Decision.Take(option.Id), events);
        }
    }
}

using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual authored research row through the full encounter procedure; no public admission.
public sealed class MutagenOverconfidenceTests : CoreCardTestBase
{
    [Theory]
    [InlineData(true, "02029", 0, false, true, 3)]
    [InlineData(true, "02029", 1, false, false, 2)]
    [InlineData(true, "02029", 0, true, false, 0)]
    [InlineData(true, "01154", 0, false, true, 3)]
    [InlineData(false, "02029", 0, false, false, 0)]
    [Rule("rr:surge.1")]
    [Rule("rr:damage.step.5")]
    [Rule("rr:attack-enemy-activation.step.3.b")]
    public void ActualPlacementDecidesOneExtraRevealAndCleanup(
        bool hero, string boost, int prevention, bool tough, bool surge, long damage)
    {
        // Surge: "reveal one additional encounter card". Step 3.b: "Resolve any Boost abilities".
        // Damage step 5 places the remaining damage after prevention. Printed 02031
        // says "placed by this activation"; ruling:d855181a23d49683 declines errata.
        var f = Create(hero, boost);
        var identity = f.World.Seats[1].IdentityCard;
        if (tough) Statuses.Give(f.World, identity, Statuses.Tough);
        if (prevention > 0) f.World.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            "preventDamage", Amount: prevention, Affects: identity.ObjectId, Lasts: new Duration(Uses: 1)));
        var events = f.Finish();
        Assert.Equal(damage, identity.Damage);
        Assert.Equal(hero ? 1 : 0, events.OfType<AttackCompleted>().Count());
        AssertFinished(f, surge);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [Rule("rr:scheme-enemy-activation.step.3")]
    public void SchemeUsesThreatActuallyPlacedAtTwoAndThree(bool stageTwo, bool surge)
    {
        // "Place threat on the main scheme equal to the scheming enemy's modified SCH value."
        var f = Create(false, "02029", stageTwo ? "02015" : "02014");
        f.Finish();
        Assert.Equal(stageTwo ? 3 : 2, f.Main.Tokens.GetValueOrDefault("k_threat"));
        AssertFinished(f, surge);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:status-cards.2")]
    public void StunAndConfuseCancelTheActivationWithoutSurge(bool hero)
    {
        // Status replacement "takes precedence over all other abilities".
        var f = Create(hero, "02029");
        Statuses.Give(f.World, f.Villain, hero ? Statuses.Stunned : Statuses.Confused);
        var events = f.Finish();
        Assert.Empty(events.OfType<AttackCompleted>());
        Assert.Equal(0, f.World.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, f.Main.Tokens.GetValueOrDefault("k_threat"));
        // A canceled activation never consumes its boost, leaving ten encounter cards.
        Assert.Equal(10, f.World.AreaOf(DeckType.EncounterDeck).Cards.Count);
        AssertFinished(f, false, activationCanceled: true);
    }

    [Theory]
    [InlineData(false, false, 0)]
    [InlineData(true, false, 2)]
    [InlineData(true, true, 0)]
    [Rule("rr:damage.2")]
    [Rule("rr:overkill.1")]
    public void AllyPlacementIncludesExcessAndOverkillAfterIdentityPrevention(bool overkill, bool tough, long damage)
    {
        // "place the specified value of damage tokens"; overkill deals excess
        // "to the identity of the player who controls the ally".
        var f = Create(true, "02029");
        var ally = f.World.CreateCard("01083", f.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        ally.TakeDamage(2);
        // This fixture supplies the engine's generic overkill modifier, rather than
        // claiming a printed Goblin attachment grants it.
        if (overkill) f.World.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Keywords.Overkill, Affects: f.Villain.ObjectId));
        if (tough) Statuses.Give(f.World, f.World.Seats[1].IdentityCard, Statuses.Tough);
        var events = FinishDefended(f, ally);
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(damage, f.World.Seats[1].IdentityCard.Damage);
        Assert.Equal(3, Assert.Single(events.OfType<AttackCompleted>()).DamageDealt);
        AssertFinished(f, true);
    }

    [Fact]
    [Rule("rr:damage.step.5")]
    public void UnrelatedDamageDoesNotTurnTwoActivationPlacementIntoThree()
    {
        var f = Create(true, "01154");
        var hero = f.World.Seats[1].IdentityCard;
        f.World.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            "preventDamage", Amount: 1, Affects: hero.ObjectId, Lasts: new Duration(Uses: 1)));
        var events = new List<GameEvent>();
        var prompt = Sequence.Work(f.World, Cards, f.Runner, events)!;
        // Seat zero passes its helper defense, then seat one can decline.
        while (prompt.Player != 1)
        {
            Sequence.Answer(f.World, Cards, f.Runner, prompt, Decision.Decline, events);
            prompt = Sequence.Work(f.World, Cards, f.Runner, events)!;
        }
        // The independent damage has no activation owner. Tough absorbs the boost's
        // own one damage, leaving the actual attack's two placement below threshold.
        Assert.True(DamagePlacement.Resolve(f.World, Cards,
            new(f.Villain, hero, 2, "SyntheticUnrelatedDamage", "Damage"), events).IsComplete);
        Statuses.Give(f.World, hero, Statuses.Tough);
        Sequence.Answer(f.World, Cards, f.Runner, prompt, Decision.Decline, events);
        events.AddRange(f.Finish());
        Assert.Equal(3, hero.Damage);
        Assert.Equal(2, Assert.Single(events.OfType<AttackCompleted>()).DamageDealt);
        AssertFinished(f, false);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [Rule("rr:attack-enemy-activation.step.3.b")]
    [Rule("rr:damage.step.5")]
    public void OwnedBoostPlacementAcrossThreeRecipientsDecidesSurgeWithoutAttackDamage(bool toughAlly, bool surge)
    {
        // Printed 02031 counts damage "placed by this activation" without a recipient restriction.
        // Concussive Blast deals 1 to each character you control during activation step 3.b.
        var f = Create(true, "01154");
        var area = f.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1);
        var ally = f.World.CreateCard("01020", area);
        var other = f.World.CreateCard("01083", area);
        if (toughAlly) Statuses.Give(f.World, ally, Statuses.Tough);
        // A generic engine modifier isolates boost placement from the zero-damage attack.
        f.World.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            "attack", Amount: -2, Affects: f.Villain.ObjectId));
        var events = f.Finish();
        Assert.Equal(1, f.World.Seats[1].IdentityCard.Damage);
        Assert.Equal(toughAlly ? 0 : 1, ally.Damage);
        Assert.Equal(1, other.Damage);
        Assert.Equal(0, Assert.Single(events.OfType<AttackCompleted>()).DamageDealt);
        AssertFinished(f, surge);
    }
    private static RedSkullFixture Create(bool hero, string boost, string stage = "02014")
    {
        var world = Hero("01001a", 2);
        World.MoveToTop(world.Seats[1].IdentityCard, world.AreaOf(DeckType.RemovedArea));
        world.Seats[1].IdentityCard = world.CreateCard(hero ? "01001a" : "01001b", world.Seats[1].Hero);
        foreach (var seat in world.Seats)
            for (int i = 0; i < 12; i++) world.CreateCard("01087", seat.Deck);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.CreateCard(stage, world.AreaOf(DeckType.VillainArea));
        world.CreateCard("02018b", world.AreaOf(DeckType.MainSchemesArea));
        var deck = world.AreaOf(DeckType.EncounterDeck);
        for (int i = 0; i < 8; i++) world.CreateCard("02029", deck);
        world.CreateCard("01101", deck); // Extra reveal is an inert Bomb Scare minion.
        world.CreateCard(boost, deck);
        var source = world.CreateCard("02031", world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)));
        source.TurnFaceDown();
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCards), 1, 4, Plan: true));
        return new(world, runner);
    }

    private static List<GameEvent> FinishDefended(RedSkullFixture f, Card ally)
    {
        var events = new List<GameEvent>();
        for (int step = 0; step < 40; step++)
        {
            var prompt = Sequence.Work(f.World, Cards, f.Runner, events);
            if (prompt is null) return events;
            var offer = prompt.Affordances.FirstOrDefault(option => option.AnchorId == ally.ObjectId);
            Decision answer = offer is not null ? Decision.Take(offer.Id) : Decision.Decline;
            if (!prompt.Cancellable && offer is null)
            {
                var order = Assert.Single(prompt.Affordances);
                Assert.Equal(Question.Order, prompt.Asking);
                answer = Decision.Take(order.Id, order.Targets!.Legal, []);
            }
            Sequence.Answer(f.World, Cards, f.Runner, prompt, answer, events);
        }
        throw new InvalidOperationException("Encounter did not finish");
    }

    private static void AssertFinished(RedSkullFixture f, bool surge, bool activationCanceled = false)
    {
        Assert.Empty(f.World.Agenda.Outstanding);
        Assert.Null(f.World.Activation);
        Assert.Equal(Outcome.Unfinished, f.World.Result);
        Assert.Equal(0, f.World.Seats[0].IdentityCard.Damage);
        Assert.Empty(f.World.AreaOf(DeckType.RevealingArea).Cards);
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.Single(f.World.AreaOf(DeckType.EncounterDiscardPile).Cards, card => card.FaceId == "02031");
        Assert.Equal(surge ? 1 : 0, f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)).Cards.Count);
        Assert.Equal(activationCanceled ? 10 : surge ? 8 : 9, f.World.AreaOf(DeckType.EncounterDeck).Cards.Count);
    }
}

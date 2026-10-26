using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CounterattackReceiptTests
{
    [Fact]
    [Rule("rr:overkill.2")]
    public void OverkillBindsTheIdentityThatActuallyReceivedTheExcess()
    {
        // "Excess damage ... is considered damage from an attack."
        var f = new CounterattackReceiptFixture();
        var ally = f.World.CreateCard("01002", f.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        ally.TakeDamage(1);
        f.World.Effects.Register(new(EffectSource.LastingEffect, "overkill", 1, Affects: f.Enemy.ObjectId));
        f.Attack();
        Assert.Equal(1, f.Finish(defender: ally.ObjectId));
        Assert.Equal(1, f.Hero.Damage);
        Assert.Equal(1, f.Enemy.Damage);
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:resolve.2")]
    public void OnlySuccessfulCounterattackPublishesItsPreparationCompletion(bool stunned)
    {
        // "One or more of its effects resolve" establishes resolution.
        var f = new CounterattackReceiptFixture();
        var suit = f.World.CreateCard("08009", f.World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        f.Hero.Exhaust();
        if (stunned) Statuses.Give(f.World, f.Hero, Statuses.Stunned);
        f.Attack();
        var prompt = f.CounterPrompt();
        var offer = Assert.Single(prompt.Affordances, a => a.AnchorId == f.Counter.ObjectId);
        f.Answer(prompt, Decision.Take(offer.Id));
        var completion = Sequence.Work(f.World, f.World.Facts, f.Runner, f.Events);
        if (stunned)
        {
            Assert.Null(completion);
            Assert.Equal(0, f.Enemy.Damage);
            Assert.False(f.Hero.Ready);
        }
        else
        {
            Assert.NotNull(completion);
            var receipt = Assert.IsType<ResolvedPreparationAbility>(f.World.Agenda.Occurrence!.ActorFacts!.PreparationAbility);
            Assert.Equal("08030", receipt.Face);
            Assert.Equal(1, receipt.Controller);
            Assert.Contains(completion.Affordances, a => a.AnchorId == f.Hero.ObjectId);
            var ready = Assert.Single(completion.Affordances, a => a.AnchorId == suit.ObjectId);
            f.Answer(completion, Decision.Take(ready.Id));
            f.Finish();
            Assert.True(f.Hero.Ready);
            Assert.False(suit.Ready);
            Assert.Equal(2, f.Enemy.Damage);
        }
        Assert.Equal(DeckType.DiscardPile, f.Counter.Area.Type);
    }

    [Fact]
    [Rule("rr:tough.3")]
    public void ToughPreventsTheResponseAndLeavesItsDiscardCostUnpaid()
    {
        // A character whose Tough prevents damage "is not considered to have taken damage".
        var f = new CounterattackReceiptFixture();
        Statuses.Give(f.World, f.Hero, Statuses.Tough);
        f.Attack();
        Assert.Equal(0, f.Finish());
        Assert.Equal(0, f.Hero.Damage);
        Assert.Equal(DeckType.UpgradesArea, f.Counter.Area.Type);
        Assert.Single(f.Events.OfType<AttackCompleted>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:you-your.2")]
    public void AnotherIdentityOrDefendingAllyCannotSupplyYourReceivedDamage(bool allyDefends)
    {
        // Damage to "you" applies to the identity hit-point dial.
        var f = new CounterattackReceiptFixture();
        var ally = f.World.CreateCard("01002",
            f.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        f.Attack(allyDefends ? 1 : 0);
        Assert.Equal(0, f.Finish(defender: allyDefends ? ally.ObjectId : -1));
        Assert.Equal(0, f.Hero.Damage);
        if (allyDefends) Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        else Assert.Equal(2, f.World.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.UpgradesArea, f.Counter.Area.Type);
    }

    [Fact]
    [Rule("rr:damage.step.9")]
    public void NonAttackDamageDuringAnEnemyAttackCannotSupplyTheReceipt()
    {
        // "After [character] ... takes any amount of damage" follows placement.
        // Synthetic boost-effect shape: the live activation does not make its direct damage attack damage.
        var f = new CounterattackReceiptFixture();
        f.World.Attack = new(f.Enemy.ObjectId, 1, f.Hero.ObjectId);
        DamagePlacement.Resolve(f.World, f.World.Facts,
            new(f.Enemy, f.Hero, 1, "Boost", "Damage"), f.Events);
        Assert.Equal(0, f.Finish());
        Assert.Equal(1, f.Hero.Damage);
        Assert.Equal(0, f.Enemy.Damage);
        Assert.Equal(DeckType.UpgradesArea, f.Counter.Area.Type);
    }

    [Fact]
    [Rule("rr:guard.1")]
    public void AttackLabelRespectsCurrentGuardBeforePayingTheDiscardCost()
    {
        // "The engaged player cannot attack any villain."
        var f = new CounterattackReceiptFixture(villain: true);
        var guard = f.World.CreateCard("01102", f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        f.World.Effects.Register(new(EffectSource.LastingEffect, "guard", 1, Affects: guard.ObjectId));
        f.Attack();
        Assert.Equal(0, f.Finish());
        Assert.True(f.Hero.Damage > 0);
        Assert.Equal(0, f.Enemy.Damage);
        Assert.Equal(DeckType.UpgradesArea, f.Counter.Area.Type);
    }

    [Fact]
    [Rule("rr:retaliate-x.1")]
    public void CounterattackIsAnAttackAndTheEnemyRetaliatesAfterIt()
    {
        // "After this character is attacked, deal X damage to the attacker."
        var f = new CounterattackReceiptFixture();
        f.World.Effects.Register(new(EffectSource.LastingEffect, "retaliate", 1, Affects: f.Enemy.ObjectId));
        f.Attack();
        Assert.Equal(1, f.Finish());
        Assert.Equal(3, f.Hero.Damage);
        Assert.Equal(2, f.Enemy.Damage);
        Assert.Equal(DeckType.DiscardPile, f.Counter.Area.Type);
    }

    [Fact]
    public void DecliningTheResponsePaysNoCostAndDoesNotLeakIntoTheNextAttack()
    {
        var f = new CounterattackReceiptFixture();
        f.Attack();
        Assert.Equal(1, f.Finish(use: false));
        Assert.Equal(DeckType.UpgradesArea, f.Counter.Area.Type);
        f.World.Effects.Register(new(EffectSource.LastingEffect, "preventDamage", 2,
            Affects: f.Hero.ObjectId, Lasts: Duration.NextUses(1)));
        f.Attack();
        Assert.Equal(0, f.Finish());
        Assert.Equal(2, f.Hero.Damage);
        Assert.Equal(0, f.Enemy.Damage);
        Assert.Equal(2, f.Events.OfType<AttackCompleted>().Count());
    }

    [Fact]
    public void RepeatedDiscoveryAndInvalidAnswersPreserveTheExactReceiptAndPendingWork()
    {
        var f = new CounterattackReceiptFixture();
        f.Attack();
        var prompt = f.CounterPrompt();
        var control = new CounterattackReceiptFixture();
        control.Attack();
        control.CounterPrompt();
        var occurrence = f.World.Agenda.Occurrence!;
        var receipt = Assert.Single(occurrence.ReceivedAttackDamage);
        Assert.Equal(1, receipt.Recipient.Controller);
        Assert.Equal(f.Hero.Incarnation, receipt.Recipient.Incarnation);
        Assert.Equal(f.Enemy.Incarnation, receipt.Attacker.Incarnation);
        string digest = f.World.Digest().Canonical();
        long words = f.World.Random.Generator.WordsConsumed;
        var current = f.World.Agenda.Current;
        int pending = f.World.Agenda.Count;
        for (int query = 0; query < 3; query++)
        {
            Assert.Contains(f.Runner.Waiting(f.World, occurrence, WindowKind.Response), a => a.Card == f.Counter.ObjectId);
            Assert.Throws<RulesNotImplementedException>(() => f.Answer(prompt, Decision.Take(int.MaxValue)));
            Assert.Equal(digest, f.World.Digest().Canonical());
            Assert.Equal(words, f.World.Random.Generator.WordsConsumed);
            Assert.Equal(current, f.World.Agenda.Current);
            Assert.Equal(pending, f.World.Agenda.Count);
            Assert.Same(receipt, Assert.Single(occurrence.ReceivedAttackDamage));
        }
        Assert.Equal(1, f.Finish());
        Assert.Equal(2, f.Enemy.Damage);
        Assert.Equal(1, control.Finish());
        Assert.Equal(control.World.Digest().Canonical(), f.World.Digest().Canonical());
        Assert.Equal(control.World.Random.Generator.WordsConsumed, f.World.Random.Generator.WordsConsumed);
    }

    [Fact]
    public void InvalidSuppliedPaymentDoesNotDiscardThePreparationOrConsumeItsOccurrence()
    {
        var f = new CounterattackReceiptFixture();
        f.Attack();
        var prompt = f.CounterPrompt();
        var offer = Assert.Single(prompt.Affordances, a => a.AnchorId == f.Counter.ObjectId);
        string digest = f.World.Digest().Canonical();
        long words = f.World.Random.Generator.WordsConsumed;
        int pending = f.World.Agenda.Count;
        Assert.Throws<RulesNotImplementedException>(() => f.Answer(prompt, Decision.Take(offer.Id, [], [int.MaxValue])));
        Assert.Equal(digest, f.World.Digest().Canonical());
        Assert.Equal(words, f.World.Random.Generator.WordsConsumed);
        Assert.Equal(pending, f.World.Agenda.Count);
        Assert.Equal(DeckType.UpgradesArea, f.Counter.Area.Type);
        Assert.Equal(1, f.Finish());
        Assert.Equal(2, f.Enemy.Damage);
    }

    [Fact]
    [Rule("rr:leaves-play.1")]
    public void AReenteredAttackerCannotReceiveTheOriginalCopiesCounterattack()
    {
        // A returned card is "a new copy of the card".
        var f = new CounterattackReceiptFixture();
        f.Attack();
        var prompt = f.CounterPrompt();
        var offer = Assert.Single(prompt.Affordances, a => a.AnchorId == f.Counter.ObjectId);
        var area = f.Enemy.Area;
        World.MoveToTop(f.Enemy, f.World.AreaOf(DeckType.EncounterDiscardPile));
        World.MoveToTop(f.Enemy, area);
        string digest = f.World.Digest().Canonical();
        Assert.Throws<RulesNotImplementedException>(() => f.Answer(prompt, Decision.Take(offer.Id)));
        Assert.Equal(digest, f.World.Digest().Canonical());
        Assert.Equal(0, f.Finish());
        Assert.Equal(0, f.Enemy.Damage);
        Assert.Equal(DeckType.UpgradesArea, f.Counter.Area.Type);
    }
}

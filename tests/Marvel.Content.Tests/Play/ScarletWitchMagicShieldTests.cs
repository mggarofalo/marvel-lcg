using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ScarletWitchMagicShieldTests
{
    [Fact]
    [Rule("rr:damage.step.3")]
    [Rule("rr:prevent.1.1")]
    public void MagicShieldPreventsThreeOfTheDamageAFriendlyCharacterWouldTake()
    {
        // Magic Shield (15008): "When a friendly character would take any amount
        // of damage, discard Magic Shield to prevent 3 of that damage."
        var declined = Fixture(out var unused);
        BeginAttack(declined);
        declined.Find(unused);
        declined.Finish();
        Assert.Equal(DeckType.UpgradesArea, unused.Area.Type);
        Assert.True(declined.Hero.Damage > 0);

        var f = Fixture(out var shield);
        BeginAttack(f);
        f.Use(f.Find(shield), shield);
        f.Finish(shield);
        Assert.Equal(DeckType.DiscardPile, shield.Area.Type);
        Assert.Equal(Math.Max(0, declined.Hero.Damage - 3), f.Hero.Damage);
    }

    [Fact]
    [Rule("rr:damage.step.2")]
    [Rule("rr:damage.step.3")]
    public void ToughPreventsTheDamageBeforeMagicShieldCouldBeOffered()
    {
        // rr:damage applies "Tough status cards" (step 2) before abilities that
        // trigger "when [character] would take any amount of damage" (step 3).
        var f = Fixture(out var shield);
        Statuses.Give(f.World, f.Hero, Statuses.Tough);
        BeginAttack(f);
        f.Finish(shield);
        Assert.Equal(0, f.Hero.Damage);
        Assert.False(Statuses.Has(f.World, f.Hero, Statuses.Tough));
        Assert.Equal(DeckType.UpgradesArea, shield.Area.Type);
    }

    private static BlackWidowPreventionFixture Fixture(out Card shield)
    {
        var f = new BlackWidowPreventionFixture();
        World.MoveToTop(f.Hero, f.World.AreaOf(DeckType.RemovedArea));
        f.World.Seats[1].IdentityCard = f.World.CreateCard("15001a,15001b", f.World.Seats[1].Hero);
        shield = f.Upgrade("15008");
        return f;
    }

    private static void BeginAttack(BlackWidowPreventionFixture f) =>
        f.World.Agenda.Now(new PhaseStep(new EnemyAttackOperation { DealBoostCards = false }, 1, 0,
            Subject: f.Villain.ObjectId, Seat: 1));
}

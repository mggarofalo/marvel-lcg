using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ExcessDamageThreatTests
{
    [Theory]
    [InlineData(false, false, 4)]
    [InlineData(false, true, 4)]
    [InlineData(true, false, 0)]
    [InlineData(true, true, 0)]
    [Rule("rr:overkill.3")]
    public void AttackExcessUsesUnpreventedDamageAndReplacesOverkill(bool tough, bool overkill, long expected)
    {
        // "counts the same value of excess damage that is calculated when resolving the overkill keyword."
        // Canonical 07022 exercises its printed routing independently of scenario admission.
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07022", 1);
        var runner = Runner();
        world.Abilities = runner;
        World.MoveToTop(source, world.AreaOf(DeckType.UpgradesArea, host: groups[1].Villain.ObjectId));
        var ally = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        ally.TakeDamage(2);
        if (tough) Statuses.Give(world, ally, Statuses.Tough);
        if (overkill) world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Keywords.Overkill, Affects: groups[1].Villain.ObjectId));
        var receipt = DamageAttacks.Resolve(world, world.Facts,
            new(groups[1].Villain, groups[1].Villain, ally, 5, "test", "Damage", Retaliate: false), []);
        Finish(world, runner);
        Assert.True(receipt.IsComplete);
        Assert.Equal(expected, receipt.Excess);
        Assert.Equal(expected, groups[1].Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.All(groups.Where(group => group != groups[1]), group =>
            Assert.Equal(0, group.Scheme.Tokens.GetValueOrDefault("k_threat")));
    }

    internal static AbilityRunner Runner() => Marvel.Content.Tests.Cards.AuthoredCards.Runner();

    internal static void Finish(World world, AbilityRunner runner)
    {
        for (int step = 0; ; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) return;
            Assert.True(step < 25, prompt.Label);
            Assert.True(prompt.Cancellable, prompt.Label);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, []);
        }
    }
}

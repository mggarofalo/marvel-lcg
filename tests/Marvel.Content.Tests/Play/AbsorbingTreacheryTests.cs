using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AbsorbingTreacheryTests
{
    [Theory]
    [InlineData(false, 3)]
    [InlineData(true, 4)]
    public void SteelKickHeroUsesMetalForIndirectDamage(bool metal, int damage)
    {
        var f = AbsorbingManFixture.Create(hero: true);
        if (metal) f.Card("04083", DeckType.EnvironmentArea);
        f.Reveal("04087");
        f.Finish();
        Assert.Equal(damage, f.World.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, f.Scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IcyGripAlwaysStunsAndIceAddsIndirectDamage(bool ice)
    {
        var f = AbsorbingManFixture.Create(hero: true);
        if (ice) f.Card("04081", DeckType.EnvironmentArea);
        f.Reveal("04090");
        f.Finish();
        Assert.True(Statuses.Has(f.World, f.World.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.Equal(ice ? 2 : 0, f.World.Seats[0].IdentityCard.Damage);
    }

    [Theory]
    [InlineData("04080", true, false)]
    [InlineData("04081", false, true)]
    [InlineData("04082", true, false)]
    [InlineData("04083", false, true)]
    public void BoostTraitGroupsUseTheCurrentEnvironment(string environment, bool stunned, bool tough)
    {
        var f = AbsorbingManFixture.Create(hero: true);
        f.Card(environment, DeckType.EnvironmentArea);
        f.Runner.ExecuteBoost(f.World, f.Card("04088", DeckType.BoostingArea), 0);
        f.Runner.ExecuteBoost(f.World, f.Card("04090", DeckType.BoostingArea), 0);
        Assert.Equal(stunned, Statuses.Has(f.World, f.World.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.Equal(tough, Statuses.Has(f.World, f.Villain, Statuses.Tough));
    }

    [Fact]
    public void OmniMorphWithoutEnvironmentDoesNotInventATraitEffect()
    {
        var f = AbsorbingManFixture.Create();
        f.Villain.TakeDamage(3);
        Hand(f.World, "01003", 2);
        f.Reveal("04089");
        f.Finish();
        Assert.True(f.World.Seats[0].IdentityCard.Ready);
        Assert.False(Statuses.Has(f.World, f.Villain, Statuses.Tough));
        Assert.Equal(3, f.Villain.Damage);
        Assert.Equal(2, f.World.Seats[0].Hand.Cards.Count);
        Assert.DoesNotContain(f.World.Cards, card => card.Area.Type == DeckType.BoostCardsDeck);
    }
}

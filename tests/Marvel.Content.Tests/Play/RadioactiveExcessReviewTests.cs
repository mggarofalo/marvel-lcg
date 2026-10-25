using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class RadioactiveExcessReviewTests
{
    [Theory]
    [InlineData(false, false, 5)]
    [InlineData(false, true, 0)]
    [InlineData(true, false, 5)]
    [InlineData(true, true, 0)]
    [Rule("rr:excess-damage")]
    [Rule("rr:overkill.3")]
    [Rule("rr:tough.2")]
    public void DirectAndAttackDamageUsePreplacementHealthAndNeverDoubleRoute(
        bool attack, bool tough, int expectedThreat)
    {
        // Synthetic six-damage assignment exercises the constant instruction's
        // unqualified "Excess damage dealt by Thunderball" independently of ATK.
        // Excess is damage "beyond that character's remaining hit points";
        // Tough must "prevent all of that damage".
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07022", 1);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var thunderball = groups[1].Villain;
        var ally = world.CreateCard("01083", world.AreaOf(
            DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        ally.TakeDamage(2);
        World.MoveToTop(source, world.AreaOf(DeckType.UpgradesArea,
            thunderball.Area.PlayArea, thunderball.ObjectId));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            ExcessDamageThreat.Modifier, Card: source.ObjectId, Affects: thunderball.ObjectId));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Keywords.Overkill, 1, Affects: thunderball.ObjectId));
        if (tough) Statuses.Give(world, ally, Statuses.Tough);
        List<GameEvent> events = [];

        DamageResult? direct = null;
        if (attack)
            DamageAttacks.Resolve(world, world.Facts,
                new(thunderball, thunderball, ally, 6, "review", "Attack", Retaliate: false), events);
        else
            direct = DamagePlacement.Resolve(world, world.Facts,
                new(thunderball, ally, 6, "review", "Damage"), events);
        WreckingSchemeFixture.Finish(world, runner, events);

        if (direct is not null)
            Assert.Equal(tough ? Damage.Outcome.NotDefeated : Damage.Outcome.Defeated, direct.Outcome);
        Assert.Equal(expectedThreat, groups[1].Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.All(groups.Where(group => group != groups[1]), group =>
            Assert.Equal(0, group.Scheme.Tokens.GetValueOrDefault("k_threat")));
        Assert.Equal(tough ? DeckType.AlliesArea : DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(tough ? 0 : 1, events.OfType<FieldSet>().Count(item =>
            item.Card == groups[1].Scheme.ObjectId && item.Field == "k_threat"));
    }
}

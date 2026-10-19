using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ScopedAttackKeywordTests
{
    private const string Book = """
        {"cards":[
          {"card":"04012","abilities":[{"trigger":{"timing":"Constant"},"effect":{"grant":{"card":"this","keyword":"piercing","attackScope":"basic"}}}]},
          {"card":"04020","abilities":[{"trigger":{"timing":"Constant"},"effect":{"grant":{"card":"this","keyword":"ranged","attackScope":"basic"}}}]},
          {"card":"04002","abilities":[{"trigger":{"timing":"Constant"},"effect":{"grant":{"card":"you","keyword":"ranged","attackScope":{"sourceTrait":"ARROW"}}}}]},
          {"card":"04009","abilities":[{"trigger":{"timing":"Constant"},"effect":{"grant":{"card":"this","keyword":"piercing","attackScope":"source"}}}]}
        ]}
        """;

    [Theory]
    [InlineData(true, 2)]
    [InlineData(false, 0)]
    [Rule("rr:piercing.1")]
    public void BlackKnightPiercesOnlyWithHisBasicAttack(bool basic, int damage)
    {
        // "Before this attack deals damage ... discard each tough status card."
        var runner = new AbilityRunner(AbilityCatalog.Parse(Book));
        var (_, world) = Playing(_ => { }, abilities: runner);
        var ally = InPlay(world, "04012");
        var target = world.TheCardIn(DeckType.VillainArea)!;
        Statuses.Give(world, target, Statuses.Tough);
        Assert.False(Keywords.Has(world, ally, Keywords.Piercing, world.Facts));
        if (basic)
        {
            Assert.Contains("Piercing", Damage.PreviewBasicAttack(world, world.Facts, ally, target, 2));
            BasicPowers.AllyPower(world, world.Facts, ally, target, BasicPowers.AttackVerb, []);
            Agendas.Finish(world, world.Facts, runner);
        }
        else
        {
            Assert.Contains("Tough prevents", Damage.PreviewAttack(world, world.Facts, ally, ally, target, 2));
            DamageAttacks.Attack(world, world.Facts, ally, target, 2, "test", "Attack", []);
        }
        Assert.Equal(damage, target.Damage);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    [Rule("rr:ranged.1")]
    public void WarMachineIgnoresRetaliateOnlyOnBasicAttack(bool basic, int damage)
    {
        // "This attack ignores the retaliate keyword."
        var runner = new AbilityRunner(AbilityCatalog.Parse(Book));
        var (_, world) = Playing(_ => { }, abilities: runner);
        var ally = InPlay(world, "04020");
        var target = world.TheCardIn(DeckType.VillainArea)!;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "retaliate", 2, Affects: target.ObjectId));
        Assert.False(Keywords.Has(world, ally, Keywords.Ranged, world.Facts));
        if (basic)
        {
            Assert.Contains("Ranged ignores", Damage.PreviewBasicAttack(world, world.Facts, ally, target, 2));
            BasicPowers.AllyPower(world, world.Facts, ally, target, BasicPowers.AttackVerb, []);
            Agendas.Finish(world, world.Facts, runner);
        }
        else DamageAttacks.Attack(world, world.Facts, ally, target, 2, "test", "Attack", []);
        Assert.Equal(damage, ally.Damage);
    }

    [Theory]
    [InlineData("04009", true)]
    [InlineData("01005", false)]
    public void BowRangedFollowsArrowSourceAndEndsWhenBowLeaves(string face, bool ranged)
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse(Book));
        var (_, world) = Playing(_ => { }, abilities: runner);
        var hero = world.Seats[0].IdentityCard;
        var bow = InPlay(world, "04002");
        var source = world.CreateCard(face, world.Seats[0].Hand);
        var target = world.TheCardIn(DeckType.VillainArea)!;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "retaliate", 1, Affects: target.ObjectId));
        Assert.Equal(ranged, AttackKeywords.Has(world, hero, source, Keywords.Ranged));
        Assert.False(AttackKeywords.Has(world, hero, hero, Keywords.Ranged, basic: true));
        Assert.Equal(ranged, Damage.PreviewAttack(world, world.Facts, hero, source, target, 1).Contains("Ranged ignores"));
        DamageAttacks.Attack(world, world.Facts, hero, source, target, 1, "test", "Attack", []);
        Assert.Equal(ranged ? 0 : 1, hero.Damage);
        Discard.Card(world, bow, "test", []);
        Assert.False(AttackKeywords.Has(world, hero, source, Keywords.Ranged));
    }

    [Fact]
    public void VibraniumPiercingBelongsToItsOwnEventInPreviewAndDamage()
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse(Book));
        var (_, world) = Playing(_ => { }, abilities: runner);
        var hero = world.Seats[0].IdentityCard;
        var source = world.CreateCard("04009", world.Seats[0].Hand);
        var other = world.CreateCard("01005", world.Seats[0].Hand);
        var target = world.TheCardIn(DeckType.VillainArea)!;
        Statuses.Give(world, target, Statuses.Tough);
        Assert.Contains("Piercing", Damage.PreviewAttack(world, world.Facts, hero, source, target, 6));
        Assert.DoesNotContain("Piercing", Damage.PreviewAttack(world, world.Facts, hero, other, target, 6));
        DamageAttacks.Attack(world, world.Facts, hero, source, target, 6, "test", "Attack", []);
        Assert.Equal(6, target.Damage);
        Assert.False(Keywords.Has(world, hero, Keywords.Piercing, world.Facts));
    }
    [Theory]
    [InlineData("basic", "all")]
    [InlineData("piercing", "attack")]
    [InlineData("ARROW", "")]
    public void InvalidAttackRestrictionFailsAdmission(string from, string to)
    {
        Assert.Throws<AbilityException>(() => new AbilityRunner(AbilityCatalog.Parse(Book.Replace(from, to))));
    }

    [Fact]
    public void AttackRestrictionCannotBeSilentlyRunAsAnAction()
    {
        string book = Book.Replace("\"timing\":\"Constant\"", "\"timing\":\"Action\",\"event\":\"WhenActionTriggered\"");
        Assert.Throws<AbilityException>(() => new AbilityRunner(AbilityCatalog.Parse(book)));
    }
}

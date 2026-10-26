using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class FinalDamageTriggerReviewTests : CoreCardTestBase
{
    [Theory]
    [InlineData("01003", "01001a")]
    [InlineData("01017", "01010a")]
    [InlineData("03005", "03001a")]
    [InlineData("05005", "05001a")]
    [Rule("rr:prevent.1.1")]
    public void ActualTakenCardStopsOfferingAfterFullRecipientPrevention(string face, string identity)
    {
        // "the amount of damage that character takes is reduced", while dealt is unchanged.
        var world = Hero(identity);
        var hero = world.Seats[0].IdentityCard;
        var source = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var card = world.CreateCard(face, face == "01017"
            ? world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0)
            : world.Seats[0].Hand);
        if (face == "03005")
            world.CreateCard("03009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("01088", world.Seats[0].Deck);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var replacement = DamagePlacement.Replace(world, new(source, hero, 2, "Review", "Attack"), []);
        var occurrence = replacement.Execution.Occurrence;
        Assert.Contains(runner.Waiting(world, occurrence, WindowKind.Interrupt), item => item.Card == card.ObjectId);
        world.Effects.Register(new(EffectSource.LastingEffect, "preventDamage", 1,
            Affects: hero.ObjectId, Lasts: Duration.NextUses(1)));
        Assert.Contains(runner.Waiting(world, occurrence, WindowKind.Interrupt), item => item.Card == card.ObjectId);
        var stale = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Interrupt), item => item.Card == card.ObjectId);
        world.Effects.Register(new(EffectSource.LastingEffect, "preventDamage", 1,
            Affects: hero.ObjectId, Lasts: Duration.NextUses(1)));
        Assert.DoesNotContain(runner.Waiting(world, occurrence, WindowKind.Interrupt), item => item.Card == card.ObjectId);
        string before = world.Digest().Canonical();
        Assert.Throws<RulesNotImplementedException>(() => runner.PrepareWindow(world, occurrence, stale, Decision.Take(card.ObjectId)));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(2, replacement.ResolvedAmount);
        Assert.Equal(2, world.Effects.Active().Count(effect => effect.Kind == "preventDamage"));
    }

    [Theory]
    [InlineData("", 1)]
    [InlineData(",\"damage\":\"dealt\"", 1)]
    [InlineData(",\"damage\":\"taken\"", 0)]
    [InlineData(",\"damage\":\"taken\"", 1)]
    [Rule("rr:prevent.1.1")]
    public void ForcedTimingUsesItsAuthoredDamageCondition(string qualifier, int prevention)
    {
        // Prevention reduces "takes", but "dealt" is not reduced; mandatory timing does not change that quantity.
        var world = Hero("01001a");
        var hero = world.Seats[0].IdentityCard;
        hero.TakeDamage(3);
        var source = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        // Synthetic contract only; this does not author or admit Energy Barrier.
        var upgrade = world.CreateCard("05017", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        upgrade.PlaceTokens("c_reflection", 3);
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"05017","abilities":[{
              "trigger":{"event":"WhenDamageWouldBeDealt","timing":"ForcedInterrupt","target":"you"QUALIFIER},
              "cost":{"removeCounters":{"card":"this","counter":"reflection","count":1}},
              "effect":{"heal":{"card":"you","amount":1}}
            }]}]}
            """.Replace("QUALIFIER", qualifier, StringComparison.Ordinal)));
        world.Abilities = runner;
        if (prevention > 0)
            world.Effects.Register(new(EffectSource.LastingEffect, "preventDamage", prevention,
                Affects: hero.ObjectId, Lasts: Duration.NextUses(1)));
        var replacement = DamagePlacement.Replace(world, new(source, hero, 1, "Review", "Damage"), []);
        bool suppressed = qualifier.Contains("taken", StringComparison.Ordinal) && prevention > 0;
        Assert.True(replacement.IsComplete);
        Assert.Equal(1, replacement.ResolvedAmount);
        Assert.Equal(suppressed ? 3 : 2, hero.Damage);
        Assert.Equal(suppressed ? 3 : 2, upgrade.Tokens["c_reflection"]);
        Assert.Equal(prevention, world.Effects.Active().Count(effect => effect.Kind == "preventDamage"));
    }
}

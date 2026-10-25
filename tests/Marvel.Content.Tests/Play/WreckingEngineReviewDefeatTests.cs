using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Local interpretations exercise engine contracts without admitting expansion content.</summary>
public sealed class WreckingEngineReviewDefeatTests
{
    [Theory]
    [InlineData(0, false, false)]
    [InlineData(1, false, false)]
    [InlineData(1, true, false)]
    [InlineData(1, false, true)]
    [InlineData(0, false, true)]
    [Rule("rr:defeat.1")]
    [Rule("rr:tough.2")]
    public void ThreatRequiresThisDamageToDefeatTheExactForeignPlayerAlly(
        int remaining, bool tough, bool priorDefeat)
    {
        // 07016: "If that character is defeated this way, place 3 threat on Wrecker's side scheme."
        // rr:defeat.1: "If an ally, minion, or side scheme is defeated, it is discarded."
        // rr:tough.2: "prevent all of that damage and discard a tough status card".
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07016", 0, players: 2);
        var runner = Runner();
        world.Abilities = runner;
        world.CreateCard("01088", world.Seats[1].Deck);
        if (priorDefeat)
        {
            var earlier = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            DamagePlacement.Resolve(world, world.Facts, new(earlier, earlier, 3, "test", "test"), []);
            Sequence.Finish(world, world.Facts, runner, []);
            Assert.Equal(DeckType.DiscardPile, earlier.Area.Type);
        }
        var ally = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        // remaining=0 denotes a surviving two-HP target; remaining=1 denotes lethal one-HP damage.
        int initialDamage = remaining == 0 ? 1 : 2;
        ally.TakeDamage(initialDamage);
        if (tough) Statuses.Give(world, ally, Statuses.Tough);
        runner.ExecuteReveal(world, source, 0);
        WreckingRemainingSchemeFixture.Finish(world, runner);
        bool defeated = remaining == 1 && !tough;
        Assert.Equal(defeated ? DeckType.DiscardPile : DeckType.AlliesArea, ally.Area.Type);
        if (!defeated) Assert.Equal(initialDamage + (tough ? 0 : 1), ally.Damage);
        Assert.Equal(defeated ? 3 : 0, groups[0].Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.All(groups.Skip(1), group => Assert.Equal(0, group.Scheme.Tokens.GetValueOrDefault("k_threat")));
        Assert.All(world.Seats, seat => Assert.Equal(0, seat.IdentityCard.Damage));
        Assert.Same(groups[1], world.EncounterTopology!.Active);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:defeat.1")]
    public void DefeatedAllyReturnedByItsForcedInterruptStillPlacesThreat(bool reenter)
    {
        // ruling:28d343ac007bd784: "he is still defeated even if you return him to your hand".
        // A synthetic self-defeat Forced Interrupt probes the retained receipt, without Red Dagger admission.
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07016", 0);
        var runner = Runner(reenter ? """{"seq":[{"returnToHand":"this"},{"putIntoPlay":{"card":"this","where":"yourPlayArea"}}]}"""
            : """{"returnToHand":"this"}""");
        world.Abilities = runner;
        var ally = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        ally.TakeDamage(2);
        int incarnation = ally.Incarnation;
        runner.ExecuteReveal(world, source, 0);
        WreckingRemainingSchemeFixture.Finish(world, runner);
        Assert.Equal(reenter ? DeckType.AlliesArea : DeckType.HandsArea, ally.Area.Type);
        Assert.Equal(incarnation + (reenter ? 1 : 0), ally.Incarnation);
        Assert.Equal(0, ally.Damage);
        Assert.Equal(3, groups[0].Scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    private static AbilityRunner Runner(string? defeatEffect = null)
    {
        if (defeatEffect is null) return AuthoredCards.Runner();
        var local = AbilityCatalog.Parse("""
        {"cards":[{"card":"01083","abilities":[{
          "trigger":{"event":"WhenCardDefeated","timing":"ForcedInterrupt","subject":"this"},
          "effect":DEFEAT_EFFECT_JSON}]}]}
        """.Replace("DEFEAT_EFFECT_JSON", defeatEffect, StringComparison.Ordinal));
        return new(AuthoredCards.Book with
        {
            Abilities = [.. AuthoredCards.Book.Abilities.Where(row => row.Card != "01083"), .. local.Abilities],
            Authored = AuthoredCards.Book.Authored.Concat(local.Authored).ToHashSet(StringComparer.Ordinal),
        });
    }
}

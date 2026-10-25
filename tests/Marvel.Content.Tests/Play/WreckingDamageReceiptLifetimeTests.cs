using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic sequences probe the lifetime of one direct-damage receipt.</summary>
public sealed class WreckingDamageReceiptLifetimeTests
{
    [Theory]
    [InlineData(null, 3)]
    [InlineData("minions", 0)]
    [InlineData("villain", 0)]
    [Rule("rr:defeat.1")]
    public void SubsequentDirectDamageReplacesTheReceiptEvenWithoutRecipients(string? subsequentQuery, int expectedThreat)
    {
        // rr:defeat.1: "If an ally, minion, or side scheme is defeated, it is discarded."
        // Engine choice: wasDefeatedByDamage reads the most recent direct damage node,
        // including an empty node. This synthetic sequence is not printed 07016 text.
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07016", 0);
        var ally = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        ally.TakeDamage(2);
        string nextDamage = subsequentQuery is null ? "" :
            """,{"dealDamage":{"cards":{"query":"QUERY_JSON"},"amount":1}}"""
                .Replace("QUERY_JSON", subsequentQuery, StringComparison.Ordinal);
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
        {"cards":[{"card":"07016","abilities":[{
          "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
          "effect":{"chooseCard":{"chooser":"firstPlayer",
            "from":{"minBy":{"of":{"query":"heroesAndAllies"},"by":"remainingHealth"}},
            "effect":{"seq":[{"dealDamage":{"cards":"chosen","amount":1}}NEXT_DAMAGE_JSON,
              {"if":{"test":{"wasDefeatedByDamage":"chosen"},
                "then":{"placeThreat":{"scheme":{"titled":"Day of Reckoning"},"amount":3}}}}
            ]}}}
        }]}]}
        """.Replace("NEXT_DAMAGE_JSON", nextDamage, StringComparison.Ordinal)));
        world.Abilities = runner;

        runner.ExecuteReveal(world, source, 0);
        WreckingRemainingSchemeFixture.Finish(world, runner);

        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(expectedThreat, groups[0].Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.All(groups.Skip(1), group => Assert.Equal(0, group.Scheme.Tokens.GetValueOrDefault("k_threat")));
        Assert.All(groups, group => Assert.Equal(subsequentQuery == "villain" && group == groups[1] ? 1 : 0,
            group.Villain.Damage));
    }

    [Fact]
    [Rule("rr:leaves-play.1")]
    public void ReselectingTheReturnedPhysicalCardCannotReuseItsPreviousIncarnationReceipt()
    {
        // rr:leaves-play.1: "considered to be a new copy of that card".
        // A synthetic own-defeat ability returns and reenters the ally. The next
        // choice names the new copy; the earlier damage defeated only the old one.
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07016", 0);
        var ally = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        ally.TakeDamage(2);
        int initialIncarnation = ally.Incarnation;
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
        {"cards":[{"card":"07016","abilities":[{
          "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
          "effect":{"chooseCard":{"chooser":"firstPlayer",
            "from":{"minBy":{"of":{"query":"heroesAndAllies"},"by":"remainingHealth"}},
            "effect":{"seq":[{"dealDamage":{"cards":"chosen","amount":1}},
              {"chooseCard":{"chooser":"firstPlayer",
                "from":{"minBy":{"of":{"query":"heroesAndAllies"},"by":"remainingHealth"}},
                "effect":{"if":{"test":{"wasDefeatedByDamage":"chosen"},
                  "then":{"placeThreat":{"scheme":{"titled":"Day of Reckoning"},"amount":3}}}}}}
            ]}}}
        }]},{"card":"01083","abilities":[{
          "trigger":{"event":"WhenCardDefeated","timing":"WhenDefeated","subject":"this"},
          "effect":{"seq":[{"returnToHand":"this"},{"putIntoPlay":{"card":"this","where":"yourPlayArea"}}]}
        }]}]}
        """));
        world.Abilities = runner;

        runner.ExecuteReveal(world, source, 0);
        WreckingRemainingSchemeFixture.Finish(world, runner);

        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(initialIncarnation + 1, ally.Incarnation);
        Assert.Equal(0, ally.Damage);
        Assert.All(groups, group => Assert.Equal(0, group.Scheme.Tokens.GetValueOrDefault("k_threat")));
    }
}

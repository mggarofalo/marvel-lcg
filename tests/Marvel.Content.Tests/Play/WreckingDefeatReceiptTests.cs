using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Local-book 07016 contract; public scenario admission remains closed.</summary>
public sealed class WreckingDefeatReceiptTests
{
    [Theory]
    [InlineData(1, false, 0)]
    [InlineData(2, true, 0)]
    [InlineData(2, false, 3)]
    [Rule("rr:defeat")]
    public void ExactDamageDefeatControlsThreatAcrossPlayers(int damage, bool tough, int threat)
    {
        // 07016: "If that character is defeated this way, place 3 threat on Wrecker's side scheme."
        // rr:defeat: "If a character has zero or fewer remaining hit points ... it is defeated."
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07016", 0, players: 2);
        // Keep the defeated ally in its owner's discard rather than recycling an empty deck.
        world.CreateCard("01088", world.Seats[1].Deck);
        var ally = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        ally.TakeDamage(damage);
        if (tough) Statuses.Give(world, ally, Statuses.Tough);
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
        {"cards":[{"card":"07016","abilities":[{
          "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
          "effect":{"chooseCard":{"chooser":"firstPlayer",
            "from":{"minBy":{"of":{"query":"heroesAndAllies"},"by":"remainingHealth"}},
            "effect":{"seq":[{"dealDamage":{"cards":"chosen","amount":1}},
              {"if":{"test":{"wasDefeatedByDamage":"chosen"},
                "then":{"placeThreat":{"scheme":{"titled":"Day of Reckoning"},"amount":3}}}}
            ]}}}
        }]}]}
        """));
        world.Abilities = runner;
        runner.ExecuteReveal(world, source, 0);
        for (int i = 0; ; i++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) break;
            Assert.True(i < 12);
            Assert.True(prompt.Cancellable, prompt.Description);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, []);
        }
        Assert.Equal(threat, groups[0].Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(threat > 0 ? DeckType.DiscardPile : DeckType.AlliesArea, ally.Area.Type);
        Assert.All(world.Seats, seat => Assert.Equal(0, seat.IdentityCard.Damage));
    }
}

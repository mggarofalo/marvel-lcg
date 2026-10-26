using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class MutagenReviewProbeTests
{
    [Theory]
    [InlineData("eachTime", -1)]
    [InlineData("forEachDiscarded", -1)]
    [InlineData("eachTime", 0)]
    [InlineData("forEachDiscarded", 0)]
    [InlineData("eachTime", 1)]
    [InlineData("forEachDiscarded", 1)]
    public void DiscardCountIsValidatedBeforeMutation(string operation, int count)
    {
        // Count validation is an engine contract; both traversal timings reject invalid counts.
        var runner = new AbilityRunner(AbilityCatalog.Parse($$$$"""
            {"cards":[{"card":"02032","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
              "effect":{"{{{{operation}}}}":{
                "effect":{"discardTop":{"from":"encounterDeck","count":{{{{count}}}}}},
                "when":{"titleInPlay":"Spider-Man"},
                "then":{"placeCounters":{"card":"you","counter":"body","count":1}}
              }}
            }]}]}
            """));
        var world = new World(CardCatalogData, players: 1) { Abilities = runner };
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01001a,01001b", seat.Hero);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        world.CreateCard("02024", deck);
        world.CreateCard("02024", deck);
        var source = world.CreateCard("02032", world.AreaOf(DeckType.RevealingArea));
        var before = System.Text.Json.JsonSerializer.Serialize(world.Digest());
        if (count < 0)
        {
            Assert.Throws<AbilityException>(() => runner.ExecuteReveal(world, source, 0));
            Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(world.Digest()));
        }
        else runner.ExecuteReveal(world, source, 0);
        Assert.Equal(2 - Math.Max(0, count), deck.Cards.Count);
        Assert.Equal(Math.Max(0, count), seat.IdentityCard.Tokens.GetValueOrDefault("c_body"));
    }

    [Theory]
    [InlineData("eachTime")]
    [InlineData("forEachDiscarded")]
    public void UnsupportedPredecessorRejectsBeforeMutation(string operation)
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse($$$$"""
            {"cards":[{"card":"02032","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
              "effect":{"{{{{operation}}}}":{
                "effect":{"placeCounters":{"card":"you","counter":"probe","count":1}},
                "when":{"titleInPlay":"Spider-Man"},
                "then":{"placeCounters":{"card":"you","counter":"body","count":1}}
              }}
            }]}]}
            """));
        var world = new World(CardCatalogData, players: 1) { Abilities = runner };
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01001a,01001b", seat.Hero);
        var source = world.CreateCard("02032", world.AreaOf(DeckType.RevealingArea));
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteReveal(world, source, 0));
        Assert.Equal(0, seat.IdentityCard.Tokens.GetValueOrDefault("c_probe"));
    }
}

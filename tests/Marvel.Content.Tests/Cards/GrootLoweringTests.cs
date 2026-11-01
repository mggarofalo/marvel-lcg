using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Cards;

// Malformed shapes of the vocabulary the Groot pack added are refused before gameplay.
public sealed class GrootLoweringTests
{
    private const string Action =
        """{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"}""";
    private const string Damage =
        """{"event":"WhenDamageWouldBeDealt","damage":"taken","timing":"ForcedInterrupt","target":"this"}""";

    [Theory]
    [InlineData(Damage, """{"preventDamageWithCounters":{"card":"this"}}""", "counter")]
    [InlineData(Damage, """{"preventDamageWithCounters":{"counter":"growth"}}""", "card")]
    [InlineData(Damage, """{"preventDamageWithCounters":{"card":"this","counter":"growth","amount":1}}""", "amount")]
    [InlineData(Damage, """{"preventDamage":{"card":"trigger.target","amount":{"incomingDamage":"game"}}}""", "trigger")]
    [InlineData(Action, """{"reduceNextCardCost":{"player":"you","amount":1,"until":"EndOfRound"}}""", "EndOfTurn")]
    [InlineData(Action, """{"reduceNextCardCost":{"player":"any","amount":1,"until":"EndOfTurn"}}""", "phase")]
    public void NewVocabularyRefusesAnyOtherShape(string trigger, string effect, string says)
    {
        var failure = Assert.Throws<AbilityException>(() => AbilityLowering.Book(AbilityCatalog.Parse(
            $$$"""{"cards":[{"card":"16001a","abilities":[{"trigger":{{{trigger}}},"effect":{{{effect}}}}]}]}""")));
        Assert.Contains(says, failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void APlayerReductionCarriesItsTraitAndTurnExpiry()
    {
        var program = AbilityLowering.Book(AbilityCatalog.Parse(
            """{"cards":[{"card":"16024","abilities":[{"trigger":""" + Action
            + ""","effect":{"reduceNextCardCost":{"player":"you","trait":"SUPERPOWER","amount":1,"until":"EndOfTurn"}}}]}]}"""));
        var reduction = Assert.IsType<AbilityPricingEffect.ReduceNextCardCost>(Assert.Single(program.On("16024")).Effect);
        Assert.Equal("SUPERPOWER", reduction.Filter!.Trait);
        Assert.Equal(Marvel.Rules.Timing.TimingPoints.EndOfTurn, reduction.Until);
        Assert.False(reduction.AnyPlayer);
    }

    [Fact]
    public void FloraAndFaunaAuthorsTheRocketOptionWhenARocketUpgradeIsAuthored()
    {
        // 16020/16048: "... or place 2 charge counters on a Rocket Raccoon upgrade and
        // ready that upgrade." Only an authored Rocket Raccoon upgrade can be in play;
        // until one is, the row authors the Groot option alone.
        var book = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        var rows = book["cards"]!.AsArray().Select(row => row!).ToList();
        string[] rocketUpgrades = ["16034", "16035", "16036", "16037", "16038", "16039"];
        bool rocketAuthored = rows.Any(row => rocketUpgrades.Contains((string?)row["card"]));
        var flora = rows.Single(row => (string?)row["card"] == "16020");
        bool chooses = flora["abilities"]!.ToJsonString().Contains("\"choose\"", StringComparison.Ordinal);
        Assert.Equal(rocketAuthored, chooses);
    }
}

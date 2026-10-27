using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic admitted compositions test description contracts without authoring pack content.
public sealed class BoundedDamageDescriptionTests
{
    private const string Group = """{"dealDamage":{"cards":"chosen","amount":3,"maximum":1}}""";

    [Theory]
    [InlineData("direct-option")]
    [InlineData("targeted-option")]
    [InlineData("card-choice")]
    [InlineData("sequence")]
    [InlineData("conditional")]
    public void SummariesIdentifyTheLaterSelectionWithoutPromisingCandidateDamage(string shape)
    {
        string choosing = "{\"chooseCard\":{\"from\":{\"query\":\"villain\"},\"effect\":" + Group + "}}";
        string effect = shape switch
        {
            "direct-option" => "{\"choose\":{\"options\":[" + Group.Replace("\"chosen\"", "\"you\"", StringComparison.Ordinal)
                + ",{\"draw\":{\"player\":\"you\",\"count\":1}}]}}",
            "targeted-option" => "{\"choose\":{\"options\":[" + choosing + ",{\"draw\":{\"player\":\"you\",\"count\":1}}]}}",
            "sequence" => choosing.Replace(Group, "{\"seq\":[" + Group + ",{\"draw\":{\"player\":\"you\",\"count\":1}}]}", StringComparison.Ordinal),
            "conditional" => choosing.Replace(Group,
                "{\"if\":{\"test\":{\"finalStep\":\"true\"},\"then\":" + Group + ",\"else\":" + Group + "}}", StringComparison.Ordinal),
            _ => choosing,
        };
        var runner = Runner("01006", "Action", effect);
        Card? source = null;
        var (_, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        string before = Snapshot(world);
        var prompt = Assert.IsType<Prompt>(runner.PreviewChoice(world, source!, 0, 0, null, false));
        var offer = prompt.Affordances[0];
        Assert.Contains("up to 1", offer.Description);
        Assert.Contains("each chosen recipient", offer.Description);
        Assert.Contains("choosing none", offer.Description);
        Assert.DoesNotContain("HP", offer.Description);
        Assert.DoesNotContain("Current damage", offer.Description);
        if (shape == "card-choice") Assert.Contains("after selecting", offer.CommitLabel);
        if (shape.EndsWith("option", StringComparison.Ordinal))
        {
            Assert.Contains("up to 1", offer.DisplayLabel);
            Assert.DoesNotContain("Take 3 damage", offer.DisplayLabel);
        }
        Assert.Equal(before, Snapshot(world));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void PlayerChoicePreservesDirectVillainDamageAndDistinguishesItsEngagedGroup(bool bounded, bool villainBounded)
    {
        string maximum = bounded ? ",\"maximum\":1" : string.Empty;
        var runner = Runner("01006", "Action", """
            {"chooseCard":{"from":{"query":"identities"},"effect":{"seq":[
              {"dealDamage":{"cards":{"query":"villain"},"amount":1VILLAIN_MAXIMUM}},
              {"dealDamage":{"cards":{"query":"enemiesEngagedWithChosenPlayer"},"amount":3MAXIMUM}}
            ]}}}
            """.Replace("VILLAIN_MAXIMUM", villainBounded ? ",\"maximum\":1" : string.Empty, StringComparison.Ordinal)
                .Replace("MAXIMUM", maximum, StringComparison.Ordinal));
        Card? source = null;
        var (_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01184", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: runner);
        var prompt = Assert.IsType<Prompt>(runner.PreviewChoice(world, source!, 0, 0, null, false));
        var offer = Assert.Single(prompt.Affordances);
        if (villainBounded) Assert.Contains("choose whether to deal 1 damage to the villain", offer.Description);
        else Assert.Contains("Deal 1 damage to the villain", offer.Description);
        if (bounded)
        {
            Assert.Contains("up to 1", offer.Description);
            Assert.Contains("each chosen enemy", offer.Description);
            Assert.Contains("later", offer.CommitLabel);
            Assert.DoesNotContain("to each enemy engaged", offer.Description);
        }
        else
        {
            Assert.Contains(villainBounded ? "3 damage to each enemy engaged" : "3 to each enemy engaged", offer.Description);
            Assert.DoesNotContain("up to", offer.Description);
            if (!villainBounded) Assert.DoesNotContain("later", offer.CommitLabel);
        }
    }

    [Fact]
    public void SourceActionSummaryRetainsTheCompiledLaterSelection()
    {
        var (runner, fields) = MutableEffectRunner("dealDamage",
            """{"cards":{"query":"minionsEngagedWithYou"},"amount":3,"maximum":1}""", false);
        Card? source = null;
        var (_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01184", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: runner);
        fields["maximum"] = new AbilityValue.Number(2);
        string before = Snapshot(world);
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == source!.ObjectId);
        var offer = runner.Describe(world, action);
        Assert.True(offer.DeferredTargetSelection);
        Assert.Contains("up to 1", offer.Description);
        Assert.Contains("each chosen recipient", offer.Description);
        Assert.DoesNotContain("up to 2", offer.Description);
        Assert.Equal(before, Snapshot(world));
    }

    private static string Snapshot(World world) => JsonSerializer.Serialize(new
    {
        Cards = world.Digest().Canonical(), Rng = world.Random.Generator.GetState(),
        world.Random.Generator.WordsConsumed, Pending = world.Agenda.Outstanding,
    });
}

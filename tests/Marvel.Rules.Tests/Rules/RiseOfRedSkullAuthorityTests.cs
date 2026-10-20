using System.Text.Json;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Rules;

/// <summary>Protects reviewed PDF facts that typography-only extraction can omit.</summary>
public sealed class RiseOfRedSkullAuthorityTests
{
    [Fact]
    public void PersistentDamageResolvesThroughThePinnedCorrectionWithoutBecomingAnRrRule()
    {
        var corpus = Marvel.Rules.Index.Corpus.Read();
        const string basis = "pack:mc10:persistent-damage";

        var published = Assert.IsType<Marvel.Rules.Index.RuleRecord>(corpus.Find(basis));
        var effective = corpus.Resolve(basis, "1.8");

        Assert.Equal("pack", published.Kind);
        Assert.Equal("ruling:1516351f87fc4382", effective.Id);
        Assert.Contains("the extra 1-4 hit points", effective.Fragment, StringComparison.Ordinal);
        Assert.DoesNotContain(basis, RuleCitations.Citable);
        Assert.Single(corpus.ReferencedBy(basis), edge => edge.From == effective.Id);
    }

    [Theory]
    [InlineData("crossbones", 5)]
    [InlineData("absorbing-man", 7)]
    [InlineData("taskmaster", 10)]
    [InlineData("zola", 12)]
    [InlineData("red-skull", 15)]
    public void EveryScenarioKeepsSeparateSetupAndVictoryInstructions(string scenario, int page)
    {
        using var index = JsonDocument.Parse(File.ReadAllBytes(
            RepositoryPaths.Dataset("rules-packs", "index.json")));
        var entries = index.RootElement.GetProperty("entries").EnumerateArray().ToList();
        string prefix = $"pack:mc10:{scenario}-campaign-instructions";

        foreach (string phase in new[] { "setup", "victory" })
        {
            var record = Assert.Single(entries, entry => entry.GetProperty("id").GetString() == $"{prefix}.{phase}");
            Assert.Equal(page, record.GetProperty("page").GetInt32());
            Assert.Equal("mc10_the_rise_of_red_skull_rules_web.pdf", record.GetProperty("source").GetString());
        }
    }

    [Theory]
    [InlineData("crossbones-campaign-instructions", "one of the TECH upgrades")]
    [InlineData("crossbones-campaign-instructions", "each EXPERIMENTAL attachment")]
    [InlineData("scenario-3-taskmaster", "Remove Taskmaster (I) and add Taskmaster (III) for expert mode.")]
    [InlineData("red-skull-campaign-instructions", "X[per-player] instead if you are playing an expert campaign")]
    [InlineData("red-skull-campaign-instructions", "Expert Campaign Only: If the players lose this game")]
    [InlineData("persistent-damage", "the defeated player does not participate in any of the victory steps")]
    [InlineData("obligations-in-player-decks", "They do not draw a card to replace that obligation.")]
    [InlineData("cards-added-to-decks", "do not count toward a player’s minimum or maximum deck size")]
    public void ReviewedInstructionsPreserveTraitsExceptionsAndContinuation(string section, string instruction)
    {
        // FFG RoRS rulebook, printed pages 3, 5, 10, 15 and 17: these
        // qualifiers decide behavior and must survive transcription intact.
        string text = File.ReadAllText(RepositoryPaths.Dataset("rules-packs", "mc10", section + ".md"));
        Assert.Contains(instruction, text, StringComparison.Ordinal);
    }
}

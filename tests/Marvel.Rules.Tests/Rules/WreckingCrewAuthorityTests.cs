using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Rules;

/// <summary>Source completeness controls; these do not certify scenario execution.</summary>
public sealed class WreckingCrewAuthorityTests
{
    [Fact]
    public void SetupPreservesThePrintedExclusionOfHeroEncounterCards()
    {
        // Authority: pack:mc03:additional-setup.prepare-encounter-decks, p.4.
        string text = Read("additional-setup.md");
        Assert.Contains("Nemesis cards and obligations are not used", text);
        Assert.Contains("encounter deck of 15 cards", text);
    }

    [Fact]
    public void BasicTargetsAreNotRestrictedToTheActiveVillain()
    {
        // Authority: pack:mc03:new-rules.the-active-villain, p.6 note.
        string text = Read("new-rules.md");
        Assert.Contains("Players may attack any villain or thwart any scheme", text);
        Assert.Contains("regardless of which villain is the active villain", text);
        Assert.Contains("In case of a tie, the first player decides", text);
    }

    [Fact]
    public void ThePageBreakRetainsVictoryAndSurvivingEncounterCards()
    {
        // Authority: pack:mc03:new-rules.multiple-villains-and-encounter-decks, p.7.
        string text = Read("new-rules.md");
        Assert.Contains("If the players defeat all 4 villains, they win the game!", text);
        Assert.Contains("that villain’s deck that are in play remain in play", text);
    }

    private static string Read(string file) =>
        File.ReadAllText(RepositoryPaths.Dataset("rules-packs", "mc03", file));
}

using System.Text;
using System.Text.Json.Nodes;
using Marvel.Rules.Campaign;
using Marvel.Session;
using Xunit;

namespace Marvel.Session.Tests;

public sealed class CampaignSaveTests
{
    [Fact]
    public void CampaignArchiveRoundTripsItsExactPendingChoiceAndBaseDeck()
    {
        var save = Save();
        byte[] bytes = CampaignSaveJson.Write(save);
        var loaded = CampaignSaveJson.Read(bytes);
        Assert.Equal(bytes, CampaignSaveJson.Write(loaded));
        Assert.Equal("hero", Assert.Single(loaded.State.Players).Hero);
        Assert.Equal("signature", Assert.Single(loaded.State.Players[0].Deck.Cards));
        Assert.Equal(CampaignPhase.Preparing, loaded.State.Phase);
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("missing")]
    [InlineData("unknown")]
    [InlineData("future")]
    public void AmbiguousAndUnsupportedArchivesAreRejected(string alteration)
    {
        string json = Encoding.UTF8.GetString(CampaignSaveJson.Write(Save()));
        string modified = alteration switch
        {
            "duplicate" => json.Replace("\"expert\":false", "\"expert\":false,\"expert\":true", StringComparison.Ordinal),
            "missing" => json.Replace("\"expert\":false,", "", StringComparison.Ordinal),
            "unknown" => "{\"unexpected\":0," + json[1..],
            "future" => json.Replace("\"schema\":1", "\"schema\":2", StringComparison.Ordinal),
            _ => throw new InvalidOperationException(),
        };
        Assert.NotEqual(json, modified);
        Assert.Throws<SessionSaveException>(() => CampaignSaveJson.Read(Encoding.UTF8.GetBytes(modified)));
    }

    [Fact]
    public void ReplayRejectsAnEditedCampaignEvenWhenJsonAndChecksumsCouldBeValid()
    {
        var save = Save();
        var tampered = save with { State = save.State with { Stage = CampaignStage.RedSkull } };
        Assert.Throws<ReplayDivergenceException>(() => CampaignReplay.Verify(tampered, Definition(),
            save.Compatibility, _ => throw new InvalidOperationException("no attempt should be opened"),
            _ => throw new InvalidOperationException("no game should be inspected")));
        Assert.Equal(CampaignSaveJson.Canonical(save.State), CampaignSaveJson.Canonical(CampaignReplay.Verify(save, Definition(), save.Compatibility,
            _ => throw new InvalidOperationException("no attempt should be opened"),
            _ => throw new InvalidOperationException("no game should be inspected"))));
    }

    [Fact]
    public void AcceptedModeAndDeckCommandsReplayWithoutCouplingDifficulty()
    {
        var save = Save();
        var state = CampaignRules.SelectScenarioMode(save.State, true);
        state = CampaignRules.ReplaceDeck(state, 0, new(["changed"]));
        save = save with { State = state, Revision = 2,
            Journal = [new("mode", Expert: true), new("deck", Seat: 0, Deck: new(["changed"]))] };
        var loaded = CampaignSaveJson.Read(CampaignSaveJson.Write(save));
        var verified = CampaignReplay.Verify(loaded, Definition(), save.Compatibility,
            _ => throw new InvalidOperationException("no game"), _ => throw new InvalidOperationException("no game"));
        Assert.True(verified.ExpertScenario);
        Assert.False(verified.Expert);
        Assert.Equal("changed", Assert.Single(verified.Players[0].Deck.Cards));
    }

    private static CampaignDefinition Definition() => new("trors",
        [.. Enumerable.Range(0, 5).Select(index => new CampaignScenario($"s{index}", $"e{index}"))],
        [], [], ["o1", "o2", "o3", "o4"], [], []);

    private static CampaignSave Save() => CampaignSave.Create(
        new("1.0.0", "replay", "rng", "digest", new('a', 64), new('b', 64), new('c', 64)),
        new(new string('d', 32), "campaign", "active"),
        new("trors", false, 42, [new("hero", new(["signature"]), 10)]), Definition());
}

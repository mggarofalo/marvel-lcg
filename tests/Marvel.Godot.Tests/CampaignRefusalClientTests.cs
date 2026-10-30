using Marvel.Client;
using Marvel.Desktop;
using Marvel.Protocol;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class CampaignRefusalClientTests
{
    [Fact]
    public void ARefusedCampaignIsRememberedAndNeverReopenedAutomatically()
    {
        var first = Connection("first");
        var second = Connection("second");
        var refused = first.Rejected(Rejection("save_incompatible"));
        var ordinary = second.Rejected(Rejection("revision_conflict"));

        Assert.True(refused.Refused);
        Assert.False(ordinary.Refused);
        Assert.True(refused.Rejected(Rejection("revision_conflict")).Refused);
        Assert.Equal(1, CampaignConnection.FirstResumable([refused, ordinary]));
        Assert.Equal(0, CampaignConnection.FirstResumable([ordinary, refused]));
        Assert.Equal(-1, CampaignConnection.FirstResumable([refused]));
        Assert.Equal(-1, CampaignConnection.FirstResumable([]));
        Assert.Contains("delete only", CampaignLibrary.Summary(refused, null), StringComparison.Ordinal);
        Assert.DoesNotContain("delete only", CampaignLibrary.Summary(ordinary, null), StringComparison.Ordinal);
    }

    [Fact]
    public void ARefusalSurvivesARestartOfTheLocalCampaignStore()
    {
        string root = Path.Combine(Path.GetTempPath(), "marvel-campaign-access-" + Guid.NewGuid().ToString("N"));
        try
        {
            var refused = Connection("refused").Rejected(Rejection("save_incompatible"));
            var ordinary = Connection("ordinary");
            using (var store = new CampaignConnectionStore(root)) { store.Save(refused); store.Save(ordinary); }
            using var restored = new CampaignConnectionStore(root);
            var loaded = restored.Load();
            Assert.True(Assert.Single(loaded, item => item.LocalId == refused.LocalId).Refused);
            Assert.False(Assert.Single(loaded, item => item.LocalId == ordinary.LocalId).Refused);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private static CampaignConnection Connection(string label) =>
        new(Guid.NewGuid().ToString("N"), "", label, new string('a', 32), "owner", null, null);

    private static ClientCampaignResult Rejection(string code) =>
        new(null, null, new ClientStartupError(code, "A bounded explanation."), null);
}

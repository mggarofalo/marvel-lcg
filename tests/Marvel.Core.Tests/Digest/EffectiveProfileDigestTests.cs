using Marvel.Core.Digest;
using Xunit;

namespace Marvel.Core.Tests.Digest;

public sealed class EffectiveProfileDigestTests
{
    [Fact]
    public void AssignmentHasCanonicalBytesAndSurvivesRoundTrip()
    {
        // Engine choice: the profile is a mandatory key, traits retain authored
        // order, and base values use ordinal key order. Physical face remains.
        var digest = Board("scout");
        const string expected = """{"v":3,"cards":[{"id":7,"card":"physical","zone":"EngagedEnemiesArea","owner":0,"index":0,"host":-1,"face_up":false,"profile":{"id":"scout","title":"Scout","kind":"minion","traits":["ROBOT","SCOUT"],"base_values":{"ATK":3,"HP":4,"SCH":2}},"fields":{"health":4}}]}""";
        Assert.Equal(expected, digest.Canonical());
        Assert.Equal(expected, StateDigest.Parse(expected).Canonical());
    }

    [Fact]
    public void EqualCurrentStatsDoNotEraseDifferentAssignmentIdentities()
    {
        Assert.NotEqual(Board("scout").Fingerprint(), Board("other").Fingerprint());
        var printed = new StateDigest([Board("scout").Cards[0] with { Profile = null }]);
        Assert.NotEqual(Board("scout").Fingerprint(), printed.Fingerprint());
        Assert.Contains("\"profile\":null", printed.Canonical(), StringComparison.Ordinal);
    }

    [Fact]
    public void VersionTwoAndMissingAssignmentKeysCannotMasqueradeAsCurrentState()
    {
        Assert.Throws<NotSupportedException>(() => StateDigest.Parse("""{"v":2,"cards":[]}"""));
        string missing = Board("scout").Canonical();
        var card = Board("scout").Cards[0] with { Profile = null };
        missing = new StateDigest([card]).Canonical().Replace("\"profile\":null,", "", StringComparison.Ordinal);
        Assert.Throws<KeyNotFoundException>(() => StateDigest.Parse(missing));
    }

    private static StateDigest Board(string id) => new([
        new CardRecord(7, "physical", "EngagedEnemiesArea", 0, 0, -1, false,
            new Dictionary<string, long> { ["health"] = 4 })
        {
            Profile = new(id, "Scout", "minion", ["ROBOT", "SCOUT"],
                new Dictionary<string, long> { ["SCH"] = 2, ["HP"] = 4, ["ATK"] = 3 }),
        },
    ]);
}

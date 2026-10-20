using System.Security.Cryptography;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Cards;

public sealed class CaptainAmericaComponentSourceTests
{
    [Fact]
    public void DividerScanMatchesTheReviewedSource()
    {
        // Repository choice: pin the exact independently read FFG component scan.
        // The bytes preserve the primary authority; this is not an OCR correctness test.
        byte[] source = File.ReadAllBytes(RepositoryPaths.Dataset(
            "rules-packs", "mc04", "captain-america-deck-divider.jpg"));
        Assert.Equal("D4DB6702C25C7D537D8700518AEC5903AEF7BD2EE9AFA8E480F9C521384B9B6C",
            Convert.ToHexString(SHA256.HashData(source)));
    }
}

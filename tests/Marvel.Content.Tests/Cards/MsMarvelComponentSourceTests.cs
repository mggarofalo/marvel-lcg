using System.Security.Cryptography;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Cards;

public sealed class MsMarvelComponentSourceTests
{
    [Fact]
    public void DividerPhotoMatchesTheReviewedSource()
    {
        // Repository choice: pin the exact photograph of the FFG printed divider.
        // Its bytes preserve the reviewed authority; this does not test OCR.
        byte[] source = File.ReadAllBytes(RepositoryPaths.Dataset(
            "rules-packs", "mc05", "ms-marvel-deck-divider.jpg"));
        Assert.Equal("0549958A91B4CE7D258055E5F04E37777768DB9DF9B33AF1E7A0BE8B7F2C1798",
            Convert.ToHexString(SHA256.HashData(source)));
    }
}

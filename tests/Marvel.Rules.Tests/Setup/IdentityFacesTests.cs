using System.Text.Json;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Setup;

public sealed class IdentityFacesTests
{
    [Rule("rr:appendix-ii-setup.step.1")]
    [Theory]
    [InlineData("12001a,12001b,12001c", "12001b,12001a,12001c")]
    [InlineData("12001c,12001a,12001b", "12001b,12001c,12001a")]
    [InlineData("01001a,01001b", "01001b,01001a")]
    [InlineData("01001b,01001a", "01001b,01001a")]
    public void StartsAlterEgoAndPreservesHeroOrder(string input, string expected)
    {
        // "Each player selects one identity, placing their alter-ego side face up."
        // pack:mc12:ant-man-hero-pack#foldable-cards describes two hero sides.
        Assert.Equal(expected, IdentityFaces.StartingSpec(input, new PrintedFacts()));
    }

    [Theory]
    [InlineData("12001a")]
    [InlineData("12001b")]
    [InlineData("12001a,12001c")]
    [InlineData("12001a,12001b,01001b")]
    [InlineData("12001a,12001b,12001a")]
    [InlineData("12001a,12001b,12001b")]
    [InlineData("12001a,12001b,01002")]
    [InlineData("12001a,12001b,missing")]
    [InlineData("12001a,12001b,")]
    public void RejectsMalformedIdentity(string input) =>
        Assert.Throws<ArgumentException>(() => IdentityFaces.StartingSpec(input, new PrintedFacts()));

    private sealed class PrintedFacts : ICardFacts
    {
        private readonly Dictionary<string, CardKind> kinds = ReadKinds();

        public CardKind Kind(string faceId) => kinds.GetValueOrDefault(faceId, CardKind.Unknown);
        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) => new Dictionary<string, string>();
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) => fallback;

        private static Dictionary<string, CardKind> ReadKinds()
        {
            using var json = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
            return json.RootElement.GetProperty("cards").EnumerateArray().ToDictionary(
                card => card.GetProperty("card_id").GetString()!,
                card => Enum.TryParse<CardKind>(card.GetProperty("type").GetString(), out var kind) ? kind : CardKind.Unknown,
                StringComparer.Ordinal);
        }
    }
}

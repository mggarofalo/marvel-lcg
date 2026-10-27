using System.Globalization;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.View;
using Xunit;

namespace Marvel.View.Tests;

public sealed class BoardThreatPresentationTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(3, false)]
    [InlineData(0, true)]
    [InlineData(3, true)]
    public void SupportThreatUsesTheSuppliedLiveTokenCount(long threat, bool alsoInFields)
    {
        // Presentation policy: supplied live threat is meaningful on a support,
        // independently of whether a printed/stat field also carries that key.
        var fields = new Dictionary<string, long>(StringComparer.Ordinal);
        if (alsoInFields) fields["k_threat"] = threat;
        CardDescriptor support = new(12, CardBack.Player, true, true, -1,
            new CardFaceDescriptor("10029", "Beat Cop", "", CardKind.Support, fields))
        {
            State = new CardStateDescriptor(true, 0, threat,
                new Dictionary<string, long>(StringComparer.Ordinal), fields, []),
        };
        var world = new WorldDescriptor(
            [new PlayerDescriptor(0, "Hulk", true)],
            [new AreaDescriptor(2, "PlayerArea", 0, -1, [support], [])],
            [], Outcome.Unfinished);

        var card = Assert.Single(Assert.Single(BoardPresentation.From(world).Areas).Cards);
        Assert.Equal(new BoardFieldPresentation("THREAT",
            threat.ToString(CultureInfo.InvariantCulture)), Assert.Single(card.Fields));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void SideSchemeThreatComesFromItsLiveTableState(long threat)
    {
        CardDescriptor scheme = new(
            12,
            CardBack.Encounter,
            FaceUp: true,
            Ready: true,
            Host: -1,
            new CardFaceDescriptor(
                "01107",
                "Breakin' & Takin'",
                "",
                CardKind.EncounterSideScheme,
                new Dictionary<string, long>(StringComparer.Ordinal)
                {
                    ["hazard"] = 1,
                }))
        {
            State = new CardStateDescriptor(
                true,
                0,
                threat,
                new Dictionary<string, long>(StringComparer.Ordinal),
                new Dictionary<string, long>(StringComparer.Ordinal),
                []),
        };
        var world = new WorldDescriptor(
            [new PlayerDescriptor(0, "Peter Parker", false)],
            [new AreaDescriptor(2, "SideSchemesArea", -1, -1, [scheme], [])],
            [],
            Outcome.Unfinished);

        BoardCardPresentation card = Assert.Single(Assert.Single(
            BoardPresentation.From(world).Areas).Cards);

        Assert.Contains(
            new BoardFieldPresentation(
                "THREAT", threat.ToString(CultureInfo.InvariantCulture)),
            card.Fields);
        Assert.Contains(new BoardFieldPresentation("HAZARD", "1"), card.Fields);
    }
}

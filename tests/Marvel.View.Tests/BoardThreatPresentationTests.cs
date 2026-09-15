using System.Globalization;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.View;
using Xunit;

namespace Marvel.View.Tests;

public sealed class BoardThreatPresentationTests
{
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

using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.View;
using Xunit;

namespace Marvel.View.Tests;

public sealed class BoardStatusPresentationTests
{
    [Fact]
    public void HostedStatusCardsBecomeHostBadgesInsteadOfBoardAreas()
    {
        CardDescriptor rhino = Readable(10, "Rhino") with
        {
            State = new CardStateDescriptor(
                true, 0, null,
                new Dictionary<string, long>(StringComparer.Ordinal),
                new Dictionary<string, long>(StringComparer.Ordinal),
                ["Stunned"]),
        };
        var world = new WorldDescriptor(
            [new PlayerDescriptor(0, "Peter Parker", false)],
            [
                Area(1, "VillainArea", [rhino]),
                Area(2, "StatusArea", [Readable(11, "Stunned", host: 10)], host: 10),
            ],
            [],
            Outcome.Unfinished);

        BoardPresentation board = BoardPresentation.From(world);

        BoardAreaPresentation area = Assert.Single(board.Areas);
        Assert.Equal(["Stunned"], Assert.Single(area.Cards).Statuses);
        Assert.DoesNotContain(board.Areas, candidate => candidate.Zone == "StatusArea");
        Assert.DoesNotContain(board.Lanes.SelectMany(lane => lane.Areas),
            candidate => candidate.Zone == "StatusArea");
    }

    private static AreaDescriptor Area(
        int id,
        string zone,
        IReadOnlyList<CardDescriptor> cards,
        int host = -1) => new(id, zone, -1, host, cards, []);

    private static CardDescriptor Readable(int id, string title, int host = -1) => new(
        id,
        CardBack.Encounter,
        FaceUp: true,
        Ready: true,
        Host: host,
        new CardFaceDescriptor(
            $"face-{id}", title, "", CardKind.Minion,
            new Dictionary<string, long>(StringComparer.Ordinal)));
}

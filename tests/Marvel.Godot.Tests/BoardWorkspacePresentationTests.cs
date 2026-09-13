using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class BoardWorkspacePresentationTests
{
    [Fact]
    public void ExplicitViewedSeatRemainsDistinctFromAnsweringAndFirstPlayer()
    {
        BoardPresentation board = Board();
        Prompt prompt = PromptForSeatOne();

        BoardWorkspacePresentation workspace = BoardWorkspacePresentation.From(
            board, prompt, selectedAffordance: null, selectedTargets: [], viewedSeat: 0);

        BoardSeatPresentation viewed = Assert.IsType<BoardSeatPresentation>(workspace.ViewedSeat);
        Assert.Equal(0, viewed.Seat);
        Assert.False(viewed.IsAnswering);
        Assert.True(viewed.IsFirstPlayer);
        Assert.True(Assert.Single(workspace.Seats, seat => seat.Seat == 1).IsAnswering);
    }

    [Fact]
    public void PromptMappingsProduceSeatBadgesWithoutInspectingCardText()
    {
        BoardWorkspacePresentation workspace = BoardWorkspacePresentation.From(
            Board(), PromptForSeatOne(), selectedAffordance: 7, selectedTargets: [101],
            viewedSeat: 1);

        BoardSeatPresentation first = Assert.Single(workspace.Seats, seat => seat.Seat == 0);
        BoardSeatPresentation second = Assert.Single(workspace.Seats, seat => seat.Seat == 1);
        Assert.Equal(1, first.LegalTargetCount);
        Assert.Equal(1, first.SelectedTargetCount);
        Assert.Equal(1, second.ActionCount);
        Assert.Equal("1 concealed player card", Assert.Single(second.Hand!.Cards).Title);
    }

    [Fact]
    public void InitialWorkspaceUsesAnsweringSeatThenStableSeatOrderFallback()
    {
        BoardPresentation board = Board();

        Assert.Equal(1, BoardWorkspacePresentation.From(
            board, PromptForSeatOne(), null, [], null).ViewedSeat!.Seat);
        Assert.Equal(0, BoardWorkspacePresentation.From(
            board, prompt: null, null, [], null).ViewedSeat!.Seat);
    }

    [Fact]
    public void InitialWorkspacePrefersTheOnlyAuthorizedReadableHand()
    {
        BoardPresentation board = Board();
        BoardAreaPresentation firstHand = board.Areas.Single(area =>
            area.Zone == "HandsArea" && area.Seat == 0);
        BoardAreaPresentation secondHand = board.Areas.Single(area =>
            area.Zone == "HandsArea" && area.Seat == 1);
        BoardAreaPresentation concealedFirst = firstHand with
        {
            Cards = [secondHand.Cards[0]],
        };
        BoardAreaPresentation readableSecond = secondHand with
        {
            Cards = [firstHand.Cards[0]],
        };
        board = board with
        {
            Areas = [.. board.Areas.Select(area => area.Id switch
            {
                3 => concealedFirst,
                5 => readableSecond,
                _ => area,
            })],
            Lanes = [.. board.Lanes.Select(lane => lane.Seat switch
            {
                0 => lane with { Areas = [lane.Areas[0], concealedFirst] },
                1 => lane with { Areas = [lane.Areas[0], readableSecond] },
                _ => lane,
            })],
        };

        BoardWorkspacePresentation workspace = BoardWorkspacePresentation.From(
            board, prompt: null, null, [], null);

        Assert.Equal(1, workspace.ViewedSeat!.Seat);
    }

    [Fact]
    public void CollectionPagesClampAndClearWithThePresentationLifecycle()
    {
        BoardPageState pages = new();

        pages.SetPage(17, page: 9, pageCount: 3);
        Assert.Equal(2, pages.Page(17, pageCount: 3));
        Assert.Equal(0, pages.Page(17, pageCount: 1));

        pages.Clear();

        Assert.Equal(0, pages.Page(17, pageCount: 3));
    }

    private static BoardPresentation Board()
    {
        BoardCardPresentation firstIdentity = Card(101, "Spider-Man", "HERO") with
        {
            Fields =
            [
                new BoardFieldPresentation("HEALTH", "10/10"),
                new BoardFieldPresentation("FIRST PLAYER TOKEN", "1"),
            ],
            Status = "READY",
        };
        BoardCardPresentation secondIdentity = Card(201, "Captain Marvel", "ALTER EGO") with
        {
            Fields = [new BoardFieldPresentation("HEALTH", "12/12")],
            Status = "EXHAUSTED",
        };
        BoardAreaPresentation scenario = Area(1, "VillainArea", -1, Card(1, "Rhino", "ENCOUNTER VILLAIN"));
        BoardAreaPresentation firstHero = Area(2, "HeroArea", 0, firstIdentity);
        BoardAreaPresentation firstHand = Area(3, "HandsArea", 0, Card(102, "Web-Shooter", "UPGRADE"));
        BoardAreaPresentation secondHero = Area(4, "HeroArea", 1, secondIdentity);
        BoardAreaPresentation secondHand = Area(5, "HandsArea", 1, new BoardCardPresentation(
            null, 1, true, "1 concealed player card", "Identity and order hidden",
            "CONCEALED PILE", "PLAYER BACK", []));
        return new BoardPresentation([scenario, firstHero, firstHand, secondHero, secondHand])
        {
            Lanes =
            [
                new BoardLanePresentation("scenario", "SCENARIO", null, [scenario]),
                new BoardLanePresentation("player-0", "PLAYER 1 · SPIDER-MAN", 0,
                    [firstHero, firstHand]),
                new BoardLanePresentation("player-1", "PLAYER 2 · CAPTAIN MARVEL", 1,
                    [secondHero, secondHand]),
            ],
        };
    }

    private static Prompt PromptForSeatOne() => new(
        Player: 1,
        Asking: Question.Option,
        When: TimingPriority.Untimed,
        Trigger: "Fixture",
        Label: "Choose",
        Cancellable: true,
        Affordances:
        [
            new Affordance(7, "Choose", 201, 1, "Choose", new TargetRequest([101], 1, 1)),
        ]);

    private static BoardAreaPresentation Area(
        int id, string zone, int seat, BoardCardPresentation card) => new(
            id, zone.ToUpperInvariant(), seat < 0 ? "Scenario" : $"Seat {seat}", [card], [])
        {
            Zone = zone,
            Seat = seat,
            Prominence = BoardAreaProminence.Live,
        };

    private static BoardCardPresentation Card(int id, string title, string kind) => new(
        id, 1, false, title, string.Empty, kind, "READY", []);
}

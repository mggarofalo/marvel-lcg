using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Timing;

/// <summary>
/// Offering a window round the table.
/// </summary>
/// <remarks>
/// None of this is reachable in the recorded milestone game, which has one
/// player who declines everything. Everything here stands on its citation.
/// </remarks>
public sealed class ResolutionTests
{
    [Rule("rr:first-player.4")]
    [Rule("rr:first-player.5")]
    [Fact]
    public void TheFirstPlayerGetsTheFirstOpportunity()
    {
        // "The first player has the first opportunity to use an interrupt / a
        // response at each appropriate game moment." Not the active player, and
        // not whoever the occurrence is happening to.
        var world = Board(players: 3);
        world.FirstPlayer = 2;

        foreach (var kind in new[] { WindowKind.Interrupt, WindowKind.Response })
        {
            var window = world.Resolution.Open(new Occurrence(1, "WhenAttacked"), kind);
            Assert.Equal(2, window.Asking);
            world.Resolution.Close();
        }
    }

    [Rule("rr:in-player-order")]
    [Rule("rr:in-player-order.2")]
    [Fact]
    public void TheOpportunityMovesClockwise()
    {
        // "The first player performs their part first, followed by the other
        // players in clockwise order", and "next player" always means the next
        // clockwise player.
        var world = Board(players: 3);
        world.FirstPlayer = 1;
        var resolution = world.Resolution;
        resolution.Open(new Occurrence(1, "WhenAttacked"), WindowKind.Interrupt);

        Assert.Equal(1, resolution.Current!.Value.Asking);
        resolution.Pass();
        Assert.Equal(2, resolution.Current!.Value.Asking);
        resolution.Pass();
        Assert.Equal(0, resolution.Current!.Value.Asking);
    }

    [Rule("rr:interrupt.5")]
    [Rule("rr:response.4")]
    [Fact]
    public void AWindowClosesOnlyWhenEveryPlayerHasDeclinedInARow()
    {
        // "Once *all* players decide they do not wish to resolve any (further)
        // interrupts..." One player declining is not the end of a window.
        var world = Board(players: 3);
        var resolution = world.Resolution;
        resolution.Open(new Occurrence(1, "WhenAttacked"), WindowKind.Interrupt);

        Assert.False(resolution.Pass());
        Assert.False(resolution.Pass());
        Assert.True(resolution.Pass());
        Assert.False(resolution.IsResolving);
    }

    [Rule("rr:interrupt.5")]
    [Rule("rr:in-player-order.1")]
    [Fact]
    public void UsingAnAbilityGivesEveryoneAnotherOpportunity()
    {
        // The word doing the work is "(further)": the count of consecutive
        // declines resets, because a player who passed on an untouched board
        // may have something to say now that it has changed. And
        // `rr:in-player-order.1` keeps the sequence going clockwise rather than
        // restarting it at the first player.
        var world = Board(players: 3);
        var resolution = world.Resolution;
        resolution.Open(new Occurrence(1, "WhenAttacked"), WindowKind.Interrupt);

        Assert.False(resolution.Pass());   // p0 declines
        resolution.Used();                 // p1 acts
        Assert.Equal(2, resolution.Current!.Value.Asking);
        Assert.Equal(0, resolution.Current!.Value.Passed);

        // p0's earlier decline is spent, so it takes three more to close.
        Assert.False(resolution.Pass());
        Assert.False(resolution.Pass());
        Assert.True(resolution.Pass());
    }

    [Rule("rr:in-player-order.1")]
    [Fact]
    public void AWindowIsNotOnePassRoundTheTable()
    {
        // "If a sequence performed in player order does not conclude after each
        // player has performed their part of the sequence once, the sequence of
        // opportunities continues in a clockwise manner until it is complete."
        // A window that closed after one lap would silently refuse the second
        // interrupt of a player who had two.
        var world = Board(players: 2);
        var resolution = world.Resolution;
        resolution.Open(new Occurrence(1, "WhenAttacked"), WindowKind.Interrupt);

        for (int lap = 0; lap < 4; lap++)
        {
            Assert.False(resolution.Pass());
            resolution.Used();
        }

        Assert.True(resolution.IsResolving);
    }

    [Fact]
    public void AOnePlayerGameClosesAWindowOnOneDecline()
    {
        // Every player declining in a row is one decline when there is one
        // player, which is why the recorded milestone game can never show that
        // the loop above exists.
        var world = Board(players: 1);
        world.Resolution.Open(new Occurrence(1, "WhenAttacked"), WindowKind.Interrupt);
        Assert.True(world.Resolution.Pass());
    }

    [Rule("rr:initiating-abilities.3")]
    [Fact]
    public void WindowsNest()
    {
        // An interrupt that plays a card is itself an occurrence with windows of
        // its own, and the outer window is still open underneath.
        // `rr:initiating-abilities.3` is why the inner sequence outlives its
        // source: it "does not stop from completing if that card leaves play
        // during this sequence".
        var world = Board(players: 2);
        var resolution = world.Resolution;
        var outer = new Occurrence(1, "WhenAttacked");
        var inner = new Occurrence(2, "WhenCardPlayed");

        resolution.Open(outer, WindowKind.Interrupt);
        resolution.Open(inner, WindowKind.Response);

        Assert.Equal(2, resolution.Depth);
        Assert.Equal(inner, resolution.Current!.Value.Occurrence);

        resolution.Pass();
        resolution.Pass();

        Assert.Equal(1, resolution.Depth);
        Assert.Equal(outer, resolution.Current!.Value.Occurrence);
    }

    [Rule("rr:interrupt.4")]
    [Fact]
    public void CancellingTheOccurrenceClosesTheWindowWithoutAskingTheRest()
    {
        // "If an interrupt changes (via a replacement effect) or cancels an
        // imminent triggering condition, further interrupts to the original
        // triggering condition cannot be triggered." There is nothing left to
        // interrupt, so the remaining players are not asked.
        var world = Board(players: 4);
        var resolution = world.Resolution;
        resolution.Open(new Occurrence(1, "WhenAttacked"), WindowKind.Interrupt);

        resolution.Close();

        Assert.False(resolution.IsResolving);
    }

    [Fact]
    public void ThereIsNoWindowToAnswerBeforeOneIsOpened()
    {
        var world = Board(players: 2);
        Assert.Null(world.Resolution.Current);
        Assert.False(world.Resolution.IsResolving);
        Assert.Throws<InvalidOperationException>(() => world.Resolution.Pass());
    }

    private static World Board(int players)
    {
        var world = new World(new Facts(), players);
        for (int seat = 0; seat < players; seat++)
        {
            world.CreateSeat($"p{seat}");
        }

        return world;
    }

    private sealed class Facts : ICardFacts
    {
        public CardKind Kind(string faceId) => CardKind.Unknown;

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            new Dictionary<string, string>(StringComparer.Ordinal);

        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
            fallback;
    }
}

using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>
/// The two halves of a resolve agree — <c>docs/event-stream.md</c>.
/// </summary>
/// <remarks>
/// <para>
/// A resolve returns a board and an account of what changed on it. Each is
/// checkable against the other and <b>neither needs a second implementation to
/// check it against</b>: if the digest moved, something must have said so, and
/// if nothing was said, the digest must not have moved.
/// </para>
/// <para>
/// <b>This is what a folded recording was really asserting.</b> Comparing every
/// step of a played game against another engine's transcript catches a board
/// that changed silently — but it catches it by knowing the answer in advance,
/// and it cannot be run once the transcript is gone. The invariant survives the
/// transcript: a client that is told nothing and re-renders anyway is showing
/// the player a board the engine never announced.
/// </para>
/// <para>
/// Played across several seeds with a policy that acts rather than declines,
/// because a declining player reaches three boards and a playing one reaches
/// the villain phase, an attack, a defence and a card leaving play.
/// </para>
/// </remarks>
public sealed class EventsExplainTheBoardTests
{
    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData(1u)]
    [InlineData(7u)]
    [InlineData(12345u)]
    public void ADecisionThatChangesTheBoardSaysSo(uint seed)
    {
        // The half that catches a silent change. A resolve that moves a card
        // and reports nothing leaves every client stale, and the board it
        // leaves is correct -- which is why nothing else notices.
        int moved = 0;

        Play(seed, (before, after, events) =>
        {
            if (before == after)
            {
                return;
            }

            moved++;
            Assert.True(
                events.Count > 0,
                $"seed {seed}: the digest moved and no event said so");
        });

        Assert.True(moved > 0, $"seed {seed} never moved the board");
    }

    [Theory]
    [InlineData(1u)]
    [InlineData(7u)]
    [InlineData(12345u)]
    public void ADecisionThatSaysNothingChangesNothing(uint seed)
    {
        // The other half, and the one that catches an invented change. An
        // event stream that reports a move nothing made is a client rendering
        // an animation for a card that did not go anywhere.
        int quiet = 0;

        Play(seed, (before, after, events) =>
        {
            if (events.Count > 0)
            {
                return;
            }

            quiet++;
            Assert.True(
                before == after,
                $"seed {seed}: the digest moved and the event list was empty");
        });

        Assert.True(quiet > 0, $"seed {seed} never had a quiet decision");
    }

    [Theory]
    [InlineData(1u)]
    [InlineData(7u)]
    [InlineData(12345u)]
    public void EveryEventSaysWhyItHappened(uint seed)
    {
        // "Every event also carries `kind`, plus `trigger` and `verb` -- the
        // engine's own names for why the transition happened. Those are the
        // half a digest can never show." An event with no trigger is a change
        // a client can render and not explain.
        Play(seed, (_, _, events) => Assert.All(
            events,
            happened => Assert.False(
                string.IsNullOrEmpty(happened.Trigger),
                $"{happened.GetType().Name} carries no trigger")));
    }

    /// <summary>
    /// Plays one seed out, calling <paramref name="check"/> with the digest
    /// before, the digest after, and what the engine said about the difference.
    /// </summary>
    private static void Play(
        uint seed, Action<string, string, IReadOnlyList<GameEvent>> check)
    {
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            ["Spider-Man"],
            seed,
            AuthoredCards.Runner());

        var game = Game.Begin(world, Cards, AuthoredCards.Runner());
        var policy = new ActingPolicy((int)seed);

        for (int decisions = 0; game.Pending is not null; decisions++)
        {
            Assert.True(decisions < 3000, $"seed {seed} is still playing");

            string before = game.State.Digest().Canonical();
            var result = game.Resolve(policy.Answer(game.Pending));
            string after = result.State.Digest().Canonical();

            check(before, after, result.Events);
        }
    }
}

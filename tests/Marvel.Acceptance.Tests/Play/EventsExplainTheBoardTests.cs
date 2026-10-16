using Marvel.Rules.Setup;
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
/// Board changes produce events, and every emitted event identifies its cause.
/// Payload accuracy is established by the focused rule contracts; a digest alone
/// cannot distinguish a correct event from an invented one.
/// </summary>
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
        int moved = 0;
        int quiet = 0;
        Play(seed, (before, after, events) =>
        {
            if (before != after)
            {
                moved++;
                Assert.NotEmpty(events);
            }
            if (events.Count == 0) quiet++;
        });
        Assert.True(moved > 0, $"seed {seed} never moved the board");
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

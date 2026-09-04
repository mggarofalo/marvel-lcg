using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>
/// A whole game, played to the end.
/// </summary>
/// <remarks>
/// <para>
/// Every other test here holds one rule against the rulebook. This holds the
/// engine against the only claim that matters to a player: <b>a game can be
/// played from the deal to an ending without meeting a rule that is not
/// written.</b>
/// </para>
/// <para>
/// <para>
/// The policy below is deliberately crude — it is not a bot and does not try to
/// win. What it does is take a decision of every kind the engine offers, which
/// is what makes an unimplemented rule surface as a failure here rather than in
/// somebody's game.
/// </para>
/// </remarks>
public sealed class WholeGameTests
{
    private const string Campaign = "rhino";

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData(12345u)]
    [InlineData(2026u)]
    [InlineData(777u)]
    [InlineData(4242u)]
    public void AGameRunsFromTheDealToAnEnding(uint seed)
    {
        var (game, world) = Deal(seed, "spider_man");
        var played = Run(game, world);

        // An ending, and a definite one: `World.Result` names which of the
        // three the rules describe actually happened.
        Assert.Null(game.Pending);
        Assert.NotEqual(Outcome.Unfinished, world.Result);
        Assert.True(game.Round > 1, $"the game ended in round {game.Round}");

        // And it was a game, not a sequence of declines: cards were paid for
        // and played, and characters used their powers.
        Assert.True(played > 0, "no card was ever played");
    }

    [Theory]
    [InlineData(12345u)]
    [InlineData(2026u)]
    public void ATwoPlayerGameRunsToo(uint seed)
    {
        // **A second player is not a bigger version of the first.** Every
        // sentence in the rules that says "in player order", "the next
        // clockwise player" or "a different player" is unreachable at one, and
        // the first two-player game this ran hit `rr:defend-defense.5` --
        // She-Hulk defending an attack aimed at Spider-Man -- inside one round.
        var (game, world) = Deal(seed, "spider_man", "she_hulk");
        var played = Run(game, world);

        Assert.Null(game.Pending);
        Assert.NotEqual(Outcome.Unfinished, world.Result);
        Assert.True(played > 0, "no card was ever played");
    }

    [Theory]
    [InlineData(12345u)]
    [InlineData(2026u)]
    public void AThreePlayerGameRunsToo(uint seed)
    {
        // Three is where "the next clockwise player" stops being "the other
        // one": the first player token wraps, `rr:in-player-order.1`'s
        // "continues in a clockwise manner" has somewhere to continue to, and
        // `rr:hazard-icon`'s extra cards go round the table rather than to a
        // pair.
        var (game, world) = Deal(seed, "spider_man", "she_hulk", "captain_marvel");
        var played = Run(game, world);

        Assert.Null(game.Pending);
        Assert.NotEqual(Outcome.Unfinished, world.Result);
        Assert.True(played > 0, "no card was ever played");
    }

    [Fact]
    public void TheSameSeedPlaysTheSameGame()
    {
        // Non-negotiable 1: a seed names a game. Two deals of one seed, driven
        // by one policy, must agree about everything -- including the shuffles
        // a reshuffled deck consumed. This is the only determinism check the
        // engine has, and it compares a game against a second run of itself.
        var (first, firstWorld) = Deal(2026, "spider_man");
        var (second, secondWorld) = Deal(2026, "spider_man");

        Run(first, firstWorld);
        Run(second, secondWorld);

        Assert.Equal(firstWorld.Result, secondWorld.Result);
        Assert.Equal(first.Round, second.Round);
        Assert.Equal(
            firstWorld.Digest().Canonical(),
            secondWorld.Digest().Canonical());
    }

    /// <summary>Plays the game out, and answers how many cards were played.</summary>
    private static int Run(Game game, World world)
    {
        int played = 0;
        for (int decisions = 0; game.Pending is not null; decisions++)
        {
            Assert.True(decisions < 600, $"still playing after {decisions} decisions");

            var options = game.Pending.Affordances;
            var ending = options.FirstOrDefault(option => option.Verb == Game.EndPhaseVerb);
            var play = options.FirstOrDefault(option => option.Verb == CardPlay.Verb);
            long threat = world.TheCardIn(DeckType.MainSchemesArea)
                ?.Tokens.GetValueOrDefault("k_threat") ?? 0;

            // Thwart once the scheme is climbing, attack otherwise.
            var power = threat >= 5
                ? options.FirstOrDefault(option => option.Verb == BasicPowers.ThwartVerb)
                : options.FirstOrDefault(option => option.Verb == BasicPowers.AttackVerb);
            power ??= options.FirstOrDefault(option => option.Verb == BasicPowers.ThwartVerb)
                ?? options.FirstOrDefault(option => option.Verb == Game.ChangeForm);

            if (ending is not null)
            {
                game.Resolve(Decision.Take(ending.Id, Excess(world, game), []));
            }
            else if (game.Pending.Asking == Question.Order)
            {
                var ordered = options[0];
                game.Resolve(new Decision(
                    ordered.Id,
                    ordered.Targets is { } targets ? [.. targets.Legal] : []));
            }
            else if (game.Pending.Asking == Question.Defender)
            {
                // The last candidate, which is an ally when there is one --
                // `rr:defend-defense.3`.
                game.Resolve(Decision.Take(options[^1].Id));
            }
            else if (play is { } card && Payment(card) is { } paying)
            {
                game.Resolve(Decision.Take(card.Id, [card.Targets!.Legal[0]], paying));
                played++;
            }
            else if (power is { } using_)
            {
                game.Resolve(using_.Targets is { } targets && targets.Legal.Count > 0
                    ? Decision.Take(using_.Id, [targets.Legal[0]], [])
                    : Decision.Take(using_.Id));
            }
            else
            {
                game.Resolve(Decision.Decline);
            }
        }

        return played;
    }

    /// <summary>`rr:end-of-player-phase.step.1` — the cards over hand size.</summary>
    private static int[] Excess(World world, Game game)
    {
        var seat = world.Seats[game.Pending!.Player];
        long limit = PhaseEnd.HandSize(world, seat, Cards);
        return [.. seat.Hand.Cards
            .Take(Math.Max(0, seat.Hand.Cards.Count - (int)limit))
            .Select(card => card.ObjectId)];
    }

    /// <summary>The first few generators that cover a cost, or null.</summary>
    private static int[]? Payment(Affordance option)
    {
        long cost = long.Parse(
            option.CostOptions[0].Cost, System.Globalization.CultureInfo.InvariantCulture);
        var spent = new List<int>();
        long generated = 0;
        foreach (var source in option.CostOptions[0].Generators)
        {
            if (generated >= cost)
            {
                break;
            }

            spent.Add(source.Effect);
            generated += source.Generates.Length;
        }

        return generated >= cost ? [.. spent] : null;
    }

    private static (Game Game, World World) Deal(uint seed, params string[] heroes)
    {
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, Campaign, heroes), Cards),
            [.. heroes.Select(hero => Setup.Hero(hero).Name)],
            seed);
        return (Game.Begin(world, Cards), world);
    }
}

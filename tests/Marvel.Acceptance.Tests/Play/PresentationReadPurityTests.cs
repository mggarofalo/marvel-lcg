using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Server;
using Marvel.Session;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>
/// Everything a client is shown is read from the published game, and reading it changes nothing
/// a replay of the same decisions would not.
/// </summary>
/// <remarks>
/// Engine choice: an area id is its creation order and an affordance handle its allocation order,
/// and both reach the wire, so a read that allocated either would renumber a later event or offer.
/// Each game is played beside an unread twin given the same answers. After every read the board,
/// its areas, cards and game areas, and the random stream must be as they were, and after every
/// decision both games must publish the same prompt, events, digest and stream.
/// </remarks>
public sealed class PresentationReadPurityTests
{
    private static readonly DatasetGameFactory Factory = DatasetGameFactory.Load(RepositoryPaths.Root);

    [Theory]
    [InlineData("rhino", "spider_man,captain_marvel,she_hulk", 1000u)]
    [InlineData("red_skull", "thor,black_widow", 1007u)]
    [InlineData("wrecking_crew_standard", "rocket_raccoon,captain_marvel", 1014u)]
    [InlineData("zola", "ant_man,wasp,quicksilver,hulk", 1021u)]
    public void ReadingAPublicGameChangesNothing(string scenario, string heroes, uint seed)
    {
        var specification = new GameSpecification(scenario, heroes.Split(','), null, seed);
        var read = Factory.Create(specification).Game;
        var twin = Factory.Create(specification).Game;
        var policy = new ActingPolicy((int)seed);

        PlayBesideTwin(read, twin, () => policy.Answer(read.Pending!));
    }

    [Theory]
    [InlineData("kang", "mot", 2, 1u)]
    [InlineData("kang", "temporal", 4, 4u)]
    public void ReadingASplitKangGameChangesNothing(string scenario, string modular, int players, uint seed)
    {
        // pack:mc11:game-areas: the separated table keeps one shared encounter deck and discard
        // pile, which a projection names; the split, a departure and the reunion each make areas.
        var specification = new GameSpecification(scenario, KangRecipeGames.Heroes.Take(players).ToArray(), [modular], seed);
        var read = new KangRecipeFactory().Create(specification).Game;
        var twin = new KangRecipeFactory().Create(specification).Game;
        var policy = new CoreGamePolicy(read.State.Facts);
        bool split = false;

        PlayBesideTwin(read, twin, () =>
        {
            split |= Places.Separated(read.State);
            return KangInsertAdvice.Answer(policy, read);
        });

        Assert.True(split);
    }

    private static void PlayBesideTwin(Game read, Game twin, Func<Decision> answer)
    {
        IReadOnlyList<GameEvent> events = [];
        for (int step = 0; read.Pending is not null; step++)
        {
            Assert.True(step < 3000, $"no ending by decision {step}");
            ReadEverything(read, events, step);
            var decision = answer();
            var resolved = read.Resolve(decision);
            var mirrored = twin.Resolve(decision);
            Assert.Equal(Published(twin, mirrored), Published(read, resolved));
            events = resolved.Events;
        }
        Assert.NotEqual(Outcome.Unfinished, read.State.Result);
    }

    private static void ReadEverything(Game game, IReadOnlyList<GameEvent> events, int step)
    {
        var world = game.State;
        foreach (var (name, read) in Reads(game, events))
        {
            string before = Board(world);
            read();
            Assert.True(before == Board(world), $"decision {step}: the {name} read changed the game");
        }
    }

    private static IEnumerable<(string Name, Action Read)> Reads(Game game, IReadOnlyList<GameEvent> events)
    {
        var world = game.State;
        yield return ("digest", () => world.Digest());
        for (int seat = 0; seat < world.Players; seat++)
        {
            int viewer = seat;
            var scope = new RestrictedVisibilityPolicy(viewer).Authorize(null, world.Players);
            yield return ($"seat {viewer} projection", () =>
                WorldProjection.For(world, game.PromptFor(viewer), events, scope, game.Active, game.Pending));
            yield return ($"seat {viewer} history", () =>
                WorldProjection.ForEvents(world, scope, game.Active).WithEvents(events));
        }
        yield return ("table projection", () => WorldProjection.For(world, game.Pending, events,
            new PermissiveVisibilityPolicy().Authorize(null, world.Players), game.Active, game.Pending));
        yield return ("spectator projection", () =>
            WorldProjection.For(world, null, events, ViewScope.None, game.Active, game.Pending));
    }

    // The digest omits area identities and game-area membership, so both are listed beside it.
    private static string Board(World world) => string.Join('\n',
        world.Digest().Canonical(),
        world.Random.Generator.WordsConsumed,
        world.Cards.Count,
        string.Join(';', world.Areas.Select(area => $"{area.Id}:{area.Type}:{area.PlayArea.Player}:{area.Host}")),
        string.Join(';', world.GameAreas.Select(area => $"{area.Id}:{string.Join(',', area.PlayAreas.Select(play => play.Player))}")));

    private static string Published(Game game, Resolution result) => string.Join('\n',
        Board(game.State),
        JsonSerializer.Serialize(game.Pending),
        JsonSerializer.Serialize(result.Events.Select(JournalJson.Event).ToArray(), SessionSaveJson.Options));
}

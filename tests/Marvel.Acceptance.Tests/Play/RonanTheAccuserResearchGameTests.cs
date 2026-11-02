using System.Text.Json;
using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>
/// Research-only complete Ronan the Accuser games. The scenario is withheld with Ship Command,
/// so direct setup exercises the executable book without public discovery.
/// </summary>
public sealed class RonanTheAccuserResearchGameTests(ITestOutputHelper output)
{
    private const string PowerStone = "16149";
    private static readonly SetupCatalog Setup = SetupCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [Rule("rr:appendix-ii-setup.step.12.a")]
    public void SetupPutsTheShipsIntoPlayAndGivesTheFirstPlayerThePowerStone(bool multiplayer, bool expert)
    {
        // 16106a: "Setup: Put the Kree Command Ship environment and the Milano support into play.
        // Attach the Universal Weapon to Ronan the Accuser. Attach the Power Stone to the first player."
        var game = Deal(multiplayer, 1, expert);
        var villain = game.State.TheCardIn(DeckType.VillainArea)!;
        var ship = Assert.Single(game.State.Cards, card => card.FaceId == "16108");
        var milano = Assert.Single(game.State.Cards, card => card.FaceId == "16142");
        var weapon = Assert.Single(game.State.Cards, card => card.FaceId == "16109");
        var stone = Assert.Single(game.State.Cards, card => card.FaceId == PowerStone);

        Assert.Equal(DeckType.EnvironmentArea, ship.Area.Type);
        Assert.Equal(PlayArea.Of(game.State.FirstPlayer), milano.Area.PlayArea);
        Assert.Equal(villain.ObjectId, weapon.Area.Host);
        Assert.Equal(game.State.Seats[game.State.FirstPlayer].IdentityCard.ObjectId, stone.Area.Host);
        Assert.Equal(expert ? "16104" : "16103", villain.FaceId);
        Assert.Equal("16106b", game.State.TheCardIn(DeckType.MainSchemesArea)!.FaceId);
        Assert.Equal(1, StateFields.Modified(game.State, villain, "stalwart", game.State.Facts, game.State.Players));
    }

    [Theory]
    [InlineData(false, 1u)]
    [InlineData(false, 2u)]
    [InlineData(false, 3u)]
    [InlineData(true, 4u)]
    [InlineData(true, 5u)]
    [InlineData(true, 6u)]
    public void RonanGamesReachAnEndingWithExactDecisionReplay(bool multiplayer, uint seed)
    {
        var game = Deal(multiplayer, seed, expert: false);
        var replay = Deal(multiplayer, seed, expert: false);
        var policy = new CoreGamePolicy(Cards);
        var hosts = new HashSet<int>();
        Assert.Equal(Observe(game), Observe(replay));
        while (game.Pending is { } prompt)
        {
            Assert.True(policy.Answered < 3000, $"seed={seed}: {prompt.Label}");
            var answer = policy.Answer(game);
            Resolution result;
            try { result = game.Resolve(answer); }
            catch (Exception failure)
            {
                throw new InvalidOperationException($"multiplayer={multiplayer}, seed={seed}, "
                    + $"decision={policy.Answered}; prompt={JsonSerializer.Serialize(PromptRecord.From(prompt))}; "
                    + $"answer={JsonSerializer.Serialize(answer)}", failure);
            }
            Assert.Equal(Observe(game, result), Observe(replay, replay.Resolve(answer)));
            hosts.Add(game.State.Cards.Single(card => card.FaceId == PowerStone).Area.Host);
        }
        Assert.NotEqual(Outcome.Unfinished, game.State.Result);
        Assert.Null(replay.Pending);
        output.WriteLine($"multiplayer={multiplayer}, seed={seed}: {game.State.Result}; round={game.Round}, "
            + $"decisions={policy.Answered}, rngWords={game.State.Random.Generator.WordsConsumed}, stoneHosts={hosts.Count}, "
            + $"stage={game.State.TheCardIn(DeckType.VillainArea)?.FaceId}, scheme={game.State.TheCardIn(DeckType.MainSchemesArea)?.FaceId}");
    }

    private static Game Deal(bool multiplayer, uint seed, bool expert)
    {
        string[] heroes = multiplayer ? ["she_hulk", "groot"] : ["she_hulk"];
        var plan = ScenarioDealer.Prepare(Setup,
            new(expert ? "ronan_the_accuser_expert" : "ronan_the_accuser", heroes, null), Cards);
        var abilities = AuthoredCards.Runner();
        var world = WorldSetup.DealConfigured(Cards, plan.Blueprints,
            [.. heroes.Select(hero => Setup.Hero(hero).Name)], seed, abilities,
            new(Expert: expert, EncounterGroups: plan.EncounterGroups));
        return Game.Begin(world, Cards, abilities);
    }

    private static string Observe(Game game, Resolution? result = null) => JsonSerializer.Serialize(new
    {
        State = game.State.Digest().Canonical(),
        RngWords = game.State.Random.Generator.WordsConsumed,
        game.State.Result,
        game.Phase,
        game.Round,
        Prompt = game.Pending is { } prompt ? PromptRecord.From(prompt) : null,
        result?.Events,
        result?.Information,
    });
}

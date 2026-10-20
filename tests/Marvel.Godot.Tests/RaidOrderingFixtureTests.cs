using Marvel.Content;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Marvel.Tests;
using Marvel.View;
using Xunit;
namespace Marvel.Godot.Tests;

public sealed class RaidOrderingFixtureTests
{
    [Fact]
    public void PlayedRaidOffersFirstPlayerAReadableChoiceOfTheNextWhenRevealedEffect()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var policy = new CoreGamePolicy(cards);
        for (uint seed = 1; seed <= 256; seed++)
        {
            var game = factory.Create(new("crossbones", ["spider_man"], null, seed)).Game;
            for (int step = 0; step < 500 && game.Pending is { } prompt; step++)
            {
                if (IsRaid(game, prompt))
                {
                    Assert.Equal(game.State.FirstPlayer, prompt.Player);
                    Assert.False(prompt.Cancellable);
                    Assert.Equal("Which ability resolves next?", prompt.DisplayQuestion);
                    Assert.Contains("first player", prompt.Description);
                    Assert.Contains(prompt.Affordances, offer => offer.DisplayLabel == "Incite 1"
                        && offer.CommitLabel == "Resolve Incite 1 next"
                        && offer.Description == "Place 1 threat on the main scheme.");
                    Assert.Contains(prompt.Affordances, offer => offer.DisplayLabel == "When Revealed"
                        && offer.Description == "When Revealed: Discard cards from the top of the encounter deck until a Weapon attachment is discarded. Reveal that card.");
                    Assert.Single(prompt.ContextCardIds);
                    WriteFixture(game, prompt);
                    return;
                }
                game.Resolve(policy.Answer(game));
            }
        }
        Assert.Fail("Seeded Crossbones journeys did not reach Raid the Armory's ordering choice.");
    }

    private static bool IsRaid(Game game, Prompt prompt) => prompt.Asking == Question.Order
        && prompt.Affordances.Any(offer => game.State.Cards[offer.AnchorId].FaceId == "04069");

    private static void WriteFixture(Game game, Prompt prompt)
    {
        string? path = Environment.GetEnvironmentVariable("MARVEL_RAID_ORDER_FIXTURE");
        if (path is null) return;
        Assert.False(Path.GetFullPath(path).StartsWith(RepositoryPaths.Root + Path.DirectorySeparatorChar,
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
        var world = WorldProjection.For(game.State, prompt, [],
            new PermissiveVisibilityPolicy().Authorize(null, game.State.Players)).World;
        File.WriteAllBytes(path, EngineJson.Write(new EngineResponse(EngineProtocol.Version, "raid-fixture", "played-crossbones",
            null, prompt, [], world)));
    }
}

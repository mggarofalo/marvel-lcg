using System.Text.Json;
using Marvel.Content;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class LegalIronManTableauTests
{
    [Fact]
    public void PlayedCoreIronManSourcesRetainTheirMeaningAndFullHand()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        Game game = factory.Create(new("rhino", ["iron_man"], null, Seed: 5)).Game;
        var policy = new CoreGamePolicy(cards);
        for (int decision = 0; decision < 48; decision++) game.Resolve(policy.Answer(game));
        WorldDescriptor world = WorldProjection.For(game.State, game.Pending, [],
            new RestrictedVisibilityPolicy(0).Authorize(null, game.State.Players)).World;
        BoardPresentation board = BoardPresentation.From(world);
        BoardCardPresentation[] sources = CardSourceGroups.Controlled(board.Areas, 0);
        Assert.Equal(5, sources.Length);
        Assert.Equal(5, board.Areas.Where(area => area.Zone == "HandsArea").Sum(area => area.Cards.Count));
        Assert.All(sources, source => Assert.False(source.Concealed));
        Assert.Contains(sources, source => source.Persistent?.Contributions.Count > 0);
        Assert.NotNull(game.Pending);
        WriteFixture(board, world, game.Pending);
    }

    private static void WriteFixture(BoardPresentation board, WorldDescriptor world, Prompt prompt)
    {
        string? path = Environment.GetEnvironmentVariable("MARVEL_LEGAL_TABLEAU_FIXTURE");
        if (path is null) return;
        Assert.False(Path.GetFullPath(path).StartsWith(RepositoryPaths.Root + Path.DirectorySeparatorChar,
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
        File.WriteAllText(path, JsonSerializer.Serialize(board.Areas));
        File.WriteAllText(path + ".world", JsonSerializer.Serialize(world));
        File.WriteAllText(path + ".prompt", JsonSerializer.Serialize(prompt));
    }
}

using System.Diagnostics;
using Marvel.Protocol;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class InvocationNativeJourneyTests
{
    public static bool NativeRequested => Environment.GetEnvironmentVariable("MARVEL_NATIVE_INVOCATION") == "true";

    [Theory(Skip = "Explicit prepared nonpublic native Invocation proof.", SkipUnless = nameof(NativeRequested))]
    [InlineData("spell", "80")]
    [InlineData("master", "80")]
    [InlineData("spell", "100")]
    [InlineData("master", "100")]
    [InlineData("spell", "150")]
    [InlineData("master", "150")]
    public async Task PreparedSevenRingsUsesViewportInput(string path, string scale)
    {
        string evidence = Environment.GetEnvironmentVariable("MARVEL_UI_EVIDENCE_DIRECTORY")!;
        string directory = Path.Combine(evidence, scale, path);
        Directory.CreateDirectory(directory);
        bool rendered = Environment.GetEnvironmentVariable("MARVEL_NATIVE_RENDERED") == "true";
        using var priority = new NativeHostPriority(rendered);
        await using var server = new NativeContractServer("invocation-" + path, directory);
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("GODOT_BIN")!)
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
        };
        foreach (string argument in new[] { rendered ? "--minimized" : "--headless",
            "--audio-driver", "Dummy", "--path", Path.Combine(RepositoryPaths.Root, "src", "Marvel.Godot"),
            "--script", "res://smoke/automation/invocation_acceptance.gd" }) start.ArgumentList.Add(argument);
        start.Environment["MARVEL_ENGINE_ENDPOINT"] = server.Endpoint;
        start.Environment["MARVEL_SMOKE_TWO_PLAYER"] = "true";
        start.Environment["MARVEL_SMOKE_VIEWPORT"] = "1920x1080";
        start.Environment["MARVEL_UI_SCALE"] = scale;
        start.Environment["MARVEL_SMOKE_MOTION"] = "disabled";
        start.Environment["MARVEL_INVOCATION_PATH"] = path;
        start.Environment["MARVEL_UI_EVIDENCE_DIRECTORY"] = directory;
        using Process process = Process.Start(start)!;
        Task<string> output = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        Task<string> error = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(70));
        string log;
        try { await process.WaitForExitAsync(deadline.Token); }
        finally
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            log = await output + await error;
            log += $"\nINVOCATION_RESOLVE_COUNT {server.Requests.Count(request => request.Operation == EngineProtocol.Resolve)}\n";
            File.WriteAllText(Path.Combine(directory, "run.log"), log);
            TestContext.Current.TestOutputHelper!.WriteLine(log);
            if (server.SceneFailure is { } failure) TestContext.Current.TestOutputHelper.WriteLine(failure.ToString());
        }
        Assert.Equal(0, process.ExitCode);
        Assert.DoesNotContain("ERROR:", log);
        Assert.Contains("UI_ACCEPTANCE_OK invocation " + path, log);
        Assert.All(server.Responses, response => Assert.Null(response.Error));
        var mutations = server.Requests.Where(request => request.Operation == EngineProtocol.Resolve).ToArray();
        Assert.Equal(2, mutations.Length);
        Assert.Equal(41, Assert.Single(mutations[0].Decision!.Resources!));
        Assert.Equal(0, Assert.Single(mutations[1].Decision!.Targets!));
        var world = server.PreparedGame.State;
        Assert.True(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Tough));
        Assert.False(Statuses.Has(world, world.Seats[1].IdentityCard, Statuses.Tough));
        Assert.Equal(path == "master", world.Seats[1].IdentityCard.Ready);
        Assert.Equal(DeckType.DiscardPile, world.Cards[41].Area.Type);
        var deck = world.Seats[1].InvocationDeck!;
        var rings = world.Cards.Single(card => card.FaceId == "09034");
        if (path == "master")
        {
            Assert.Same(rings, deck.PublicTop);
            Assert.Empty(deck.Discard.Cards);
        }
        else
        {
            Assert.Same(deck.Discard, rings.Area);
            Assert.NotSame(rings, deck.PublicTop);
        }
        Assert.Equal(5, deck.Deck.Cards.Count + deck.Discard.Cards.Count);
        Assert.Single(deck.Deck.Cards, card => card.FaceUp);
    }
}

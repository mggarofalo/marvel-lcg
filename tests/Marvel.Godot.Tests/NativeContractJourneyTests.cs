using System.Diagnostics;
using Marvel.Protocol;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Tests;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class NativeContractJourneyTests
{
    private static readonly string[] JourneyOperations =
        [EngineProtocol.Setup, EngineProtocol.Open, EngineProtocol.Resolve, EngineProtocol.Sync, EngineProtocol.Close];
    public static bool NativeRequested => Environment.GetEnvironmentVariable("MARVEL_NATIVE_CONTRACT") is not null;

    [Fact(Skip = "Run through the native case manifest with a prepared Core contract.", SkipUnless = nameof(NativeRequested))]
    public async Task PreparedCoreSceneUsesRealControlsAndAuthoritativeSocketResults()
    {
        string contract = Environment.GetEnvironmentVariable("MARVEL_NATIVE_CONTRACT")!;
        string directory = Path.Combine(Path.GetTempPath(), "marvel-native-contract-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        await using var server = new NativeContractServer(contract, directory);
        bool rendered = Environment.GetEnvironmentVariable("MARVEL_NATIVE_RENDERED") == "true";
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("GODOT_BIN")!)
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
        };
        foreach (string argument in new[] { rendered ? "--minimized" : "--headless", "--audio-driver", "Dummy", "--path",
            Path.Combine(RepositoryPaths.Root, "src", "Marvel.Godot"), "--script", "res://smoke/native_contract_smoke.gd" })
            start.ArgumentList.Add(argument);
        start.Environment["MARVEL_ENGINE_ENDPOINT"] = server.Endpoint;
        start.Environment["MARVEL_NATIVE_CONTROL"] = directory;
        using Process process = Process.Start(start)!;
        Task<string> output = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        Task<string> error = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(rendered ? 180 : 80));
        string log;
        try
        {
            Task finished = await Task.WhenAny(process.WaitForExitAsync(deadline.Token), server.Completion);
            await finished;
        }
        finally
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            log = await output + await error;
            TestContext.Current.TestOutputHelper!.WriteLine(log);
            TestContext.Current.TestOutputHelper.WriteLine("Host requests: " + string.Join(",", server.Requests.Select(request => request.Operation)));
            foreach (EngineResponse response in server.Responses.Where(response => response.Error is not null))
                TestContext.Current.TestOutputHelper.WriteLine("Host rejection: " + response.Error);
            if (server.SceneFailure is { } sceneFailure)
                TestContext.Current.TestOutputHelper.WriteLine("Scene failure: " + sceneFailure);
        }
        Assert.Equal(0, process.ExitCode);
        Assert.DoesNotContain("ERROR:", log);
        Assert.Contains("NATIVE_CONTRACT_OK " + contract, log);
        Assert.All(server.Responses, response => Assert.Null(response.Error));
        VerifyAuthority(contract, server);
    }

    private static void VerifyAuthority(string contract, NativeContractServer server)
    {
        Assert.All(server.Requests, request => Assert.Contains(request.Operation, JourneyOperations));
        EngineRequest[] mutations = server.Requests.Where(request => request.Operation == EngineProtocol.Resolve).ToArray();
        int expected = contract switch
        {
            "dense-table" => 0,
            "melee-sequential" => 3,
            "beat-cop-move" or "beat-cop-damage" => 2,
            "heimdall-order" => 4,
            "widow-preparation" => 3,
            "end-phase" or "card-look" or "bruno-0" or "bruno-1" or "bruno-3" => 2,
            "wakanda-daggers-first" or "wakanda-claws-first" => 4,
            _ => 1,
        };
        Assert.Equal(expected, mutations.Length);
        if (expected == 0) return;
        VerifyOutcome(contract, server);
        if (contract == "lost-reply") Assert.Contains(server.Requests, request => request.Operation == EngineProtocol.Sync);
        if (contract.StartsWith("wakanda", StringComparison.Ordinal)) VerifyWakanda(contract, server, mutations);
        if (contract == "variable-payment") VerifyVariable(server, mutations);
        if (contract == "repeated-targets") VerifyRepeated(server, mutations);
        VerifyCardChoices(contract, server, mutations);
        VerifyAbilityContracts(contract, server, mutations);
    }

    private static void VerifyAbilityContracts(string contract, NativeContractServer server, EngineRequest[] mutations)
    {
        if (contract.StartsWith("beat-cop-", StringComparison.Ordinal)) BeatCopNativeAssertions.Verify(contract, server, mutations);
        if (contract.StartsWith("widow-", StringComparison.Ordinal)) WidowNativeAssertions.Verify(contract, server, mutations);
        if (contract == "lockjaw-discard") LockjawNativeAssertions.Verify(server, mutations);
        if (contract == "melee-sequential") MeleeNativeAssertions.Verify(server, mutations);
        if (contract == "preemptive-strike") PreemptiveStrikeNativeAssertions.Verify(server, mutations);
        if (contract == "defeat-context") DefeatContextNativeAssertions.Verify(server, mutations);
        if (contract == "lightning-payment") LightningNativeAssertions.Verify(server, mutations);
        if (contract == "heimdall-order") HeimdallNativeAssertions.Verify(server, mutations);
        if (contract.StartsWith("bruno-", StringComparison.Ordinal)) BrunoNativeAssertions.Verify(contract, server, mutations);
    }

    private static void VerifyCardChoices(string contract, NativeContractServer server, EngineRequest[] mutations)
    {
        if (contract == "card-look") VerifyLook(server, mutations);
        if (contract == "resource-declaration")
        {
            ResourceAllocation payment = Assert.Single(Assert.Single(mutations).Decision!.Allocated);
            Assert.Equal("BY", payment.PaidAs);
            var world = server.Responses.Last(response => response.World is not null).World!;
            var scheme = Assert.Single(world.Areas.Single(area => area.Zone == "MainSchemesArea").Cards);
            Assert.Equal(1, scheme.State!.Threat);
        }
    }

    private static void VerifyLook(NativeContractServer server, EngineRequest[] mutations)
    {
        Assert.Equal(2, mutations.Length);
        Assert.Contains(server.Responses, response => response.Prompt?.PublicKind == Marvel.Rules.Prompts.PublicDecisionKind.CardLook);
        var world = server.Responses.Last(response => response.World is not null).World!;
        Assert.Contains(world.Areas.Single(area => area.Zone == "HandsArea").Cards, card => card.Face?.Title == "Mark V Armor");
        var discarded = world.Areas.Single(area => area.Zone == "DiscardPile").Cards;
        Assert.Contains(discarded, card => card.Face?.Title == "Arc Reactor");
        Assert.Contains(discarded, card => card.Face?.Title == "Powered Gauntlets");
        Assert.All(world.Areas.Single(area => area.Zone == "PlayerDeck").Cards, card => Assert.Null(card.Face));
    }

    private static void VerifyOutcome(string contract, NativeContractServer server)
    {
        if (contract is "victory" or "keyboard-target")
        {
            Assert.Equal(Outcome.PlayersWin, server.Responses.Last(response => response.World is not null).World!.Outcome);
            Assert.Null(server.Responses.Last(response => response.World is not null).Prompt);
        }
        if (contract == "end-phase")
            Assert.Contains(server.Responses.SelectMany(response => response.Events).OfType<CardsMoved>(),
                move => move.From.Zone == "HandsArea" && move.To.Zone == "DiscardPile" && move.Cards.Count == 1);
    }

    private static void VerifyVariable(NativeContractServer server, EngineRequest[] mutations)
    {
        var decision = Assert.Single(mutations).Decision!;
        Assert.Equal(2, decision.Values!["X"]);
        Assert.Single(decision.Resources!);
        var world = server.Responses.Last(response => response.World is not null).World!;
        var channel = Assert.Single(world.Areas.Where(area => area.Zone == "UpgradesArea").SelectMany(area => area.Cards),
            card => card.Face?.Title == "Energy Channel");
        Assert.Equal(2, channel.State!.Counters["energy"]);
    }

    private static void VerifyRepeated(NativeContractServer server, EngineRequest[] mutations)
    {
        var initial = server.Responses.First(response => response.World is not null).World!;
        var hero = Assert.Single(initial.Areas.SelectMany(area => area.Cards), card => card.Face?.Title == "Spider-Man");
        var ally = Assert.Single(initial.Areas.SelectMany(area => area.Cards), card => card.Face?.Title == "Black Cat");
        int heroId = Assert.IsType<int>(hero.Id);
        int allyId = Assert.IsType<int>(ally.Id);
        Assert.Equal([heroId, heroId, allyId], Assert.Single(mutations).Decision!.Targets);
        var final = server.Responses.Last(response => response.World is not null).World!;
        var cards = final.Areas.SelectMany(area => area.Cards).Where(card => card.Id.HasValue).ToDictionary(card => card.Id!.Value);
        Assert.Equal(hero.State!.Damage + 2, cards[heroId].State!.Damage);
        Assert.Equal(ally.State!.Damage + 1, cards[allyId].State!.Damage);
    }

    private static void VerifyWakanda(string contract, NativeContractServer server, EngineRequest[] mutations)
    {
        Assert.Equal(4, mutations.Length);
        Assert.Single(server.Responses, response => response.Prompt?.PublicKind == Marvel.Rules.Prompts.PublicDecisionKind.SpecialAbilityNext);
        var world = server.Responses.Last(response => response.World is not null).World!;
        var villain = Assert.Single(world.Areas.Single(area => area.Zone == "VillainArea").Cards);
        Assert.Equal(contract == "wakanda-daggers-first" ? 5 : 4, villain.State!.Damage);
        Assert.Contains(world.Areas.Single(area => area.Zone == "DiscardPile").Cards, card => card.Face?.Title == "Wakanda Forever!");
        Assert.Equal(Marvel.Rules.Prompts.PublicDecisionKind.PlayerAction, server.Responses.Last(response => response.World is not null).Prompt!.PublicKind);
    }
}

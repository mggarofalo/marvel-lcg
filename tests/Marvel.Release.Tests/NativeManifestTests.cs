using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Marvel.Tests;
using Xunit;

namespace Marvel.Release.Tests;

public sealed class NativeManifestTests
{
    [Fact]
    public void SharedManifestRetainsEveryReviewedCaseAndItsPlatformIndependentInputs()
    {
        using JsonDocument manifest = Read();
        Dictionary<string, JsonElement> cases = Cases(manifest);
        string[] baseline = NativeCaseBaseline.Ids().ToArray();
        Assert.Equal(125, baseline.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(125, baseline.Length);
        foreach (string id in baseline)
        {
            Assert.True(cases.TryGetValue(id, out JsonElement entry), $"Missing reviewed native obligation: {id}");
            Match match = Regex.Match(id, @"^(.*)-(\d+)-(motion|reduced|narrow|two-player)$");
            Assert.True(match.Success);
            int scale = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            string name = match.Groups[1].Value;
            string suffix = match.Groups[3].Value;
            Assert.Equal($"res://smoke/{name.Replace('-', '_')}_smoke.gd", entry.GetProperty("script").GetString());
            Assert.Equal(scale, entry.GetProperty("scale").GetInt32());
            Assert.Equal(suffix == "reduced" ? "disabled" : "enabled", entry.GetProperty("motion").GetString());
            int players = suffix == "two-player" || name == "layout-continuity"
                || name == "recipient-commitment" && scale == 150 ? 2 : 1;
            Assert.Equal(players, entry.GetProperty("players").GetInt32());
            string[] profiles = Strings(entry, "profiles");
            Assert.Contains("exhaustive", profiles);
            bool representative = name != "local-game" || suffix != "motion" || scale == 100;
            Assert.Equal(representative, profiles.Contains("representative", StringComparer.Ordinal));
        }
    }

    [Fact]
    public void CasesHaveBoundedExecutionObservableCompletionAndResolvableFixtures()
    {
        using JsonDocument manifest = Read();
        Assert.Equal(1, manifest.RootElement.GetProperty("schema").GetInt32());
        JsonElement fixtures = manifest.RootElement.GetProperty("fixtures");
        foreach ((string id, JsonElement entry) in Cases(manifest))
        {
            Assert.Matches("^[a-z0-9-]+$", id);
            Assert.InRange(entry.GetProperty("timeoutSeconds").GetInt32(), 1, 600);
            Assert.All(Strings(entry, "markers"), marker => Assert.False(string.IsNullOrWhiteSpace(marker)));
            Assert.NotEmpty(Strings(entry, "markers"));
            Assert.NotEmpty(Strings(entry, "profiles"));
            Assert.All(Strings(entry, "profiles"), profile => Assert.Contains(profile,
                new List<string> { "representative", "exhaustive", "inspection-all-scales" }));
            string script = entry.GetProperty("script").GetString()!;
            Assert.StartsWith("res://smoke/", script, StringComparison.Ordinal);
            Assert.DoesNotContain("..", script, StringComparison.Ordinal);
            Assert.True(File.Exists(RepositoryPaths.Repository("src", "Marvel.Godot", script[6..])), script);
            Assert.InRange(entry.GetProperty("players").GetInt32(), 1, 4);
            int scale = entry.GetProperty("scale").GetInt32();
            Assert.InRange(scale, 50, 150);
            Assert.Equal(0, scale % 10);
            string[] viewport = entry.GetProperty("viewport").GetString()!.Split('x');
            Assert.Equal(2, viewport.Length);
            Assert.True(int.Parse(viewport[0], CultureInfo.InvariantCulture) >= 1920);
            Assert.True(int.Parse(viewport[1], CultureInfo.InvariantCulture) >= 1080);
            if (entry.TryGetProperty("fixture", out JsonElement fixture))
                Assert.True(fixtures.TryGetProperty(fixture.GetString()!, out _), $"{id} names an unknown fixture");
        }
    }

    [Fact]
    public async Task IndependentCaseSelectionDoesNotSilentlyDropAnUnknownSelector()
    {
        // The runner's selection boundary is executable without Godot or a build.
        var start = new System.Diagnostics.ProcessStartInfo("pwsh")
        {
            WorkingDirectory = RepositoryPaths.Root, UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
        };
        foreach (string argument in new[] { "-NoProfile", "-File", "tools/native-cases.ps1", "-List", "-Case", "missing-obligation" })
            start.ArgumentList.Add(argument);
        using var process = System.Diagnostics.Process.Start(start)!;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        var output = process.StandardOutput.ReadToEndAsync(deadline.Token);
        var error = process.StandardError.ReadToEndAsync(deadline.Token);
        try { await process.WaitForExitAsync(deadline.Token); }
        catch (OperationCanceledException) { process.Kill(true); throw; }
        Assert.NotEqual(0, process.ExitCode);
        Assert.Contains("No native cases match", await output + await error, StringComparison.Ordinal);
    }

    private static JsonDocument Read() => JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Repository("tools", "native-cases.json")));
    private static Dictionary<string, JsonElement> Cases(JsonDocument manifest) => manifest.RootElement.GetProperty("cases")
        .EnumerateArray().ToDictionary(entry => entry.GetProperty("id").GetString()!, StringComparer.Ordinal);
    private static string[] Strings(JsonElement entry, string property) => entry.GetProperty(property)
        .EnumerateArray().Select(value => value.GetString()!).ToArray();
}

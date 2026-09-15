using System.Text.Json;
using Marvel.Tests;
using Xunit;

namespace Marvel.Architecture.Tests;

public sealed class CommitGatePolicyTests
{
    [Fact]
    public void CompleteReleaseBuildPrecedesTestsThatConsumeIt()
    {
        using JsonDocument configuration = JsonDocument.Parse(File.ReadAllText(
            RepositoryPaths.Repository(".husky", "task-runner.json")));
        JsonElement[] tasks = configuration.RootElement.GetProperty("tasks")
            .EnumerateArray().ToArray();
        int build = Array.FindIndex(tasks, task => Name(task) == "release-build");
        int tests = Array.FindIndex(tasks, task => Name(task) == "all-tests");
        int native = Array.FindIndex(tasks, task => Name(task) == "native-smoke");

        Assert.True(build >= 0 && tests > build && native > tests,
            "The complete Release build, managed tests and native smoke must run in order.");
        Assert.Equal(
            ["husky", "exec", ".husky/csx/release-build.csx"], Args(tasks[build]));
        Assert.Contains("start.ArgumentList.Add(\"build\");", Script("release-build"));
        Assert.Contains("start.ArgumentList.Add(\"Marvel.slnx\");", Script("release-build"));
        Assert.Contains("start.ArgumentList.Add(\"Release\");", Script("release-build"));
        Assert.Contains("start.ArgumentList.Add(\"--warnaserror\");", Script("release-build"));
        Assert.Contains("start.ArgumentList.Add(\"Marvel.slnx\");", Script("all-tests"));
        Assert.Contains("start.ArgumentList.Add(\"--no-build\");", Script("all-tests"));
        Assert.Contains("godot-smoke.ps1", Script("native-smoke"));
        Assert.Contains("start.ArgumentList.Add(\"-Representative\");", Script("native-smoke"));
    }

    private static string Name(JsonElement task) => task.GetProperty("name").GetString()!;

    private static string[] Args(JsonElement task) =>
        [.. task.GetProperty("args").EnumerateArray().Select(value => value.GetString()!)];

    private static string Script(string name) => File.ReadAllText(
        RepositoryPaths.Repository(".husky", "csx", $"{name}.csx"));
}

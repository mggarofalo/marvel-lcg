using System.Text.Json;
using Marvel.Tests;
using Xunit;

namespace Marvel.Core.Tests;

public sealed class DevelopmentWorkflowTests
{
    [Fact]
    public void PreCommitRunsPinnedComplexityLengthAndUnitTestGates()
    {
        string root = RepositoryPaths.Root;
        using JsonDocument tools = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(
            root, ".config", "dotnet-tools.json")));
        JsonElement husky = tools.RootElement.GetProperty("tools").GetProperty("husky");
        Assert.Equal("0.9.1", husky.GetProperty("version").GetString());
        Assert.False(husky.GetProperty("rollForward").GetBoolean());
        Assert.Equal("lizard==1.24.0\n", File.ReadAllText(Path.Combine(
            root, "requirements-dev.txt")).Replace("\r\n", "\n", StringComparison.Ordinal));

        using JsonDocument runner = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(
            root, ".husky", "task-runner.json")));
        Dictionary<string, JsonElement> tasks = runner.RootElement.GetProperty("tasks")
            .EnumerateArray()
            .ToDictionary(task => task.GetProperty("name").GetString()!, task => task);
        Assert.Equal(
            ["--CCN", "10", "--warnings_only", "${staged}"],
            Arguments(tasks["cyclomatic-complexity"]));
        Assert.Equal("staged", tasks["cyclomatic-complexity"]
            .GetProperty("filteringRule").GetString());
        Assert.Equal(
            ["src/**/*.cs", "src/**/*.gd", "tests/**/*.cs", "tools/**/*.cs"],
            Includes(tasks["cyclomatic-complexity"]));
        Assert.Equal(
            ["husky", "exec", ".husky/csx/file-length.csx", "--args", "${staged}"],
            Arguments(tasks["file-length"]));
        Assert.Equal("staged", tasks["file-length"]
            .GetProperty("filteringRule").GetString());
        Assert.Equal(
            ["**/*.cs", "**/*.csx", "**/*.gd", "**/*.ps1", "**/*.sh"],
            Includes(tasks["file-length"]));
        Assert.Equal(
            ["datasets/**/*"],
            Strings(tasks["file-length"], "exclude"));
        Assert.Equal(
            ["test", "tests/Marvel.UnitTests.slnx", "--configuration", "Release", "--nologo"],
            Arguments(tasks["unit-tests"]));
        Assert.All(tasks.Values, task =>
            Assert.Equal("pre-commit", task.GetProperty("group").GetString()));

        string hook = File.ReadAllText(Path.Combine(root, ".husky", "pre-commit"));
        Assert.Contains("dotnet husky run --group pre-commit", hook, StringComparison.Ordinal);
        string buildTargets = File.ReadAllText(Path.Combine(root, "Directory.Build.targets"));
        Assert.Contains("Name=\"MarvelInstallHusky\"", buildTargets, StringComparison.Ordinal);
        Assert.Contains("Exists('$(MSBuildThisFileDirectory).git')", buildTargets, StringComparison.Ordinal);
        Assert.Contains("'$(CI)' != 'true'", buildTargets, StringComparison.Ordinal);
        string lengthGate = File.ReadAllText(Path.Combine(
            root, ".husky", "csx", "file-length.csx"));
        Assert.Contains("const int MaximumLines = 500;", lengthGate, StringComparison.Ordinal);
    }

    private static string[] Arguments(JsonElement task) => task.GetProperty("args")
        .EnumerateArray()
        .Select(argument => argument.GetString()!)
        .ToArray();

    private static string[] Includes(JsonElement task) => Strings(task, "include");

    private static string[] Strings(JsonElement task, string property) => task
        .GetProperty(property)
        .EnumerateArray()
        .Select(element => element.GetString()!)
        .ToArray();
}

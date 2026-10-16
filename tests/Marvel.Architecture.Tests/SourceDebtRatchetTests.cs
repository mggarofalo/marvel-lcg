using System.Diagnostics;
using Marvel.Tests;
using Xunit;

namespace Marvel.Architecture.Tests;

public sealed class SourceDebtRatchetTests
{
    private const string Baseline = "tests/Marvel.Architecture.Tests/source-layout-baseline.txt";

    [Fact]
    public void ReviewedDebtCanOnlyShrinkFromTheBranchBase()
    {
        // Architecture choice: editing the current baseline cannot authorize more debt.
        // CI supplies its fetched comparison commit; local commits compare the branch fork.
        string? comparison = Environment.GetEnvironmentVariable("MARVEL_ARCHITECTURE_BASE");
        comparison = string.IsNullOrWhiteSpace(comparison)
            ? Git("merge-base", "HEAD", "origin/main").Trim() : comparison;
        string previous = Git("show", $"{comparison}:{Baseline}");
        Assert.Empty(SourceDebtRatchet.Increases(
            previous.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries),
            File.ReadAllLines(Path.Combine(RepositoryPaths.Root, Baseline))));
    }

    [Theory]
    [InlineData("src/A.cs:9: SL003: A=350", "src/A.cs:28: SL003: A=349")]
    [InlineData("src/A.cs:9: SL005: Apply=9", "src/A.cs:100: SL005: Apply=9")]
    public void ShrinkageAndLineMovementAreAllowed(string before, string after) =>
        Assert.Empty(SourceDebtRatchet.Increases([before], [after]));

    [Theory]
    [InlineData("src/A.cs:9: SL003: A=350", "src/A.cs:9: SL003: A=351")]
    [InlineData("src/A.cs:9: SL005: Apply=9", "src/A.cs:9: SL005: Apply=10")]
    [InlineData("src/A.cs:9: SL005: Apply=9", "src/B.cs:9: SL005: Apply=9")]
    public void BaselineEditsCannotAdmitGrowthOrNewOwners(string before, string after) =>
        Assert.Single(SourceDebtRatchet.Increases([before], [after]));

    [Fact]
    public void AnAdditionalOverloadCannotBorrowAnotherOverloadsAdmission()
    {
        Assert.Single(SourceDebtRatchet.Increases(["src/A.cs:9: SL005: Apply=10"],
            ["src/A.cs:9: SL005: Apply=9", "src/A.cs:30: SL005: Apply=8"]));
        Assert.Empty(SourceDebtRatchet.Increases(
            ["src/A.cs:9: SL005: Apply=10", "src/A.cs:30: SL005: Apply=8"],
            ["src/A.cs:40: SL005: Apply=8"]));
    }

    private static string Git(params string[] arguments)
    {
        var start = new ProcessStartInfo("git")
        {
            WorkingDirectory = RepositoryPaths.Root, RedirectStandardOutput = true,
            RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true,
        };
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        using Process process = Process.Start(start)!;
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, $"Cannot establish reviewed source debt: {error}");
        return output;
    }
}

using System.Reflection;
using Marvel.Rules.Play;
using Xunit;

namespace Marvel.Architecture.Tests;

public sealed class AgendaProcedureBoundaryTests
{
    [Fact]
    public void VillainPhaseOnlyPlansTheVillainPhase()
    {
        string[] methods = typeof(VillainPhase)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(method => method.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal([nameof(VillainPhase.Schedule)], methods);
    }

    [Fact]
    public void SequenceRoutesAgendaOperationsThroughTheNeutralDispatcher()
    {
        string source = File.ReadAllText(Path.Combine(
            Marvel.Tests.RepositoryPaths.Root,
            "src", "Marvel.Rules", "Play", "Sequence.cs"));

        Assert.Contains("AgendaProcedures.ApplyWithWorldAbilities", source, StringComparison.Ordinal);
        Assert.Contains("AgendaProcedures.AnswerWithWorldAbilities", source, StringComparison.Ordinal);
        Assert.DoesNotContain("VillainPhase.Take", source, StringComparison.Ordinal);
        Assert.DoesNotContain("VillainPhase.Answer", source, StringComparison.Ordinal);
    }
}

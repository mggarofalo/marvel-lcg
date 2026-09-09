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

    [Fact]
    public void NeutralDispatcherDelegatesAttackOperationsToTheirOwner()
    {
        string dispatcher = File.ReadAllText(Path.Combine(
            Marvel.Tests.RepositoryPaths.Root,
            "src", "Marvel.Rules", "Play", "VillainPhase.cs"));

        Assert.Contains("AttackProcedure.Apply", dispatcher, StringComparison.Ordinal);
        Assert.Contains("AttackProcedure.Answer", dispatcher, StringComparison.Ordinal);
        Assert.DoesNotContain("Attack.Initiate", dispatcher, StringComparison.Ordinal);
        Assert.DoesNotContain("Attack.DeclareDefender", dispatcher, StringComparison.Ordinal);
    }

    [Fact]
    public void NeutralDispatcherHasExplicitRequiredProcedureOwners()
    {
        string source = File.ReadAllText(Path.Combine(
            Marvel.Tests.RepositoryPaths.Root,
            "src", "Marvel.Rules", "Play", "VillainPhase.cs"));

        string[] owners =
        [
            "AttackProcedure",
            "ThreatProcedure",
            "RevealProcedure",
            "DefeatProcedure",
            "PlayerActionProcedure",
            "AbilityContinuationProcedure",
        ];

        foreach (string owner in owners)
        {
            Assert.Contains(owner, source, StringComparison.Ordinal);
        }

        Assert.Contains("step.Operation.Procedure switch", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AgendaStepsStoreClosedProcedureSpecificOperations()
    {
        var attack = new PhaseStep(Steps.Attack, 1, 2, Subject: 7, Seat: 0);
        var reveal = new PhaseStep(Steps.RevealEncounterCard, 1, 4, Subject: 8, Seat: 0);
        var continuation = new PhaseStep(
            Steps.ChooseOption,
            1,
            0,
            Subject: 9,
            Seat: 0,
            AbilityPath: ["sequence:0"]);

        Assert.Equal(AgendaProcedureKind.Attack, attack.Operation.Procedure);
        Assert.Equal(AgendaProcedureKind.Reveal, reveal.Operation.Procedure);
        Assert.Equal(AgendaProcedureKind.AbilityContinuation, continuation.Operation.Procedure);
        Assert.NotEqual(attack.Operation.GetType(), reveal.Operation.GetType());
        Assert.NotEqual(reveal.Operation.GetType(), continuation.Operation.GetType());
    }

    [Fact]
    public void UnknownAgendaOperationCannotBeConstructed()
    {
        Assert.Throws<RulesNotImplementedException>(() =>
            new PhaseStep("InventedOperation", 1, 1));
    }

    [Fact]
    public void OneClosedOperationCannotBeRetaggedAsAnotherProceduresPayload()
    {
        var attack = new PhaseStep(Steps.Attack, 1, 2, Subject: 7, Seat: 0);

        Assert.Throws<InvalidOperationException>(() =>
            attack with { What = Steps.RevealEncounterCard });
    }
}

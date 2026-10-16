using System.Reflection;
using Marvel.Rules.Play;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Marvel.Architecture.Tests;

public sealed class AgendaProcedureBoundaryTests
{
    private static readonly Lazy<CSharpCompilation> RulesSource = new(() =>
        PresentationSourceCompilation.Compile(Path.Combine(
            Marvel.Tests.RepositoryPaths.Root, "src", "Marvel.Rules")));

    [Fact]
    public void VillainPhaseOnlyPlansTheVillainPhase()
    {
        string[] methods = typeof(VillainPhase)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(method => method.Name).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal([nameof(VillainPhase.Schedule)], methods);
    }

    [Fact]
    public void SequenceDispatchesThroughTheNeutralOwner()
    {
        string[] calls = CalledMembers("Sequence.cs");
        Assert.Contains("Marvel.Rules.Play.AgendaProcedures.ApplyWithWorldAbilities", calls);
        Assert.Contains("Marvel.Rules.Play.AgendaProcedures.AnswerWithWorldAbilities", calls);
        Assert.DoesNotContain(calls, call => call.StartsWith("Marvel.Rules.Play.VillainPhase.", StringComparison.Ordinal));
    }

    [Fact]
    public void DispatcherUsesProcedureOwnersInsteadOfImplementingTheirRules()
    {
        string[] calls = CalledMembers("AgendaProcedures.cs");
        foreach (string owner in new[] { "AttackProcedure", "ThreatProcedure", "RevealProcedure",
                     "DefeatProcedure", "PlayerActionProcedure", "AbilityContinuationProcedure" })
            Assert.Contains($"Marvel.Rules.Play.{owner}.Apply", calls);
        Assert.Contains("Marvel.Rules.Play.AttackProcedure.Answer", calls);
        Assert.DoesNotContain(calls, call => call.StartsWith("Marvel.Rules.Play.Attack.", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("var step = new PhaseStep(\"InventedOperation\", 1, 1);", "CS1503")]
    [InlineData("var step = new PhaseStep(new EnemyAttackOperation(ProcedureAbilities: []), 1, 1);", "CS1739")]
    [InlineData("var step = new PhaseStep(new EnemyAttackOperation(), 1, 1) with { What = Steps.RevealEncounterCard };", "CS0200")]
    [InlineData("var step = new PhaseStep(new EnemyAttackOperation(), 1, 1) with { ProcedureAbilities = new PendingAbility[0] };", "CS0117")]
    public void InvalidOperationShapesAreRejectedByTheCompiler(string statement, string diagnostic)
    {
        // Engine API choice: invalid combinations cannot be represented by a
        // normal consumer. Compile a valid control before trusting a rejection.
        Assert.Empty(CompileConsumer("var step = new PhaseStep(new EnemyAttackOperation(), 1, 1);"));
        Diagnostic[] errors = CompileConsumer(statement);
        Assert.NotEmpty(errors);
        Assert.All(errors, error => Assert.Equal(diagnostic, error.Id));
    }

    [Theory]
    [InlineData("CharacterAttackOperation", "CharacterAttack")]
    [InlineData("CharacterThwartOperation", "CharacterThwart")]
    public void CharacterPowerOperationsRequireTheirOwnedPayload(string operation, string payload)
    {
        Assert.Empty(CompileConsumer($"var step = new {operation}(new {payload}(1, 2, 0));"));
        Diagnostic[] errors = CompileConsumer($"var step = new {operation}();");
        Assert.NotEmpty(errors);
        Assert.All(errors, error => Assert.Equal("CS7036", error.Id));
    }

    [Fact]
    public void OperationKindsCannotBeInventedOutsideTheEngineAssembly()
    {
        const string declaration = """
            public sealed record Invented : AgendaOperation
            {
                public override string What => "Invented";
            }
            """;
        Assert.Empty(CompileConsumer("var step = new PhaseStep(new EnemyAttackOperation(), 1, 1);"));
        Diagnostic[] errors = CompileConsumer("", declaration);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, error => error.Id == "CS0122");
        Assert.Contains(errors, error => error.Id == "CS0534");
    }

    [Fact]
    public void RecordCopyConstructorsCannotForgeNewOperationKinds()
    {
        const string declaration = """
            public sealed record Forged : AgendaOperation
            {
                public Forged(AgendaOperation original) : base(original) { }
                public override string What => "Invented";
            }
            """;
        Assert.Empty(CompileConsumer("var copied = new EnemyAttackOperation() with { Character = 4 };"));
        Diagnostic[] errors = CompileConsumer("", declaration);
        Assert.NotEmpty(errors);
        Assert.All(errors, error => Assert.Equal("CS0534", error.Id));
    }

    private static Diagnostic[] CompileConsumer(string statement, string declaration = "")
    {
        string source = $$"""
            using Marvel.Rules.Play;
            using Marvel.Rules.Timing;
            using Marvel.Rules.State;
            public static class Consumer { public static void Use() { {{statement}} } }
            {{declaration}}
            """;
        return CSharpCompilation.Create("Agenda.Consumer.Probe",
                [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest))],
                PresentationSourceCompilation.References(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error).ToArray();
    }

    private static string[] CalledMembers(string file)
    {
        CSharpCompilation compilation = RulesSource.Value;
        SyntaxTree tree = Assert.Single(compilation.SyntaxTrees,
            item => Path.GetFileName(item.FilePath) == file);
        SemanticModel model = compilation.GetSemanticModel(tree);
        return tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Select(call => model.GetSymbolInfo(call).Symbol).OfType<IMethodSymbol>()
            .Select(method => $"{method.ContainingType.ToDisplayString()}.{method.Name}")
            .Distinct(StringComparer.Ordinal).ToArray();
    }
}

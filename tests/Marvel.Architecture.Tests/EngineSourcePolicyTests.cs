using Marvel.Tests;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Marvel.Architecture.Tests;

public sealed class EngineSourcePolicyTests
{
    [Theory]
    [InlineData("Marvel.Core")]
    [InlineData("Marvel.Rules")]
    [InlineData("Marvel.Cards")]
    [InlineData("Marvel.Content")]
    [InlineData("Marvel.Session")]
    public void EngineSourcesOwnTheirStreamsAndHaveNoSharedMutableState(string project)
    {
        CSharpCompilation compilation = PresentationSourceCompilation.Compile(
            Path.Combine(RepositoryPaths.Root, "src", project));
        string[] violations = EngineSourcePolicy.Violations(compilation);
        Assert.True(violations.Length == 0, string.Join("\n", violations));
        Assert.Empty(EngineStaticStatePolicy.Violations(compilation));
    }

    [Theory]
    [InlineData("using Clock = System.DateTime; class C { object Read() => Clock.UtcNow; }")]
    [InlineData("using static System.Guid; class C { object Read() => NewGuid(); }")]
    [InlineData("class C { System.Random Read() => new(7); }")]
    [InlineData("class C { int Read() => System.Environment.CurrentManagedThreadId; }")]
    [InlineData("class C { System.Threading.Tasks.Task Read() => System.Threading.Tasks.Task.CompletedTask; }")]
    [InlineData("class C { string Read() => System.IO.File.ReadAllText(\"x\"); }")]
    public void CompiledAmbientCapabilitiesAreRejectedRegardlessOfSpelling(string source)
    {
        Assert.Empty(EngineSourcePolicy.Violations(Compile("class C { int Read(int value) => value + 1; }")));
        Assert.Contains(EngineSourcePolicy.Violations(Compile(source)), item => item.StartsWith("ambient:", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("class C { Marvel.Core.Random.EngineRandom Read() => new(7); }")]
    [InlineData("using R = Marvel.Core.Random.MersenneTwister; class C { R Read() => new(7); }")]
    [InlineData("class C { void Read(Marvel.Core.Random.MersenneTwister rng) => rng.SetState(rng.GetState()); }")]
    [InlineData("class C { System.Action<Marvel.Core.Random.MersenneTwisterState> Read(Marvel.Core.Random.MersenneTwister rng) => rng.SetState; }")]
    public void CompiledRandomConstructionAndRestorationNeedTheirExactOwner(string source)
    {
        Assert.Empty(EngineSourcePolicy.Violations(Compile(
            "class C { uint Read(Marvel.Core.Random.MersenneTwister rng) => rng.NextUInt32(); }")));
        Assert.Contains(EngineSourcePolicy.Violations(Compile(source)), item => item.StartsWith("rng-owner:", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Marvel.Core.Random.EngineRandom", "SetSeed", "CS1061")]
    [InlineData("Marvel.Core.Random.MersenneTwister", "Seed", "CS1061")]
    public void ConsumersCannotReseedAnExistingGameStream(string type, string member, string expected)
    {
        Compile("class C { uint Read(Marvel.Core.Random.MersenneTwister rng) => rng.NextUInt32(); }");
        Diagnostic[] errors = CreateCompilation($"class C {{ void Read({type} rng) => rng.{member}(7); }}")
            .GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.NotEmpty(errors);
        Assert.All(errors, error => Assert.Equal(expected, error.Id));
    }

    [Fact]
    public void EvenTheOwningConstructorCannotCreateASecondGameplayStream()
    {
        const string start = "namespace Marvel.Rules.State { class World { public World(uint seed) { ";
        const string create = "var first = new Marvel.Core.Random.EngineRandom(seed);";
        Assert.Empty(EngineSourcePolicy.Violations(Compile(start + create + " } } }")));
        Assert.Contains(EngineSourcePolicy.Violations(Compile(start + create
            + "var second = new Marvel.Core.Random.EngineRandom(seed); } } }")),
            item => item.Contains("multiple construction/seeding sites", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("class C { static int Counter; }")]
    [InlineData("class C { static readonly int[] Values = [1]; }")]
    [InlineData("class C { static System.Collections.Generic.IReadOnlyList<int> Values { get; } = new int[] {1}; }")]
    [InlineData("class C { static readonly System.Collections.Generic.Dictionary<object, object> Cache = new(); }")]
    [InlineData("class C { static readonly System.Collections.Immutable.ImmutableArray<int[]> Values = [new int[] {1}]; }")]
    [InlineData("class C { static readonly System.Collections.Immutable.ImmutableArray<int>.Builder Values = System.Collections.Immutable.ImmutableArray.CreateBuilder<int>(); }")]
    [InlineData("class C { static readonly System.Collections.Immutable.ImmutableDictionary<int,int>.Builder Values = System.Collections.Immutable.ImmutableDictionary.CreateBuilder<int,int>(); }")]
    [InlineData("class C { static System.Collections.Generic.IReadOnlySet<string> Values { get; } = new System.Collections.Generic.HashSet<string>(); }")]
    [InlineData("class C { static event System.Action Changed; }")]
    [InlineData("record B { public System.Collections.Generic.List<int> Values { get; } = new(); } record D : B; class C { static readonly D Shared = new(); }")]
    [InlineData("class C { static int Counter { get; set; } }")]
    public void CompiledStaticStateCannotHideBehindReadonlyOrCollectionInterfaces(string source)
    {
        Assert.Empty(EngineStaticStatePolicy.Violations(Compile(
            "class C { static readonly System.Collections.Immutable.ImmutableArray<int> Values = [1,2]; }")));
        Assert.Empty(EngineStaticStatePolicy.Violations(Compile(
            "class C { static readonly System.Collections.Immutable.ImmutableDictionary<int,int> Values = System.Collections.Immutable.ImmutableDictionary<int,int>.Empty; }")));
        Assert.NotEmpty(EngineStaticStatePolicy.Violations(Compile(source)));
    }

    [Fact]
    public void ReadonlyWrappersMustOwnTheirBackingDictionary()
    {
        const string prefix = "using System.Collections.Generic; using System.Collections.ObjectModel; ";
        Assert.Empty(EngineStaticStatePolicy.Violations(Compile(prefix +
            "class C { static readonly ReadOnlyDictionary<int,int> Values = new(new Dictionary<int,int> {{1,2}}); }")));
        Assert.NotEmpty(EngineStaticStatePolicy.Violations(Compile(prefix +
            "class C { static Dictionary<int,int> Backing => new(); static readonly ReadOnlyDictionary<int,int> Values = new(Backing); }")));
    }

    [Theory]
    [InlineData("ImmutableHashSet.Create<string>(COMPARER)")]
    [InlineData("ImmutableSortedSet.Create<string>(COMPARER)")]
    [InlineData("new string[] { \"one\" }.ToFrozenSet(COMPARER)")]
    public void SharedCollectionsCannotRetainMutableComparers(string construction)
    {
        const string prefix = "using System; using System.Collections.Generic; "
            + "using System.Collections.Immutable; using System.Collections.Frozen; ";
        const string comparer = "sealed class Mutable : IEqualityComparer<string>, IComparer<string> { "
            + "public bool IgnoreCase { get; set; } public bool Equals(string a,string b) => "
            + "string.Equals(a,b,IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal); "
            + "public int GetHashCode(string a) => IgnoreCase ? a.ToUpperInvariant().GetHashCode() : a.GetHashCode(); "
            + "public int Compare(string a,string b) => string.Compare(a,b,IgnoreCase); } ";
        string Source(string value) => prefix + comparer + "class C { static readonly object Values = "
            + construction.Replace("COMPARER", value, StringComparison.Ordinal) + "; }";
        Assert.Empty(EngineStaticStatePolicy.Violations(Compile(Source("StringComparer.Ordinal"))));
        Assert.Empty(EngineStaticStatePolicy.Violations(Compile(Source("(StringComparer)null"))));
        Assert.NotEmpty(EngineStaticStatePolicy.Violations(Compile(Source("new Mutable()"))));
    }

    private static CSharpCompilation Compile(string source)
    {
        CSharpCompilation compilation = CreateCompilation(source);
        using var emitted = new MemoryStream();
        var result = compilation.Emit(emitted);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics.Where(item => item.Severity == DiagnosticSeverity.Error)));
        return compilation;
    }

    private static CSharpCompilation CreateCompilation(string source) => CSharpCompilation.Create("Engine.Policy.Probe",
        [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest))],
        PresentationSourceCompilation.References(),
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
}

using Marvel.Tests;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Marvel.Architecture.Tests;

public sealed class TestQualityPolicyTests
{
    [Fact]
    public void TestsContainBehaviorAndDoNotRepeatIdenticalCasesWithinOneClass()
    {
        // This narrow syntax gate does not infer assertion quality, helper behavior,
        // equivalent algorithms, or coverage. Those remain behavioral review obligations.
        string tests = RepositoryPaths.Repository("tests") + Path.DirectorySeparatorChar;
        string[] diagnostics = SourceLayoutPolicyTests.ApplicationSources()
            .Where(path => path.StartsWith(tests, StringComparison.Ordinal))
            .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path),
                path: Path.GetRelativePath(RepositoryPaths.Root, path)))
            .SelectMany(TestQualityPolicy.Diagnostics).ToArray();
        Assert.True(diagnostics.Length == 0, string.Join(Environment.NewLine, diagnostics));
    }

    [Theory]
    [InlineData("[Fact] public void Empty() { }", "TQ001")]
    [InlineData("[Fact] public void Empty() { /* not behavior */ ; }", "TQ001")]
    [InlineData("[Fact] public void First() { Assert.Equal(2, 1 + 1); } [Fact] public void Second() { /* different prose */ Assert.Equal(2, 1+1); }", "TQ002")]
    [InlineData("[Theory, InlineData(1)] public void First(int value) => Assert.Equal(1, value); [Theory, InlineData(1)] public void Second(int value) => Assert.Equal(1, value);", "TQ002")]
    public void CompiledInvalidFixturesFailTheIntendedPolicy(string members, string code)
    {
        SyntaxTree tree = Compile(members);
        Assert.Contains($": {code}:", Assert.Single(TestQualityPolicy.Diagnostics(tree)), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("[Theory, InlineData(1)] public void First(int value) => Assert.True(value > 0); [Theory, InlineData(2)] public void Second(int value) => Assert.True(value > 0);")]
    [InlineData("[Fact] public void First() => Assert.Equal(2, 1 + 1); [Fact] public void Second() => Assert.Equal(3, 1 + 2);")]
    [InlineData("[Fact] public void UsesHelper() => Check(); private static void Check() => Assert.Equal(2, 1 + 1);")]
    public void CompiledDistinctCasesAndDelegatedAssertionsRemainValid(string members) =>
        Assert.Empty(TestQualityPolicy.Diagnostics(Compile(members)));

    private static SyntaxTree Compile(string members)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText($"using Xunit; public class Cases {{ {members} }}", path: "Cases.cs");
        CSharpCompilation compilation = CSharpCompilation.Create("TestQualityFixture", [tree],
            PresentationSourceCompilation.References(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var output = new MemoryStream();
        var emitted = compilation.Emit(output);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
        return tree;
    }
}

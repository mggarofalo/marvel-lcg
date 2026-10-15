using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Marvel.Architecture.Tests;

public sealed class GeneratedPartialTypePolicyTests
{
    [Theory]
    [InlineData("src/Marvel.Godot/Main.cs", "namespace Marvel.Godot; public partial class Main { }", true)]
    [InlineData("src/Marvel.Server/EngineJsonContext.cs", "namespace Marvel.Server; partial class EngineJsonContext { }", true)]
    [InlineData("tools/Marvel.Cards.Extract/Printed.cs", "namespace Marvel.Cards.Extract; partial class Printed { }", true)]
    [InlineData("src/Marvel.Godot/Other.cs", "namespace Marvel.Godot; public partial class Main { }", false)]
    [InlineData("src/Marvel.Godot/Main.cs", "namespace Wrong; public partial class Main { }", false)]
    [InlineData("src/Marvel.Godot/Main.cs", "namespace Marvel.Godot; public partial class Other { }", false)]
    [InlineData("src/Marvel.Godot/Main.cs", "namespace Marvel.Godot; partial class Main { } partial class Other { }", false)]
    [InlineData("src/Marvel.Godot/Main.cs", "namespace Marvel.Godot; class Owner { partial class Main { } }", false)]
    [InlineData("src/Marvel.Godot/Main.cs", "namespace Marvel.Godot { partial class Main { } }", false)]
    public void OnlyExactGeneratorDeclarationsAreExempt(string path, string source, bool expected)
    {
        CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(
            source, cancellationToken: TestContext.Current.CancellationToken)
            .GetCompilationUnitRoot(TestContext.Current.CancellationToken);
        TypeDeclarationSyntax declaration = root.DescendantNodes().OfType<TypeDeclarationSyntax>()
            .First(type => type.Modifiers.Any(SyntaxKind.PartialKeyword));

        Assert.Equal(expected, GeneratedPartialTypePolicy.IsRequired(path, root, declaration));
    }
}

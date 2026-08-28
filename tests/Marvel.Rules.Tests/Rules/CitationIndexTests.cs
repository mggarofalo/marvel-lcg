using Marvel.Rules.Index;
using Xunit;

namespace Marvel.Rules.Tests.Rules;

/// <summary>The source reader behind the Rules Reference citation report.</summary>
public sealed class CitationIndexTests
{
    [Fact]
    public void DocumentationExamplesAreNotCitations()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            $"marvel-rules-index-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            string attribute = "[Rule(\"rr:actual-citation\")]";
            string noted = "[Rule(\"rr:noted-citation\", Note = \"why it belongs here\")]";
            string example = "[Rule(\"rr:not-a-citation\")]";
            File.WriteAllLines(
                Path.Combine(root, "ExampleTests.cs"),
                [
                    $"/// Every <c>{example}</c> is checked.",
                    "/*",
                    example,
                    "*/",
                    "#if false",
                    example,
                    "#endif",
                    "string interpolation = $@\"",
                    "{ '\"' }",
                    example,
                    "\";",
                    "string documentation = \"\"\"",
                    example,
                    "\"\"\";",
                    "string verbatim = @\"",
                    example,
                    "\";",
                    "string marker = \"/*\";",
                    "char slash = '/';",
                    "public sealed class ExampleTests",
                    "{",
                    $"    {attribute}",
                    $"    {noted}",
                    "    [Fact]",
                    "    public void Example() { }",
                    "}",
                ]);

            Assert.Equal(
                [
                    new Cited("rr:actual-citation", "ExampleTests.cs"),
                    new Cited("rr:noted-citation", "ExampleTests.cs"),
                ],
                Citations.Read(root, root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

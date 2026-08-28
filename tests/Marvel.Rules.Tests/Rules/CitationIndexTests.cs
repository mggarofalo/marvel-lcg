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
            File.WriteAllText(
                Path.Combine(root, "ExampleTests.cs"),
                $$"""
                /// Every <c>[Rule("rr:not-a-citation")]</c> is checked.
                public sealed class ExampleTests
                {
                    {{attribute}}
                    [Fact]
                    public void Example() { }
                }
                """);

            Cited citation = Assert.Single(Citations.Read(root, root));
            Assert.Equal("rr:actual-citation", citation.Id);
            Assert.Equal("ExampleTests.cs", citation.Site);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

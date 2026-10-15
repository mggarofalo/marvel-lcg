using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Marvel.Architecture.Tests;

public sealed class PresentationSourceCompilationTests
{
    [Fact]
    public void NestedSourcesCannotHideEngineMutationOrConstants()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"marvel-source-policy-{Guid.NewGuid():N}");
        try
        {
            string nested = Path.Combine(directory, "Projection", "Cards");
            Directory.CreateDirectory(nested);
            File.WriteAllText(Path.Combine(nested, "BoundaryViolation.cs"), """
                using Marvel.Rules.Play;
                using Marvel.Rules.State;
                internal static class BoundaryViolation
                {
                    public static void Mutate(Card card)
                    {
                        _ = EncounterDeck.AccelerationToken;
                        card.Exhaust();
                    }
                }
                """);
            foreach (string generated in new[] { "bin", "obj", ".godot" })
            {
                string generatedDirectory = Path.Combine(directory, generated);
                Directory.CreateDirectory(generatedDirectory);
                File.WriteAllText(Path.Combine(generatedDirectory, "Generated.cs"), "invalid C#");
            }

            CSharpCompilation compilation = PresentationSourceCompilation.Compile(directory);

            Assert.Equal(
                [
                    "Marvel.Rules.Play.EncounterDeck.AccelerationToken",
                    "Marvel.Rules.State.Card.Exhaust()",
                ],
                PresentationSourceCompilation.ExternalMarvelMembers(compilation));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

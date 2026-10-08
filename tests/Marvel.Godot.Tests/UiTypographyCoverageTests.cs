using System.Globalization;
using Marvel.Tests;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class UiTypographyCoverageTests
{
    [Fact]
    public void NativeShapingProbeIncludesEveryNonAsciiUiSymbolAndNumericMarker()
    {
        string root = Path.GetFullPath(Path.Combine(RepositoryPaths.Dataset("cards", "cards.json"), "../../../src/Marvel.Godot"));
        string[] files = [.. Directory.EnumerateFiles(root, "*.cs"), Path.Combine(root, "Main.tscn")];
        char[] symbols = [.. files.SelectMany(File.ReadAllText).Where(character => character > 127
            && (char.IsSymbol(character) || char.IsPunctuation(character)
                || char.GetUnicodeCategory(character) == UnicodeCategory.LetterNumber)).Distinct()];
        Assert.NotEmpty(symbols);
        foreach (char symbol in symbols) Assert.Contains(symbol, UiTypographySample.Characters);
    }
}

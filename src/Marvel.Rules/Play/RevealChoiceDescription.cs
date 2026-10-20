using Marvel.Rules.State;
namespace Marvel.Rules.Play;

/// <summary>Printed display copy for an already admitted reveal ability; never an execution parser.</summary>
internal static class RevealChoiceDescription
{
    internal static string Printed(ICardFacts facts, string face, int ordinal)
    {
        string text = facts.Text(face);
        // Product display choice: use a complete printed paragraph when its trigger
        // is explicit. Unrecognized formatting retains all printed text.
        string[] paragraphs = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        string[] revealed = paragraphs.Where(paragraph =>
            paragraph.StartsWith("When Revealed:", StringComparison.Ordinal)).ToArray();
        if (ordinal >= 0 && ordinal < revealed.Length)
            return revealed[ordinal];
        return string.IsNullOrWhiteSpace(text)
            ? "Resolve this card's printed When Revealed ability."
            : "Printed card text: " + text;
    }
}

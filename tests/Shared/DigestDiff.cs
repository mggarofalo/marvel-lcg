using System.Text.Json;

namespace Marvel.Tests;

/// <summary>
/// Names the first card two digests disagree about.
/// </summary>
/// <remarks>
/// The digest is a document rather than a hash precisely so a divergence can
/// name a card and a field. Printing two 11 KB strings at the moment that
/// matters most would throw that away, so every byte comparison in the suite
/// reports through here.
/// </remarks>
public static class DigestDiff
{
    /// <summary>Describes the first difference, for an assertion message.</summary>
    /// <param name="recorded">The digest from the fixture.</param>
    /// <param name="produced">The digest the C# engine built.</param>
    public static string Describe(string recorded, string produced)
    {
        using var expected = JsonDocument.Parse(recorded);
        using var actual = JsonDocument.Parse(produced);
        var left = expected.RootElement.GetProperty("cards").EnumerateArray().ToList();
        var right = actual.RootElement.GetProperty("cards").EnumerateArray().ToList();

        if (left.Count != right.Count)
        {
            // Report the ids rather than the counts: a missing card is a card
            // the engine failed to make, and which one it is says why.
            var mine = right.Select(card => card.GetProperty("id").GetInt32()).ToHashSet();
            var missing = left.Select(card => card.GetProperty("id").GetInt32())
                              .Where(id => !mine.Contains(id)).ToList();
            var extra = mine.Where(id => !left.Select(c => c.GetProperty("id").GetInt32())
                                              .Contains(id)).ToList();
            return $"the recording has {left.Count} cards, this board has {right.Count}"
                 + (missing.Count > 0 ? $"\n  never made: {string.Join(", ", missing)}" : "")
                 + (extra.Count > 0 ? $"\n  made and should not be: {string.Join(", ", extra)}" : "");
        }

        for (int index = 0; index < left.Count; index++)
        {
            string a = left[index].GetRawText();
            string b = right[index].GetRawText();
            if (a != b)
            {
                return $"card {index} differs\n  recorded {a}\n  produced {b}";
            }
        }

        return "the card arrays agree; the difference is elsewhere in the document";
    }
}

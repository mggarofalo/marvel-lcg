using Marvel.Rules.Setup;

namespace Marvel.Content.Setup;

/// <summary>Enumerates small editor quantities through the authoritative construction assessment.</summary>
public static class CoreDeckQuantities
{
    /// <summary>Offers removal of optional imported faces and legal positive quantities for admitted faces.</summary>
    public static IReadOnlyDictionary<string, int[]> Assess(IReadOnlyList<string> draft, HeroSetup hero,
        CardCatalog cards, IReadOnlySet<string> authored)
    {
        var result = new Dictionary<string, int[]>(StringComparer.Ordinal);
        var required = hero.HeroDeck.ToHashSet(StringComparer.Ordinal);
        foreach (string face in authored.Concat(draft).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            if (required.Contains(face)) continue;
            var allowed = new List<int> { 0 };
            var remainder = draft.Where(item => item != face).ToArray();
            for (int quantity = 1; quantity <= 3; quantity++)
                if (CoreDeckAssessment.Assess([.. remainder, .. Enumerable.Repeat(face, quantity)], hero, cards, authored).CanExtend)
                    allowed.Add(quantity);
            // Product choice: optional imported cards can always be removed, even when other corrections remain.
            result.Add(face, allowed.ToArray());
        }
        return result;
    }
}

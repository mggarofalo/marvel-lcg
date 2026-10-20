using Marvel.Rules.State;
using System.Collections.Immutable;

namespace Marvel.Rules.Setup;

/// <summary>Checks selected aspects separately from mandatory signature membership.</summary>
internal static class DeckAspectAssessment
{
    private static readonly ImmutableArray<string> Aspects = ["Aggression", "Justice", "Leadership", "Protection"];

    internal static void Assess(IReadOnlyList<string> deck, IReadOnlyList<string> customization,
        ICardFacts facts, DeckAspectRule rule, List<DeckDiagnostic> diagnostics)
    {
        if (!Enum.IsDefined(rule)) throw new ArgumentOutOfRangeException(nameof(rule));
        var selected = new HashSet<string>(StringComparer.Ordinal);
        foreach (string face in customization.Distinct(StringComparer.Ordinal))
        {
            string aspect = Class(facts, face);
            if (Aspects.Contains(aspect)) selected.Add(aspect);
            else if (aspect != "Basic") diagnostics.Add(new("unsupported-classification", face,
                "Choose a Basic card or a card from a supported aspect."));
        }
        // Core Learn to Play p.22: "exactly one aspect" and cards from that aspect and/or Basic.
        // 04031b Double Agent: "Choose two aspects instead of one during deck-building."
        int maximum = rule == DeckAspectRule.EqualTwoAspects ? 2 : 1;
        if (selected.Count > maximum)
        {
            diagnostics.Add(new("multiple-aspects", "", $"Choose cards from at most {maximum} aspects.", selected.Count, maximum));
            return;
        }
        if (rule == DeckAspectRule.EqualTwoAspects) Balance(deck, facts, selected, diagnostics);
    }

    private static void Balance(IReadOnlyList<string> deck, ICardFacts facts,
        HashSet<string> selected, List<DeckDiagnostic> diagnostics)
    {
        // 04031b: "You must include an equal number of cards from those aspects in your deck."
        // Signature aspect cards count, but do not choose the customization's two aspects.
        int deficit = int.MaxValue;
        for (int left = 0; left < Aspects.Length; left++)
            for (int right = left + 1; right < Aspects.Length; right++)
            {
                string first = Aspects[left], second = Aspects[right];
                if (selected.Any(aspect => aspect != first && aspect != second)) continue;
                int difference = Math.Abs(deck.Count(face => Class(facts, face) == first)
                    - deck.Count(face => Class(facts, face) == second));
                deficit = Math.Min(deficit, difference);
            }
        if (deficit > 0) diagnostics.Add(new("aspect-balance", "",
            $"Balance your two chosen aspects; add {deficit} cards to the smaller aspect or remove cards from the larger.", deficit, 0));
    }

    private static string Class(ICardFacts facts, string face) => facts.Attributes(face).GetValueOrDefault("Class", "");
}

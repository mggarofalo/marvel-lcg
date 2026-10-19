using Marvel.Rules.Setup;

namespace Marvel.Content.Setup;

/// <summary>Checks product admission before asking the engine about construction.</summary>
public static class CoreDeckAssessment
{
    /// <summary>Accepts an editable draft; unsupported faces remain visible as corrections.</summary>
    public static DeckAssessment Assess(IReadOnlyList<string> draft, HeroSetup hero,
        CardCatalog cards, IReadOnlySet<string> authored)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(hero);
        ArgumentNullException.ThrowIfNull(cards);
        ArgumentNullException.ThrowIfNull(authored);
        var diagnostics = new List<DeckDiagnostic>();
        foreach (string face in draft.Distinct(StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(face) || face.Contains(',', StringComparison.Ordinal))
                diagnostics.Add(new("single-face-required", face ?? "", "Choose one printed player-card face."));
            else if (!cards.Has(face))
                diagnostics.Add(new("unknown-face", face, "This printed card face is not recognized."));
            else if (!authored.Contains(face))
                diagnostics.Add(new("unsupported-face", face, "This card is outside the supported Core content."));
        }
        // Product choice: never assess a reduced substitute for the submitted deck.
        // Resolve admission defects first so unknown facts cannot produce false legality.
        return diagnostics.Count > 0 ? new(null, diagnostics)
            : PlayerDeckAssessment.Assess(new(draft), hero.HeroDeck, hero.Hero.Single(), cards, hero.AspectRule);
    }
}

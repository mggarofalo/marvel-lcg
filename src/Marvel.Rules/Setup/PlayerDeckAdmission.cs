using Marvel.Rules.State;

namespace Marvel.Rules.Setup;

/// <summary>Admits ordinary player decks before any game state is allocated.</summary>
public static class PlayerDeckAdmission
{
    /// <summary>Classifies an ordered recipe and verifies its exact signature-card membership.</summary>
    public static IReadOnlyList<Creation> Admit(
        PlayerDeckRecipe recipe, IReadOnlyList<string> signature, int player, ICardFacts facts, string identity)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentNullException.ThrowIfNull(signature);
        ArgumentNullException.ThrowIfNull(facts);
        var assessment = PlayerDeckAssessment.Assess(recipe, signature, identity, facts);
        if (!assessment.IsValid)
            throw new ArgumentException(string.Join("; ", assessment.Diagnostics.Select(item => item.Message)), nameof(recipe));
        var signatureFaces = signature.ToHashSet(StringComparer.Ordinal);
        return recipe.Cards.Select(face => new Creation(face,
            signatureFaces.Contains(face) ? CreationSource.HeroDeck : CreationSource.PlayerDeck, player)).ToArray();
    }
}

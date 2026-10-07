using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Places a sole response's commitment beside its accept and pass controls.</summary>
internal static class ContextualOfferDescription
{
    internal static string? SingleResponse(PromptPresentation? prompt, PublicDecisionKind? kind)
    {
        if (kind != PublicDecisionKind.Response || prompt is null) return null;
        AffordancePresentation[] offers = [.. prompt.Affordances.Where(offer => offer.Illegal is null)];
        if (offers.Length != 1) return null;
        string summary = DecisionCopy.CompactActionSummary(offers[0]);
        return summary.Length == 0 ? null : summary;
    }
}

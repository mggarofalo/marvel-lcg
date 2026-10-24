using Marvel.View;
using Marvel.Rules.Prompts;

namespace Marvel.Godot;

/// <summary>Matches offered card identities to already-authorized current faces in offer order.</summary>
internal static class SearchChoiceCandidates
{
    internal static IReadOnlyList<BoardCardPresentation> Inspected(Prompt prompt, BoardPresentation board) =>
        prompt.ContextCardIds.Select(id => board.Areas.SelectMany(area => area.Cards)
            .FirstOrDefault(card => card.TargetId == id && !card.Concealed))
            .OfType<BoardCardPresentation>().ToArray();

    internal static IReadOnlyList<BoardCardPresentation> From(PromptPresentation prompt, BoardPresentation board) =>
        prompt.Affordances.Select(option => board.Areas.SelectMany(area => area.Cards)
            .FirstOrDefault(card => card.TargetId is not null && card.TargetId == option.CardAnchorId && !card.Concealed))
            .OfType<BoardCardPresentation>().ToArray();
}

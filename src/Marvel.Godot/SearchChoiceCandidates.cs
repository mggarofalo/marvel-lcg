using Marvel.View;

namespace Marvel.Godot;

/// <summary>Matches offered card identities to already-authorized current faces in offer order.</summary>
internal static class SearchChoiceCandidates
{
    internal static IReadOnlyList<BoardCardPresentation> From(PromptPresentation prompt, BoardPresentation board) =>
        prompt.Affordances.Select(option => board.Areas.SelectMany(area => area.Cards)
            .FirstOrDefault(card => card.TargetId is not null && card.TargetId == option.CardAnchorId && !card.Concealed))
            .OfType<BoardCardPresentation>().ToArray();
}
